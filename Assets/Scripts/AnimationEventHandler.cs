using UnityEngine;

// Этот скрипт должен висеть на том же объекте, что и компонент Animator (на 3D-модели)
[RequireComponent(typeof(Animator))]
public class AnimationEventHandler : MonoBehaviour
{
    [Header("Настройки шагов")]
    [Tooltip("Звук, который будет играть при касании земли")]
    public AudioClip footstepSound;
    
    // Ссылка на источник звука (будет искать автоматически)
    private AudioSource audioSource;

    private void Awake()
    {
        // Пытаемся найти AudioSource на самой модели
        audioSource = GetComponent<AudioSource>();
        
        // Если на модели его нет, ищем на родительском объекте (капсуле)
        if (audioSource == null) 
        {
            audioSource = GetComponentInParent<AudioSource>();
        }

        // Если вообще нигде нет - выводим предупреждение (чтобы не было крашей)
        if (audioSource == null)
        {
            Debug.LogWarning($"[AnimationEvent] На объекте {gameObject.name} или его родителях не найден AudioSource для шагов!");
        }
    }

    // ЭТОТ МЕТОД ВЫЗЫВАЕТСЯ АНИМАТОРОМ ИЗ FBX!
    // Важно: Имя метода должно в точности совпадать с тем, что написано в Event'ах анимации!
    public void PlayFootstep()
    {
        if (footstepSound != null && audioSource != null)
        {
            // Слегка меняем высоту звука (pitch) для реалистичности
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.PlayOneShot(footstepSound);
        }
    }
    
    // Если у тебя были другие эвенты (например PlayJump), добавь их сюда так же:
    /*
    public AudioClip jumpSound;
    public void PlayJump()
    {
        if (jumpSound != null && audioSource != null) audioSource.PlayOneShot(jumpSound);
    }
    */
}