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

    [Header("Attack Timings")]
    public float hitboxActivationDelay = 0.4f;   
    public float hitboxDeactivationDelay = 0.5f; 
    public float recoveryDelay = 0.2f;           
    public float postAttackCooldown = 1.0f;      

    [Header("Audio")]
    [Tooltip("Звук взмаха/рыка при начале атаки")]
    public AudioClip attackSound; // <--- НОВОЕ ПОЛЕ ДЛЯ ЗВУКА ЗАМАХА
    private AudioSource audioSource;

    [Header("Rotation Speeds")]
    public float rotationSpeedNormal = 8f; // Быстрый поворот во время бега/паузы
    public float rotationSpeedAttack = 2f; // Медленный поворот во время удара

    [HideInInspector] public bool isSwinging = false;
    private float currentCooldownTimer = 0f;

    // Дебаг
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
        if (enemyHitbox == null) enemyHitbox = GetComponentInChildren<BoxCollider>();

        if (enemyHitbox != null)
        {
            hitScript = enemyHitbox.GetComponent<EnemyHitbox>();
            if (hitScript == null) hitScript = enemyHitbox.gameObject.AddComponent<EnemyHitbox>();
            
            // Если у монстра нет своего AudioSource, создаем его
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            
            hitScript.mainAudioSource = audioSource;
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

        // 1. БЛОКИРОВКА ВО ВРЕМЯ УДАРА (Анимация летит)
        if (isSwinging) 
        {
            // Враг стоит на месте, но МЕДЛЕННО поворачивается за игроком!
            FaceTarget(rotationSpeedAttack);
            return;
        }

        // 2. ОТКАТ АТАКИ (Отдых)
        if (currentCooldownTimer > 0)
        {
            currentCooldownTimer -= Time.deltaTime;
            StopMovement();
            
            // Во время отдыха враг БЫСТРО поворачивается за игроком
            FaceTarget(rotationSpeedNormal);
            return; 
        }

        // 3. ПОИСК ЦЕЛИ
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

    // --- НОВЫЙ МЕТОД ПОВОРОТА ---
    private void FaceTarget(float turnSpeed)
    {
        // Вычисляем направление к игроку (игнорируя разницу в высоте Y)
        Vector3 direction = (target.position - transform.position).normalized;
        direction.y = 0f; 

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Плавно вращаем модельку
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * turnSpeed);
        }
    }

    private void MoveToTarget()
    {
        if (agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.SetDestination(target.position);
            agent.speed = stats.MoveSpeed.Value;
            
            // Во время бега NavMeshAgent сам крутит врага, но мы можем помочь
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
        if (agent.isOnNavMesh) agent.isStopped = true;
        if (animator != null) animator.SetBool("Fly", false);
    }

    // ================= ПОСЛЕДОВАТЕЛЬНОСТЬ АТАКИ =================
    private IEnumerator AttackRoutine()
    {
        isSwinging = true;

        StopMovement();
        if (hitScript != null) hitScript.SetDamage(stats.Damage.Value);

        if (animator != null) animator.SetBool("Attack", true);

        // --- ВОСПРОИЗВЕДЕНИЕ ЗВУКА ЗАМАХА ---
        if (audioSource != null && attackSound != null)
        {
            audioSource.PlayOneShot(attackSound);
        }

        // --- ФАЗА 1: ЗАМАХ ---
        dbg_Windup = true;
        yield return new WaitForSeconds(hitboxActivationDelay);
        dbg_Windup = false;

        // --- ФАЗА 2: АКТИВНЫЙ УРОН ---
        dbg_HitboxActive = true;
        if (hitScript != null) hitScript.StartAttack();
        
        yield return new WaitForSeconds(hitboxDeactivationDelay);
        
        if (hitScript != null) hitScript.EndAttack();
        dbg_HitboxActive = false;

        // --- ФАЗА 3: ОТКАТ АНИМАЦИИ ---
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
}