using UnityEngine;
using System.Collections;

public class Character : MonoBehaviour, ISaveable
{
    private CharacterController characterController;
    private AudioSource audioSource;

    [SerializeField] StatsContainer playerStats;
    public float Gravity = -9.81f; 

    private Vector3 velocity; 
    private bool IsGrounded = false;
    private bool IsRun = false; 

    // --- НАСТРОЙКИ ДЭША ---
    private bool isDashCooldown = false;
    private StatModifier dashActiveBonus = new StatModifier(4f, StatModType.Flat, "DashActive");
    private float dashFadeSpeed = 0f; 

    private StatModifier runBonus = new StatModifier(1.5f, StatModType.Flat, "Run");

    // --- УСТАЛОСТЬ БЕГА ПОСЛЕ АТАК / СКИЛЛОВ ---
    private Coroutine runFatigueCoroutine;
    [HideInInspector] public bool isRunFatigued = false;

    public Transform Camera;
    public CharacterAnimatorController animController;

    [Header("Dash Visuals")]
    public SkinnedMeshRenderer playerMesh; 
    public Material ghostMaterial;         
    public float ghostSpawnRate = 0.05f;   

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        audioSource = GetComponentInChildren<AudioSource>();
        if (Camera == null) Camera = GameObject.Find("Main Camera").transform;
    }

    void Update()
    {
        // 1. ОПТИМИЗАЦИЯ: Считываем статус земли ровно 1 раз за кадр
        IsGrounded = characterController.isGrounded;

        if (IsGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. ВВОД И НАПРАВЛЕНИЕ
        float x = Input.GetAxisRaw("Horizontal"); // GetAxisRaw отзывчивее для клавиатуры
        float z = Input.GetAxisRaw("Vertical");

        if (playerStats != null && playerStats.IsMovementLocked)
        {
            x = 0f;
            z = 0f;
        }

        Vector3 moveInput = new Vector3(x, 0, z);
        if (moveInput.magnitude > 1) moveInput.Normalize();

        // ОПТИМИЗАЦИЯ: Убрали лишнее ".transform", так как Camera - это уже Transform!
        Vector3 cameraForward = Camera.forward;
        Vector3 cameraRight = Camera.right;
        
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDir = cameraRight * moveInput.x + cameraForward * moveInput.z;

        // 3. РЫВОК (DASH)
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashCooldown)
        {
            StartCoroutine(DashRoutine());
        }

        if (dashFadeSpeed > 0)
        {
            dashFadeSpeed -= 10f * Time.deltaTime; 
            if (dashFadeSpeed < 0) dashFadeSpeed = 0;
        }

        // 4. БЕГ (CTRL) - Переключатель
        if (Input.GetKeyDown(KeyCode.LeftControl) && !isRunFatigued)
        {
            IsRun = !IsRun;
            if (IsRun) playerStats.MoveSpeed.AddModifier(runBonus);
            else playerStats.MoveSpeed.RemoveModifier(runBonus);
        }

        // 5. ПРЫЖОК
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
        {
            velocity.y = Mathf.Sqrt(playerStats.JumpHeight.Value * -2f * Gravity);
        }

        // 6. ГРАВИТАЦИЯ (Применяем ДО финального движения)
        velocity.y += Gravity * Time.deltaTime;

        // 7. РАСЧЕТ СКОРОСТЕЙ
        float nominalSpeed = playerStats.MoveSpeed.Value; 
        float finalSpeed = (nominalSpeed + dashFadeSpeed) * playerStats.ImpactSpeedMultiplier;

        // 8. ПЛАВНОЕ ЗАТУХАНИЕ ОТБРОСА (Knockback)
        Vector3 knockbackForce = Vector3.zero;
        if (playerStats.CurrentKnockbackVelocity.sqrMagnitude > 0.1f)
        {
            knockbackForce = playerStats.CurrentKnockbackVelocity;
            playerStats.CurrentKnockbackVelocity = Vector3.Lerp(playerStats.CurrentKnockbackVelocity, Vector3.zero, Time.deltaTime * 3f);
        }
        else
        {
            playerStats.CurrentKnockbackVelocity = Vector3.zero;
        }

        // --- ГЛАВНАЯ ОПТИМИЗАЦИЯ: ОБЪЕДИНЯЕМ ВСЕ СИЛЫ В ОДИН ВЕКТОР ---
        // (Движение * Скорость) + Гравитация (velocity) + Отбрасывание (knockbackForce)
        Vector3 finalMoveVector = (moveDir * finalSpeed) + velocity + knockbackForce;

        // ОДИН единственный вызов физики на весь кадр!
        characterController.Move(finalMoveVector * Time.deltaTime);

        // 9. АНИМАЦИЯ (Только если есть контроллер)
        if (animController != null)
        {
            animController.SetAnimationSpeedMultiplier(playerStats.ImpactSpeedMultiplier);
        }
    }

    IEnumerator DashRoutine()
    {
        isDashCooldown = true;

        // --- ФАЗА 1: АКТИВНЫЙ РЫВОК ---
        playerStats.MoveSpeed.AddModifier(dashActiveBonus);
        playerStats.DodgeInviсible = true;
        
        float dashDuration = 0.3f; 
        float timer = 0f;
        float lastGhostSpawnTime = 0f;

        while (timer < dashDuration)
        {
            timer += Time.deltaTime; 

            if (timer - lastGhostSpawnTime > ghostSpawnRate)
            {
                SpawnGhost();
                lastGhostSpawnTime = timer;
            }

            yield return null; 
        }

        // --- ФАЗА 2: ЗАТУХАНИЕ ---
        playerStats.DodgeInviсible = false;
        playerStats.MoveSpeed.RemoveModifier(dashActiveBonus);
        dashFadeSpeed = 3f; 

        // --- ФАЗА 3: ПЕРЕЗАРЯДКА ---
        yield return new WaitForSeconds(2f); 
        isDashCooldown = false;
    }

    private void SpawnGhost()
    {
        if (playerMesh == null || ghostMaterial == null) return;

        GameObject ghostObj = new GameObject("DashGhost");
        ghostObj.transform.position = playerMesh.transform.position;
        ghostObj.transform.rotation = playerMesh.transform.rotation;

        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();

        Mesh bakedMesh = new Mesh();
        playerMesh.BakeMesh(bakedMesh); 
        meshFilter.mesh = bakedMesh;

        meshRenderer.material = ghostMaterial;
        ghostObj.AddComponent<GhostFade>();
    }

    // --- СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        data.playerPosition[0] = transform.position.x;
        data.playerPosition[1] = transform.position.y;
        data.playerPosition[2] = transform.position.z;
    }

    public void LoadData(SaveData data)
    {
        if (data.playerPosition[0] == 0 && data.playerPosition[1] == 0 && data.playerPosition[2] == 0) return;

        if (characterController == null) characterController = GetComponent<CharacterController>();

        characterController.enabled = false;
        
        Vector3 targetPos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
        transform.position = targetPos;
        velocity = Vector3.zero;
        
        characterController.enabled = true;
    }
    
    // Защита от толкания врагов как коробок (если нужно)
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;
        if (body == null || body.isKinematic) return;
        if (hit.moveDirection.y < -0.3) return; 
        
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);
        body.linearVelocity = pushDir * 2f;
    }

    public void ApplyRunFatigue(float duration = 0.75f)
    {
        if (runFatigueCoroutine != null) StopCoroutine(runFatigueCoroutine);
        runFatigueCoroutine = StartCoroutine(RunFatigueRoutine(duration));
    }

    private IEnumerator RunFatigueRoutine(float duration)
    {
        isRunFatigued = true;

        // Если игрок бежал — принудительно выключаем бег и снимаем бонус скорости
        if (IsRun)
        {
            IsRun = false;
            playerStats.MoveSpeed.RemoveModifier(runBonus);
        }

        // Синхронизируем аниматор (сбрасываем анимацию бега на шаг)
        if (animController != null)
        {
            animController.SetRunState(false);
        }

        yield return new WaitForSeconds(duration);

        isRunFatigued = false;
        runFatigueCoroutine = null;
    }

}