using UnityEngine;
using UnityEngine.AI; // Заготовка на будущее для ходьбы

public class NPCBehavior : MonoBehaviour
{
    public enum NPCState
    {
        Idle,           // Просто стоит
        Patrolling,     // Ходит по маршруту (в будущем)
        Talking         // Разговаривает с игроком (замирает и смотрит)
    }

    [Header("Настройки")]
    public NPCState currentState = NPCState.Idle;

    [Header("Ссылки")]
    public Animator animator;

    // Внутренние переменные
    [HideInInspector] public Quaternion originalRotation; 
    
    [HideInInspector] public bool isTalking = false;

    private void Start()
    {
        if (animator == null) animator = GetComponent<Animator>();
        // Больше ничего тут не делаем!
    }
    private void Update()
    {
        // Если NPC разговаривает, он не должен патрулировать или менять анимации
        if (isTalking) return;

        switch (currentState)
        {
            case NPCState.Idle:
                HandleIdle();
                break;
            case NPCState.Patrolling:
                HandlePatrolling();
                break;
        }
    }

    private void HandleIdle()
    {
        // Говорим Аниматору стоять на месте (те же параметры, что у игрока)
        if (animator != null)
        {
            animator.SetBool("IsMoving", false);
            animator.SetBool("Run", false);
        }
    }

    private void HandlePatrolling()
    {
        // В будущем здесь будет логика NavMeshAgent
        // agent.SetDestination(waypoint);
        // animator.SetBool("IsMoving", true);
    }
}