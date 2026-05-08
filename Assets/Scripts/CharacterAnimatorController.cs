using UnityEngine;

public class CharacterAnimatorController : MonoBehaviour
{
    [Header("References")]
    public Transform capsule;
    private CharacterController characterController;
    private Animator characterAnimation;
    private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip footstepSound;
    
    // Внутренние переменные
    private float jumpCooldown = 0f;
    private bool isRunMode = false; 
    private float lastGroundedTime = 0f;

    void Start()
    {
        characterController = capsule.GetComponent<CharacterController>();
        characterAnimation = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    void Update()
    {
        if (characterController.isGrounded) lastGroundedTime = Time.time;
        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        bool hasInput = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));
        
        // Твоя логика переключателя бега
        if (Input.GetKeyDown(KeyCode.LeftControl)) isRunMode = !isRunMode;

        // Отправка в Аниматор
        characterAnimation.SetBool("IsMoving", hasInput);
        characterAnimation.SetBool("Run", isRunMode);
    }

    private void HandleJump()
    {
        characterAnimation.SetBool("IsGrounded", characterController.isGrounded);
        jumpCooldown -= Time.deltaTime;
        
        // Логика Coyote Time для прыжка
        bool wasGroundedRecently = (Time.time - lastGroundedTime) < 0.15f;

        if (Input.GetKeyDown(KeyCode.Space) && wasGroundedRecently && jumpCooldown <= 0)
        {
            characterAnimation.SetTrigger("Jump");
            if(jumpSound) audioSource.PlayOneShot(jumpSound);
            lastGroundedTime = -100f; 
            jumpCooldown = 0.2f; 
        }
    }

    // --- НОВЫЙ МЕТОД ДЛЯ ANIMATION EVENTS ---
    // Этот метод будет вызываться из скрипта AnimationEventHandler, который висит на модели
    public void PlayFootstepSound()
    {
        // Проверяем, на земле ли мы (Coyote Time), чтобы не шагать в воздухе
        bool isStableGrounded = (Time.time - lastGroundedTime) < 0.2f;

        if (isStableGrounded && footstepSound != null && audioSource != null)
        {
            // Слегка меняем высоту звука (pitch) для реалистичности
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(footstepSound);
        }
    }

    public void StartDeath() 
    {
        if (characterAnimation != null)
            characterAnimation.SetBool("Death", true);
        else
            Debug.LogError("Animator is missing on " + gameObject.name);
    }

    public void StartRespawn() 
    {
        characterAnimation.SetBool("Death", false);
    }
}