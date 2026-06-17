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
        
        // Запоминаем изначальный поворот при старте
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

        // Безопасно находим мозг NPC
        NPCBehavior b = GetNPCBehavior();

        // 1. НАЧАЛО ДИАЛОГА (игрок нажал E)
        if (Input.GetKeyDown(KeyCode.E) && !DialogueManager.Instance.isDialogueActive)
        {
            // Передаем мозг "b", а вторым параметром ничего не пишем (будет использоваться дефолтный диалог)
            StartInteraction(b); 
        }
        
        // 2. КОНЕЦ ДИАЛОГА (окно закрылось)
        if (b != null && b.isTalking && !DialogueManager.Instance.isDialogueActive)
        {
            EndInteraction(b);
        }
    }

    private NPCBehavior GetNPCBehavior()
    {
        NPCBehavior b = GetComponent<NPCBehavior>();
        if (b == null) b = GetComponentInParent<NPCBehavior>();
        if (b == null) b = GetComponentInChildren<NPCBehavior>();
        return b;
    }

    // ИСПРАВЛЕННЫЙ МЕТОД: Принимает мозг "b" И файл диалога "customDialogue"
    public void StartInteraction(NPCBehavior b, DialogueData customDialogue = null)
    {
        // Если передали особый диалог - берем его, иначе берем дефолтный из инспектора
        DialogueData dialogueToUse = (customDialogue != null) ? customDialogue : myDialogue;

        if (dialogueToUse == null) return;

        DialogueManager.Instance.StartDialogue(dialogueToUse, this.transform);
        
        // Помечаем в мозге NPC, что он разговаривает
        if (b != null) b.isTalking = true;

        // Поворот к игроку
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
            lookCoroutine = StartCoroutine(SmoothTurnRoutine(startRotation));
        }
    }

    private IEnumerator SmoothTurnRoutine(Quaternion targetRot)
    {
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

            EndInteraction(GetNPCBehavior());
        }
    }
}