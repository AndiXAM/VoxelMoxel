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

    private StatModifier runBonus = new StatModifier(2.5f, StatModType.Flat, "Run");

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
        // 1. СБРОС СКОРОСТИ ПАДЕНИЯ
        IsGrounded = characterController.isGrounded;
        if (IsGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        // 2. ВВОД
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 moveInput = new Vector3(x, 0, z);
        if (moveInput.magnitude > 1) moveInput.Normalize();

        Vector3 cameraForward = Camera.transform.forward;
        Vector3 cameraRight = Camera.transform.right;
        cameraForward.y = 0;
        cameraRight.y = 0;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDir = cameraRight * moveInput.x + cameraForward * moveInput.z;

        // 3. ЛОГИКА ДЭША (SHIFT)
        if (Input.GetKeyDown(KeyCode.LeftShift) && !isDashCooldown)
        {
            StartCoroutine(DashRoutine());
        }

        // Логика затухания инерции рывка
        if (dashFadeSpeed > 0)
        {
            dashFadeSpeed -= 10f * Time.deltaTime; 
            if (dashFadeSpeed < 0) dashFadeSpeed = 0;
        }

        // 4. ДВИЖЕНИЕ (ФИНАЛЬНЫЙ РАСЧЕТ)
        // Номинальная скорость (База + Спринт + Жесткий Дэш)
        float nominalSpeed = playerStats.MoveSpeed.Value; 

        // Итоговая скорость (С учетом затухания рывка и замедления от ударов)
        float finalSpeed = (nominalSpeed + dashFadeSpeed) * playerStats.ImpactSpeedMultiplier;

        // Применяем движение с учетом вектора отбрасывания!
        Vector3 finalMoveVector = (moveDir * finalSpeed) + playerStats.CurrentKnockbackVelocity;
        characterController.Move(finalMoveVector * Time.deltaTime);

        // --- УПРАВЛЕНИЕ АНИМАЦИЕЙ ---
        if (animController != null)
        {
            // Передаем множитель замедления в Аниматор, чтобы ноги двигались медленнее при получении удара
            float animSpeed = playerStats.ImpactSpeedMultiplier;
            animController.SetAnimationSpeedMultiplier(animSpeed);
        }

        // 5. ОБЫЧНЫЙ БЕГ (CTRL) - Переключатель
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            if (!IsRun)
            {
                IsRun = true;
                playerStats.MoveSpeed.AddModifier(runBonus);
            }
            else
            {
                IsRun = false;
                playerStats.MoveSpeed.RemoveModifier(runBonus);
            }
        }

        // 6. ПРЫЖОК
        if (Input.GetKeyDown(KeyCode.Space) && IsGrounded)
        {
            velocity.y = Mathf.Sqrt(playerStats.JumpHeight.Value * -2f * Gravity);
        }

        // 7. ГРАВИТАЦИЯ
        velocity.y += Gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
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
}