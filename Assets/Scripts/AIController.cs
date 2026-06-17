using UnityEngine;
using UnityEngine.AI;
using System.Collections; 

public class MonsterAI : MonoBehaviour
{
    [Header("Targeting")]
    public Transform target;
    public float aggroRange = 15f; 
    public float attackDistance = 3f; 

    [Header("References")]
    private NavMeshAgent agent;
    public Animator animator;
    public BoxCollider enemyHitbox;
    public StatsContainer stats;
    private EnemyHitbox hitScript;

    [Header("Attack Settings & Timings")]
    [Tooltip("Сила ошеломления (Hitstun), которую монстр накладывает на игрока при ударе")]
    public float impactPower = 1f;       // <--- НОВАЯ НАСТРОЙКА ИМПАКТА
    
    public float hitboxActivationDelay = 0.4f;   
    public float hitboxDeactivationDelay = 0.5f; 
    public float recoveryDelay = 0.2f;           
    public float postAttackCooldown = 1.0f;      

    [Header("Audio")]
    public AudioClip attackSound; 
    private AudioSource audioSource;

    [Header("Rotation Speeds")]
    public float rotationSpeedNormal = 8f; 
    public float rotationSpeedAttack = 2f; 

    [HideInInspector] public bool isSwinging = false;
    private float currentCooldownTimer = 0f;

    [Header("Debug Phase Viewer (Только для чтения)")]
    public bool dbg_IsAttacking;  
    public bool dbg_Windup;       
    public bool dbg_HitboxActive; 
    public bool dbg_Recovery;     
    public bool dbg_Resting;      

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        audioSource = GetComponent<AudioSource>();
        if (stats == null) stats = GetComponent<StatsContainer>();
        
        // Поиск хитбокса
        if (enemyHitbox == null) enemyHitbox = GetComponentInChildren<BoxCollider>();

        if (enemyHitbox != null)
        {
            hitScript = enemyHitbox.GetComponent<EnemyHitbox>();
            if (hitScript == null) hitScript = enemyHitbox.gameObject.AddComponent<EnemyHitbox>();
            
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            
            // --- НАСТРОЙКА ХИТБОКСА (УРОН И ИМПАКТ) ---
            hitScript.mainAudioSource = audioSource;
            
            // Передаем силу Импакта из Инспектора монстра прямо в скрытую переменную Хитбокса!
            hitScript.currentImpactPower = impactPower; 
            
            hitScript.ForceResetHitbox();
        }

        if (target == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }
    }

    void Update()
    {
        if (target == null || stats == null) return;
        
        UpdateDebugVisuals();

        // --- ГЛОБАЛЬНАЯ ФИЗИКА (ОТБРАСЫВАНИЕ) ---
        // Работает всегда, даже если монстр бьет или отдыхает!
        ApplyKnockback();

        // 1. Блокировка во время удара (Анимация летит)
        if (isSwinging) 
        {
            FaceTarget(rotationSpeedAttack);
            return; // Выходим, но отбрасывание выше уже сработало!
        }

        // 2. Откат атаки (Отдых)
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            StopMovement();
            FaceTarget(rotationSpeedNormal);
            return; // Выходим, но отбрасывание сработало!
        }

        // 3. Поиск цели
        float distance = Vector3.Distance(transform.position, target.position);

        if (distance <= attackDistance)
        {
            StartCoroutine(AttackRoutine());
        }
        else if (distance <= aggroRange)
        {
            MoveToTarget();
        }
        else
        {
            StopMovement();
        }
    }

    private void FaceTarget(float turnSpeed)
    {
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; 

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void MoveToTarget()
    {
        if (agent.isOnNavMesh)
        {
            
            
            agent.isStopped = false;
            agent.SetDestination(target.position);
            agent.speed = stats.MoveSpeed.Value * stats.ImpactSpeedMultiplier; 
            
            FaceTarget(rotationSpeedNormal);

            if (animator != null) 
            {
                animator.SetBool("Fly", true); 
                animator.SetBool("Attack", false); 
            }
        }
    }

    private void StopMovement()
    {
        if (agent.isOnNavMesh) 
        {
            agent.isStopped = true;
        }
        if (animator != null) animator.SetBool("Fly", false);
    }
    private IEnumerator AttackRoutine()
    {
        isSwinging = true;

        StopMovement();
        
        // Обновляем урон перед ударом (вдруг на монстра повесили бафф на урон)
        if (hitScript != null) hitScript.SetDamage(stats.Damage.Value);

        if (animator != null) animator.SetBool("Attack", true);

        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        dbg_Windup = true;
        yield return new WaitForSeconds(hitboxActivationDelay);
        dbg_Windup = false;

        dbg_HitboxActive = true;
        if (hitScript != null) hitScript.StartAttack();
        
        yield return new WaitForSeconds(hitboxDeactivationDelay);
        
        if (hitScript != null) hitScript.EndAttack();
        dbg_HitboxActive = false;

        dbg_Recovery = true;
        yield return new WaitForSeconds(recoveryDelay);
        dbg_Recovery = false;

        if (animator != null) animator.SetBool("Attack", false);

        currentCooldownTimer = postAttackCooldown;
        isSwinging = false;
    }

    private void UpdateDebugVisuals()
    {
        dbg_IsAttacking = isSwinging;
        dbg_Resting = (!isSwinging && currentCooldownTimer > 0);
        
        if (!isSwinging)
        {
            dbg_Windup = false;
            dbg_HitboxActive = false;
            dbg_Recovery = false;
        }
    }

    private void ApplyKnockback()
    {
        if (agent.isOnNavMesh && stats.CurrentKnockbackVelocity.sqrMagnitude > 0.1f)
        {
            // 1. Двигаем
            agent.Move(stats.CurrentKnockbackVelocity * Time.deltaTime);

            // 2. Плавно гасим силу вектора
            stats.CurrentKnockbackVelocity = Vector3.Lerp(stats.CurrentKnockbackVelocity, Vector3.zero, Time.deltaTime * 3f);
        }
        else
        {
            stats.CurrentKnockbackVelocity = Vector3.zero;
        }
    }
}