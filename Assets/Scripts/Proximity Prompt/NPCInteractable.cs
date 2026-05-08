using UnityEngine;
using System.Collections;

[RequireComponent(typeof(SphereCollider))]
public class NPCInteractable : MonoBehaviour
{
    [Header("Данные для разговора")]
    public DialogueData myDialogue;
    public string actionVerb = "говорить";

    [Header("Настройки поворота")]
    public bool lookAtPlayer = true; 
    public float turnSpeed = 5f;     
    public float rotationOffset = 0f; 

    [Header("Ссылка на Игрока")]
    public Transform playerTarget;

    private bool isPlayerInRange = false;
    private Coroutine lookCoroutine;
    private Quaternion startRotation; 

    private void Start()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        
        // Запоминаем изначальный поворот
        startRotation = transform.rotation; 

        if (playerTarget == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTarget = p.transform;
        }
    }

    private void Update()
    {
        if (!isPlayerInRange) return;

        // Попытка получить ссылку на мозг NPC (безопасный метод)
        NPCBehavior b = GetNPCBehavior();

        // 1. НАЧАЛО ДИАЛОГА
        if (Input.GetKeyDown(KeyCode.E) && !DialogueManager.Instance.isDialogueActive)
        {
            StartInteraction(b);
        }
        
        // 2. КОНЕЦ ДИАЛОГА (когда окно закрылось)
        // Если поведение существует, оно сейчас "говорит", а окно диалога УЖЕ закрыто
        if (b != null && b.isTalking && !DialogueManager.Instance.isDialogueActive)
        {
            EndInteraction(b);
        }
    }

    // --- БЕЗОПАСНЫЙ ПОИСК КОМПОНЕНТА ---
    private NPCBehavior GetNPCBehavior()
    {
        // Ищем на этом же объекте
        NPCBehavior b = GetComponent<NPCBehavior>();
        // Если не нашли - ищем в родителях или детях (на всякий случай)
        if (b == null) b = GetComponentInParent<NPCBehavior>();
        if (b == null) b = GetComponentInChildren<NPCBehavior>();
        return b;
    }

    private void StartInteraction(NPCBehavior b)
    {
        DialogueManager.Instance.StartDialogue(myDialogue);
        if (b != null) b.isTalking = true;

        if (lookAtPlayer && playerTarget != null)
        {
            Vector3 dir = playerTarget.position - transform.position;
            dir.y = 0;
            if (dir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dir.normalized) * Quaternion.Euler(0, rotationOffset, 0);
                if (lookCoroutine != null) StopCoroutine(lookCoroutine);
                lookCoroutine = StartCoroutine(SmoothTurnRoutine(targetRot));
            }
        }
    }

    private void EndInteraction(NPCBehavior b)
    {
        if (b != null) b.isTalking = false;
        
        if (lookAtPlayer)
        {
            if (lookCoroutine != null) StopCoroutine(lookCoroutine);
            // Прямо здесь запускаем возврат к startRotation
            lookCoroutine = StartCoroutine(SmoothTurnRoutine(startRotation));
        }
    }

    private IEnumerator SmoothTurnRoutine(Quaternion targetRot)
    {
        // Пока разница углов больше 1 градуса - крутимся
        while (Quaternion.Angle(transform.rotation, targetRot) > 1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            yield return null; 
        }
        transform.rotation = targetRot;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
            DialogueManager.Instance.ShowPrompt(true, actionVerb);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
            DialogueManager.Instance.ShowPrompt(false);

            if (DialogueManager.Instance.isDialogueActive)
            {
                DialogueManager.Instance.EndDialogue();
            }

            // При уходе обязательно дергаем возврат
            EndInteraction(GetNPCBehavior());
        }
    }
}