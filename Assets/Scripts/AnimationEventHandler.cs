using UnityEngine;

// Этот скрипт вешается на тот же объект, где находится Animator!
public class AnimationEventHandler : MonoBehaviour
{
    // Ссылка на наш главный контроллер анимаций (лежит на капсуле)
    public CharacterAnimatorController mainController;

    // Этот метод будет вызываться ИЗ АНИМАЦИИ
    public void PlayFootstep()
    {
        if (mainController != null)
        {
            mainController.PlayFootstepSound(); // Вызываем метод в главном скрипте
        }
    }
}