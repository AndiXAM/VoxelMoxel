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
    private bool IsRun = false; // Переключатель бега

    

    // --- НАСТРОЙКИ ДЭША ---
    private bool isDashCooldown = false;
    
    // Модификатор для РЕЗКОГО рывка (активная фаза)
    private StatModifier dashActiveBonus = new StatModifier(4f, StatModType.Flat, "DashActive");
    
    // Переменная для затухания после рывка
    private float dashFadeSpeed = 0f; 

    // Модификатор для обычного бега (на Ctrl)
    private StatModifier runBonus = new StatModifier(2.5f, StatModType.Flat, "Run");

    public Transform Camera;

    [Header("Dash Visuals")]
    public SkinnedMeshRenderer playerMesh; // Ссылка на сетку (Mesh) твоего персонажа
    public Material ghostMaterial;         // Тот самый материал, который мы создали
    public float ghostSpawnRate = 0.05f;   // Как часто спавнить фантомов (каждые 0.05 сек)

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

        // Логика затухания (простая математика: просто отнимаем число каждый кадр)
        if (dashFadeSpeed > 0)
        {
            // Уменьшаем скорость на 40 единиц в секунду (настрой это число, если затухание слишком долгое)
            dashFadeSpeed -= 10f * Time.deltaTime; 
            if (dashFadeSpeed < 0) dashFadeSpeed = 0;
        }

        // 4. ДВИЖЕНИЕ
        // Скорость = (База + Бег + Активный Дэш) + Затухание
        float totalSpeed = playerStats.MoveSpeed.Value + dashFadeSpeed;
        
        characterController.Move(moveDir * totalSpeed * Time.deltaTime);


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
        float dashDuration = 0.3f; // Длина рывка
        float timer = 0f;
        float lastGhostSpawnTime = 0f;

        // Пока длится рывок (0.3 сек), мы постоянно бежим вперед и спавним фантомов
        while (timer < dashDuration)
        {
            timer += Time.deltaTime; // Увеличиваем таймер

            // Если прошло достаточно времени с прошлого фантома - спавним нового
            if (timer - lastGhostSpawnTime > ghostSpawnRate)
            {
                SpawnGhost();
                lastGhostSpawnTime = timer;
            }

            yield return null; // Ждем до следующего кадра (ОБЯЗАТЕЛЬНО!)
        }

        // --- ФАЗА 2: ЗАТУХАНИЕ ---
        playerStats.DodgeInviсible = false;
        playerStats.MoveSpeed.RemoveModifier(dashActiveBonus);
        dashFadeSpeed = 3f; 

        // --- ФАЗА 3: ПЕРЕЗАРЯДКА ---
        yield return new WaitForSeconds(2f); 
        isDashCooldown = false;
    }

    // Метод, который создает слепок (фантом)
    private void SpawnGhost()
    {
        // Проверка от ошибок
        if (playerMesh == null || ghostMaterial == null) return;

        // 1. Создаем пустой объект
        GameObject ghostObj = new GameObject("DashGhost");
        
        // 2. Ставим его туда же, где сейчас игрок (и с тем же поворотом)
        ghostObj.transform.position = playerMesh.transform.position;
        ghostObj.transform.rotation = playerMesh.transform.rotation;

        // 3. Вешаем нужные компоненты для отображения
        MeshRenderer meshRenderer = ghostObj.AddComponent<MeshRenderer>();
        MeshFilter meshFilter = ghostObj.AddComponent<MeshFilter>();

        // 4. ДЕЛАЕМ "ФОТОГРАФИЮ" (Слепок позы)
        Mesh bakedMesh = new Mesh();
        playerMesh.BakeMesh(bakedMesh); // Запекаем текущую позу в статический Mesh
        meshFilter.mesh = bakedMesh;

        // 5. Красим в белый полупрозрачный материал
        meshRenderer.material = ghostMaterial;

        // 6. Вешаем скрипт, который заставит его раствориться
        ghostObj.AddComponent<GhostFade>();
    }

     public void SaveData(SaveData data)
    {
        // Записываем текущие координаты в массив
        data.playerPosition[0] = transform.position.x;
        data.playerPosition[1] = transform.position.y;
        data.playerPosition[2] = transform.position.z;
        
        Debug.Log($"<color=green>[Save] Позиция игрока сохранена: {transform.position}</color>");
    }

    // 2. Метод ЗАГРУЗКИ (Применение данных из файла в игру)
    public void LoadData(SaveData data)
    {
        // ЗАЩИТА: Если в файле везде нули (новый сейв), не телепортируем.
        // Иначе игрок упадет под карту при первом же запуске.
        if (data.playerPosition[0] == 0 && data.playerPosition[1] == 0 && data.playerPosition[2] == 0)
        {
            Debug.Log("[Load] Данные позиции отсутствуют, остаемся на стартовой точке.");
            return;
        }

        // Получаем ссылку на контроллер, если её нет
        if (characterController == null) characterController = GetComponent<CharacterController>();

        // ВАЖНО: CharacterController блокирует смену позиции через transform.position.
        // Чтобы телепорт сработал, его ОБЯЗАТЕЛЬНО нужно выключить на время перемещения.
        characterController.enabled = false;

        // Применяем координаты из сохранения
        Vector3 targetPos = new Vector3(data.playerPosition[0], data.playerPosition[1], data.playerPosition[2]);
        transform.position = targetPos;

        // Сбрасываем скорость падения в 0, чтобы игрока не "вдавило" в пол накопленной гравитацией
        velocity = Vector3.zero;

        // Включаем контроллер обратно
        characterController.enabled = true;

        Debug.Log($"<color=yellow>[Load] Игрок успешно загружен в позицию: {targetPos}</color>");
    }
}