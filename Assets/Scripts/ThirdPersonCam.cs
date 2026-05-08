using Unity.Cinemachine;
using UnityEngine;
using TMPro;

public class ThirdPersonCam : MonoBehaviour
{
    [Header("Прозрачность при приближении")]
    public Renderer[] playerRenderers; 
    public float fadeStartRadius = 2.5f; 
    public float fadeEndRadius = 0.5f;   
    
    [Tooltip("Создай дубликаты материалов персонажа с типом Transparent и положи сюда, в том же порядке")]
    public Material[] fadeMaterials; // Сюда положишь свой palette2_Fade

    // Внутренняя память для оригинальных Opaque материалов
    private Material[][] originalMaterials; 
    private bool isCurrentlyFading = false; // Флаг текущего состояния
    [Header("References")]
    public Transform orientation;
    public Transform player;
    public Transform capsule;
    public Transform FreeLookCamera;

    public GameObject AimCanvas;

    public float rotationSpeed;

    [Header("Боевая Камера (Из-за плеча)")]
    [Tooltip("Объект CameraAimTarget внутри персонажа, за которым следит Cinemachine")]
    public Transform cameraFollowTarget; 
    public Vector3 freeCameraOffset = new Vector3(0f, 0f, 0f); // Позиция по центру
    public Vector3 battleCameraOffset = new Vector3(0.7f, 0f, 0f); // Сдвиг вправо (чтобы камера смотрела через левое плечо)
    public float offsetTransitionSpeed = 5f; // Скорость сдвига камеры

    private Vector3 originalLocalPosition;

    private string cameraMode = "freeCamera";

    private CinemachineInputAxisController ciac;
    private CinemachineOrbitalFollow COF;
    private Transform cameraTransform; 

    private int maxCameraRadius = 5;
    private int minCameraRadius = 1;
    private float targetRadius;

    void Start()
    {
        ciac = FreeLookCamera.GetComponent<CinemachineInputAxisController>();
        COF = FreeLookCamera.GetComponent<CinemachineOrbitalFollow>();
        cameraTransform = FreeLookCamera.transform; 
        targetRadius = COF.Radius;
        AimCanvas.SetActive(false);

        // --- ЗАПОМИНАЕМ ПОЗИЦИЮ ---
        if (cameraFollowTarget != null)
        {
            // Запоминаем ту позицию, которую ты настроил в Инспекторе (на уровне головы)
            originalLocalPosition = cameraFollowTarget.localPosition;
        }

        // Запоминаем оригинальные материалы
        if (playerRenderers != null && playerRenderers.Length > 0)
        {
            originalMaterials = new Material[playerRenderers.Length][];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                {
                    originalMaterials[i] = playerRenderers[i].sharedMaterials;
                }
            }
        }
    }

    void Update()
    {
        // 1. ПЕРЕКЛЮЧЕНИЕ РЕЖИМОВ
        if (Input.GetKeyDown(KeyCode.C))
        {
            if (cameraMode == "freeCamera")
            {
                cameraMode = "battleCamera";
                ciac.enabled = true;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                AimCanvas.SetActive(true);
            }
            else
            {
                cameraMode = "freeCamera";
                ciac.enabled = false;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                AimCanvas.SetActive(false);
            }
        }

        // 2. УПРАВЛЕНИЕ ОРИЕНТАЦИЕЙ ИГРОКА
        if (cameraMode == "freeCamera")
        {
            Vector3 viewDir = capsule.position - new Vector3(transform.position.x, capsule.position.y, transform.position.z);
            orientation.forward = viewDir.normalized;

            float horizontalInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");
            Vector3 inputDir = orientation.forward * verticalInput + orientation.right * horizontalInput;

            if (inputDir != Vector3.zero)
            {
                capsule.forward = Vector3.Slerp(capsule.forward, inputDir.normalized, Time.deltaTime * rotationSpeed);
            }

            if (Input.GetKey(KeyCode.Mouse1)) ciac.enabled = true;
            if (Input.GetKeyUp(KeyCode.Mouse1)) ciac.enabled = false;
        }
        else if (cameraMode == "battleCamera")
        {
            Vector3 cameraForward = cameraTransform.forward;
            cameraForward.y = 0;
            cameraForward.Normalize();

            capsule.forward = Vector3.Slerp(capsule.forward, cameraForward, Time.deltaTime * rotationSpeed);
        }

        // 3. ЗУМ
        if (Input.GetAxis("Mouse ScrollWheel") > 0f && targetRadius < maxCameraRadius)
            targetRadius += 0.4f;
        else if (Input.GetAxis("Mouse ScrollWheel") < 0f && targetRadius > minCameraRadius)
            targetRadius -= 0.4f;

        COF.Radius = Mathf.Lerp(COF.Radius, targetRadius, Time.deltaTime * 10f);

        // --- 4. СМЕЩЕНИЕ КАМЕРЫ ---
        if (cameraFollowTarget != null)
        {
            // Если мы в бою, прибавляем сдвиг к стартовой позиции. 
            // Если нет - возвращаемся к стартовой позиции (к голове).
            Vector3 desiredPosition = (cameraMode == "battleCamera") 
                ? originalLocalPosition + battleCameraOffset 
                : originalLocalPosition;
            
            cameraFollowTarget.localPosition = Vector3.Lerp(cameraFollowTarget.localPosition, desiredPosition, Time.deltaTime * offsetTransitionSpeed);
        }

        // 5. ПРОЗРАЧНОСТЬ
        HandlePlayerTransparency();
    }
    private void HandlePlayerTransparency()
    {
        if (playerRenderers == null || originalMaterials == null) return;

        float alpha = Mathf.InverseLerp(fadeEndRadius, fadeStartRadius, COF.Radius);
        alpha = Mathf.Clamp01(alpha);

        if (alpha >= 0.99f && isCurrentlyFading)
        {
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                if (playerRenderers[i] != null)
                    playerRenderers[i].sharedMaterials = originalMaterials[i];
            }
            isCurrentlyFading = false;
            return;
        }

        if (alpha < 0.99f && !isCurrentlyFading)
        {
            if (fadeMaterials != null && fadeMaterials.Length > 0)
            {
                for (int i = 0; i < playerRenderers.Length; i++)
                {
                    if (playerRenderers[i] != null)
                        playerRenderers[i].sharedMaterials = fadeMaterials; 
                }
                isCurrentlyFading = true;
            }
        }

        if (isCurrentlyFading)
        {
            foreach (Renderer r in playerRenderers)
            {
                if (r == null) continue;
                foreach (Material m in r.materials)
                {
                    if (m.HasProperty("_BaseColor"))
                    {
                        Color color = m.GetColor("_BaseColor");
                        color.a = alpha;
                        m.SetColor("_BaseColor", color);
                    }
                    else if (m.HasProperty("_Color"))
                    {
                        Color color = m.color;
                        color.a = alpha;
                        m.color = color;
                    }
                }
            }
        }
    }
}
