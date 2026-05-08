using UnityEngine;

public class CharacterAnimatorController1 : MonoBehaviour
{
    [Header("References")]
    public Transform capsule;
    private CharacterController characterController;
    private Animator characterAnimation;
    private AudioSource audioSource;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip jumpSound;
    [SerializeField] private AudioClip footstepSound;
    
    [Header("Settings")]
    [SerializeField] private float walkStepDelay;
    [SerializeField] private float runStepDelay;

    // Внутренние переменные
    private float footstepTimer = 0f;
    private float jumpCooldown = 0f;
    private bool isRunMode = false; 

    // НОВАЯ ПЕРЕМЕННАЯ: Время, когда мы последний раз касались земли
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
        // 1. Обновляем таймер "памяти о земле"
        if (characterController.isGrounded)
        {
            lastGroundedTime = Time.time;
        }

        HandleMovement();
        HandleJump();
    }

    private void HandleMovement()
    {
        bool hasInput = (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.D));
        
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isRunMode = !isRunMode;
        }

        characterAnimation.SetBool("IsMoving", hasInput);
        characterAnimation.SetBool("Run", isRunMode);

        if (hasInput && characterController.isGrounded)
        {
            footstepTimer -= Time.deltaTime;
            float currentDelay = isRunMode ? runStepDelay : walkStepDelay;

            if (footstepTimer <= 0)
            {
                if(footstepSound) audioSource.PlayOneShot(footstepSound);
                footstepTimer = currentDelay;
            }
        }
        else
        {
            if (!hasInput) footstepTimer = 0.1f;
        }
    }

    private void HandleJump()
    {
        characterAnimation.SetBool("IsGrounded", characterController.isGrounded);
        jumpCooldown -= Time.deltaTime;

        // ИСПРАВЛЕННОЕ УСЛОВИЕ:
        // Мы проверяем не "стоим ли мы сейчас", а "были ли мы на земле последние 0.15 сек"
        // Это позволяет анимации сработать, даже если физика уже подкинула нас вверх
        bool wasGroundedRecently = (Time.time - lastGroundedTime) < 0.15f;

        if (Input.GetKeyDown(KeyCode.Space) && wasGroundedRecently && jumpCooldown <= 0)
        {
            characterAnimation.SetTrigger("Jump");
            if(jumpSound) audioSource.PlayOneShot(jumpSound);
            
            // Сбрасываем таймер земли, чтобы нельзя было прыгнуть дважды в воздухе
            lastGroundedTime = -100f; 
            
            jumpCooldown = 0.2f; 
        }
    }
    public void StartDeath() {
        characterAnimation.SetBool("Death", true);
    }

    public void StartRespawn() {
        characterAnimation.SetBool("Death", false);
    }
}


