using UnityEngine;
using UnityEngine.AI;
using System.Collections; 

[RequireComponent(typeof(NavMeshAgent))]
public class AdvancedEnemyAI : MonoBehaviour
{
    public enum AIState
    {
        Chase,          
        Attacking,      
        ChoosingAction, 
        Waiting,        
        Defending,
        Dodging         // <--- НОВОЕ СОСТОЯНИЕ
    }

    [Header("Текущее состояние")]
    public AIState currentState = AIState.Chase;

    [Header("Настройки ИИ (Дистанции)")]
    public Transform target;        
    public float aggroRange = 15f;  
    public float drawWeaponRange = 7f; 
    public float attackRange = 2f;  
    public float reactionRange = 4f; 

    [Header("Настройки Бега")]
    public float timeToRun = 5f; 
    public float runSpeedMultiplier = 1.3f; 
    private float chaseTimer = 0f;

    [Header("Настройки ИИ (Поведение после атаки)")]
    public float chanceToAttack = 50f;
    public float chanceToDefend = 35f;
    public float chanceToWait = 15f;

    [Header("Шанс реакции при сближении")]
    [Range(0, 100)]
    public float chaseReactionChance = 40f; 
    
    // --- НОВЫЙ БЛОК: НАСТРОЙКИ УВОРОТА ---
    [Header("Настройки Уворота (Dodge)")]
    public int maxDodgeCharges = 1;         // Максимум зарядов
    public float dodgeRechargeTime = 7f;    // Время восстановления 1 заряда
    public float dodgeDuration = 0.4f;      // Сколько длится сам уворот (неуязвимость)
    public float dodgeSpeed = 15f;          // Скорость скольжения при увороте
    [Tooltip("Насколько сильно враг заходит за спину (в градусах). 90 = ровно сбоку.")]
    public float dodgeAngle = 60f;       

    [Tooltip("Максимальная дистанция до игрока, при которой враг будет использовать уворот (Dodge)")]
    public float maxDodgeDistance = 4f;   
    
    [Header("Dodge Visuals (Фантомы)")]
    public SkinnedMeshRenderer enemyMesh;   // Сетка врага для слепка
    public Material ghostMaterial;          // Тот же материал, что у игрока
    public float ghostSpawnRate = 0.05f;    

    private int currentDodgeCharges;
    private float dodgeRechargeTimer = 0f;
    
    private float actionTimer = 0f;

    [Header("Ссылки")]
    public StatsContainer stats;
    public AdvancedEnemyCombat combat; 
    public Animator animator;
    
    private PlayerStateFlags targetFlags;
    private NavMeshAgent agent;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        if (stats == null) stats = GetComponent<StatsContainer>();
        if (combat == null) combat = GetComponent<AdvancedEnemyCombat>();

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) 
            {
                target = player.transform;
                targetFlags = player.GetComponent<PlayerStateFlags>(); 
            }
        }
        else targetFlags = target.GetComponent<PlayerStateFlags>();

        // Инициализация уворотов
        currentDodgeCharges = maxDodgeCharges;
    }

    private void Update()
    {
        if (target == null || combat == null || stats == null) return;

        ApplyKnockback();

        float distanceToPlayer = Vector3.Distance(transform.position, target.position);

        SetWeaponState(distanceToPlayer <= drawWeaponRange);
        HandleDodgeRecharge(); // Обновляем заряды уворота

        switch (currentState)
        {
            case AIState.Chase:
                HandleChaseState(distanceToPlayer);
                break;
            case AIState.Attacking:
                ResetChaseTimer();
                HandleAttackingState(distanceToPlayer);
                break;
            case AIState.ChoosingAction:
                HandleChoosingActionState(distanceToPlayer);
                break;
            case AIState.Waiting:
                ResetChaseTimer();
                HandleWaitingState(distanceToPlayer);
                break;
            case AIState.Defending:
                ResetChaseTimer();
                HandleDefendingState(distanceToPlayer);
                break;
            case AIState.Dodging:
                // В этом состоянии Update ничего не делает, всё работает внутри Корутины!
                break;
        }

        UpdateMovementAnimation();
    }

    // ================= ЛОГИКА УВОРОТА И ЗАРЯДОВ =================

    private void HandleDodgeRecharge()
    {
        // Если зарядов меньше максимума, тикает таймер
        if (currentDodgeCharges < maxDodgeCharges)
        {
            dodgeRechargeTimer += Time.deltaTime;
            if (dodgeRechargeTimer >= dodgeRechargeTime)
            {
                currentDodgeCharges++;
                dodgeRechargeTimer = 0f;
                Debug.Log($"ВРАГ: Заряд уворота восстановлен! ({currentDodgeCharges}/{maxDodgeCharges})");
            }
        }
    }
    // ================= ЛОГИКА СОСТОЯНИЙ =================

    private void HandleChaseState(float distanceToPlayer)
    {
        if (distanceToPlayer > aggroRange)
        {
            StopMovement();
            ResetChaseTimer();
            return;
        }

        // --- НОВАЯ ЛОГИКА: ЗАЩИТА НА ПОДХОДЕ ---
        if (distanceToPlayer <= reactionRange && targetFlags != null && targetFlags.isSwingingWeapon)
        {
            // Если игрок атакует, а мы еще не в блоке/парировании
            if (!stats.IsParry && !stats.IsBlock)
            {
                float roll = Random.Range(0f, 100f);
                if (roll <= chaseReactionChance)
                {
                    Debug.Log("ВРАГ ЗАМЕТИЛ АТАКУ ПРИ СБЛИЖЕНИИ!");
                    StopMovement();
                    FaceTarget();
                    ReactToPlayerAttack(distanceToPlayer);
                    
                    // Уходим в ожидание после реакции, чтобы не "залипнуть" в беге
                    currentState = AIState.Waiting;
                    actionTimer = 0.6f;
                    return;
                }
            }
        }

        if (distanceToPlayer <= attackRange)
        {
            currentState = AIState.Attacking;
        }
        else
        {
            chaseTimer += Time.deltaTime;
            MoveToTarget();
        }
    }

    private void HandleAttackingState(float distanceToPlayer)
    {
        if (distanceToPlayer > attackRange)
        {
            currentState = AIState.Chase;
            return;
        }

        MoveToTarget();
        FaceTarget();

        if (!combat.isSwinging)
        {
            combat.TryAttack();
            if (combat.isSwinging) currentState = AIState.ChoosingAction; 
        }
    }

    private void HandleChoosingActionState(float distanceToPlayer)
    {
        if (combat.isSwinging) 
        {
            MoveToTarget();
            return; 
        }

        if (distanceToPlayer > attackRange)
        {
            currentState = AIState.Chase;
            return;
        }

        float roll = Random.Range(0f, 100f);
        if (roll <= chanceToAttack) currentState = AIState.Attacking;
        else if (roll <= chanceToAttack + chanceToDefend)
        {
            actionTimer = 0.8f;
            currentState = AIState.Defending;
        }
        else
        {
            actionTimer = 0.5f;
            currentState = AIState.Waiting;
        }
    }

    private void HandleWaitingState(float distanceToPlayer)
    {
        // В ожидании враг всё равно смотрит на игрока и может медленно идти
        if (distanceToPlayer > attackRange) MoveToTarget();
        else StopMovement();

        FaceTarget();
        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0) currentState = AIState.Chase;
    }

    private void HandleDefendingState(float distanceToPlayer)
    {
        MoveToTarget(); 
        FaceTarget();

        if (targetFlags != null && targetFlags.isSwingingWeapon)
        {
            ReactToPlayerAttack(distanceToPlayer);
            return;
        }

        actionTimer -= Time.deltaTime;
        if (actionTimer <= 0) currentState = AIState.Chase;
    }

    // ================= ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ =================

    private void MoveToTarget()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            
            float currentSpeed = stats.MoveSpeed.Value;
            if (chaseTimer >= timeToRun) currentSpeed *= runSpeedMultiplier;
            
            // Замедление от импакта работает
            agent.speed = currentSpeed * stats.ImpactSpeedMultiplier; 
        }
    }

    private void StopMovement()
    {
        if (agent.isOnNavMesh) agent.isStopped = true;
    }

    private void ResetChaseTimer()
    {
        chaseTimer = 0f;
    }

    private void UpdateMovementAnimation()
    {
        if (animator != null)
        {
            bool isMoving = agent.velocity.magnitude > 0.1f && !agent.isStopped;
            animator.SetBool("IsMoving", isMoving);
            bool isRunning = chaseTimer >= timeToRun && isMoving;
            animator.SetBool("Run", isRunning || agent.velocity.magnitude > 4.5f);
        }
    }

    private void FaceTarget()
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    private void SetWeaponState(bool state)
    {
        if (animator != null && animator.GetBool("WeaponEquipped") != state)
        {
            animator.SetBool("WeaponEquipped", state);
        }
    }

    private void ReactToPlayerAttack(float distanceToPlayer)
    {
        if (stats.IsParry || stats.IsBlock || currentState == AIState.Dodging) return;

        // 1. ПРОВЕРКА ДИСТАНЦИИ ДЛЯ УВОРОТА
        // Враг уворачивается ТОЛЬКО если игрок находится близко (например, ближе 3-4 метров)
        // Если игрок дальше - враг экономит заряд уворота и использует блок/парирование!
        // Можно вынести эту переменную в Header наверх скрипта

        if (currentDodgeCharges > 0 && distanceToPlayer <= maxDodgeDistance)
        {
            Debug.Log("ВРАГ УВОРАЧИВАЕТСЯ (Ближний бой)!");
            StartCoroutine(EnemyDodgeRoutine());
        }
        else
        {
            // 2. ДАЛЬНИЙ БОЙ ИЛИ НЕТ ЗАРЯДОВ (Блок / Парирование)
            float reactionRoll = Random.Range(0f, 100f);
            
            // На дистанции враг чаще блокирует, так как парировать снаряды сложнее
            // (Можешь настроить эти шансы по своему вкусу)
            if (reactionRoll <= 20f) 
            {
                Debug.Log("ВРАГ ПАРИРУЕТ (Дальняя/Средняя дистанция)!");
                StartCoroutine(EnemyParryRoutine());
            }
            else 
            {
                Debug.Log("ВРАГ БЛОКИРУЕТ (Дальняя/Средняя дистанция)!");
                StartCoroutine(EnemyBlockRoutine());
            }
            
            currentState = AIState.Waiting; 
            actionTimer = 0.5f; 
        }
    }

    // --- ИСПРАВЛЕННАЯ КОРУТИНА (КРУГОВОЙ УВОРОТ) ---
    private IEnumerator EnemyDodgeRoutine()
    {
        currentState = AIState.Dodging;
        currentDodgeCharges--; 
        stats.DodgeInviсible = true; 

        // Выбираем направление уворота (1 = по часовой стрелке, -1 = против)
        float directionSign = Random.Range(0, 2) == 0 ? 1f : -1f; 

        if (animator != null) animator.SetTrigger("Dodge");

        // Отключаем контроль NavMesh (чтобы не мешал скользить)
        agent.isStopped = true;
        agent.updatePosition = false; 

        float timer = 0f;
        float lastGhostSpawnTime = 0f;

        // Вычисляем общую угловую скорость (в градусах за секунду)
        // dodgeAngle - это сколько всего градусов мы должны пролететь за время dodgeDuration
        float angularSpeed = (dodgeAngle / dodgeDuration) * directionSign;

        // Запоминаем точку, вокруг которой вращаемся (Игрок)
        Vector3 centerPoint = target.position;

        // --- ПРОЦЕСС КРУГОВОГО СКОЛЬЖЕНИЯ И ФАНТОМОВ ---
        while (timer < dodgeDuration)
        {
            timer += Time.deltaTime;

            // 1. ДВИЖЕНИЕ ПО ОРБИТЕ
            // RotateAround вращает объект вокруг заданной точки, по заданной оси (Y - вверх), с заданной скоростью
            transform.RotateAround(centerPoint, Vector3.up, angularSpeed * Time.deltaTime);

            // 2. Враг продолжает смотреть на игрока во время уворота!
            FaceTarget();

            // 3. Спавн фантомов
            if (timer - lastGhostSpawnTime > ghostSpawnRate)
            {
                SpawnGhost();
                lastGhostSpawnTime = timer;
            }

            yield return null;
        }

        // --- ОКОНЧАНИЕ УВОРОТА ---
        stats.DodgeInviсible = false; 
        
        // Возвращаем контроль агенту, жестко привязывая его к новой позиции
        agent.Warp(transform.position); // Warp - самый надежный способ сбросить позицию агента
        agent.updatePosition = true;
        
        // ПОСЛЕ УВОРОТА: С вероятностью 70% атакуем в спину, 30% уходим в глухую защиту
        float roll = Random.Range(0f, 100f);
        if (roll <= 70f)
        {
            currentState = AIState.Attacking;
        }
        else
        {
            actionTimer = 0.5f;
            currentState = AIState.Defending;
        }
    }

    // Метод создания слепка (фантома) - точно как у игрока
    private void SpawnGhost()
    {
        if (enemyMesh == null || ghostMaterial == null) return;

        GameObject ghostObj = new GameObject("EnemyDashGhost");
        ghostObj.transform.position = enemyMesh.transform.position;
        ghostObj.transform.rotation = enemyMesh.transform.rotation;

        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();

        Mesh bakedMesh = new Mesh();
        enemyMesh.BakeMesh(bakedMesh); 
        meshFilter.mesh = bakedMesh;

        meshRenderer.material = ghostMaterial;
        ghostObj.AddComponent<GhostFade>(); // Тот самый скрипт исчезновения!
    }

    private IEnumerator EnemyParryRoutine()
    {
        stats.IsParry = true; 
        if (animator != null) animator.SetTrigger("Parry"); 
        yield return new WaitForSeconds(0.5f); 
        stats.IsParry = false;
    }

    private IEnumerator EnemyBlockRoutine()
    {
        stats.IsBlock = true; 
        if (animator != null) animator.SetBool("IsBlocking", true); 
        yield return new WaitForSeconds(0.7f); // Удержанный блок
        stats.IsBlock = false;
        if (animator != null) animator.SetBool("IsBlocking", false);
    }

    private void ApplyKnockback()
    {
        if (currentState == AIState.Dodging) return;

        if (agent.isOnNavMesh && stats.CurrentKnockbackVelocity.sqrMagnitude > 0.1f)
        {
            agent.ResetPath(); 
            
            // 1. Двигаем агента
            agent.Move(stats.CurrentKnockbackVelocity * Time.deltaTime);

            // 2. ПЛАВНО ГАСИМ СИЛУ ВЕКТОРA в ИИ врага!
            stats.CurrentKnockbackVelocity = Vector3.Lerp(stats.CurrentKnockbackVelocity, Vector3.zero, Time.deltaTime * 3f);
        }
        else
        {
            stats.CurrentKnockbackVelocity = Vector3.zero;
        }
    }
}