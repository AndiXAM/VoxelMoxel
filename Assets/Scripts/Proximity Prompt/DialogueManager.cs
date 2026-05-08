using UnityEngine;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI Панели")]
    public GameObject promptPanel;   
    public GameObject dialoguePanel; 

    [Header("UI Текст")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI promptText; 

    [Header("Выборы")]
    public Transform choicesParent;      
    public GameObject choicePrefab;      

    [Header("Audio")]
    public AudioSource audioSource; // Один общий источник на Canvas

    // Внутренние настройки текущего "голоса"
    private float currentSpeed;
    private AudioClip currentSound;
    private float currentPitch;
    
    private Coroutine typingCoroutine;
    private bool isTyping = false;
    private string fullCurrentText;

    private DialogueData currentDialogue;
    private DialogueNode currentNode;
    private int currentNodeIndex = 0;
    
    [HideInInspector] public bool isDialogueActive = false;
    private List<GameObject> activeChoiceButtons = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        promptPanel.SetActive(false);
        dialoguePanel.SetActive(false);
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Update()
    {
        if (isDialogueActive && activeChoiceButtons.Count == 0 && Input.GetMouseButtonDown(0))
        {
            if (isTyping) FinishTypingImmediately();
            else
            {
                if (currentNode != null && currentNode.forceEndDialogue) EndDialogue();
                else JumpToNode(currentNodeIndex + 1);
            }
        }
    }

    public void ShowPrompt(bool show, string actionName = "talk")
    {
        if (isDialogueActive && show) return;
        promptPanel.SetActive(show);
        if (show && promptText != null) promptText.text = $"Press [E] to {actionName}";
    }

    public void StartDialogue(DialogueData newDialogue)
    {
        currentDialogue = newDialogue;
        isDialogueActive = true;

        // --- ПОДТЯГИВАЕМ ГОЛОС ИМЕННО ЭТОГО NPC ---
        currentSpeed = currentDialogue.typingSpeed;
        currentSound = currentDialogue.typingSound;
        currentPitch = currentDialogue.voicePitch;

        promptPanel.SetActive(false);
        dialoguePanel.SetActive(true);
        nameText.text = currentDialogue.npcName;

        DetermineStartNode();
        JumpToNode(currentNodeIndex);
    }

    private void DetermineStartNode()
    {
        if (currentDialogue.quest == null || QuestManager.Instance == null)
        {
            currentNodeIndex = currentDialogue.startNodeIndex;
            return;
        }

        string qName = currentDialogue.quest.questName;
        if (QuestManager.Instance.IsQuestCompleted(qName)) currentNodeIndex = currentDialogue.afterQuestNodeIndex;
        else if (QuestManager.Instance.HasActiveQuest(qName))
        {
            if (QuestManager.Instance.IsQuestReadyToTurnIn(qName)) currentNodeIndex = currentDialogue.completedNodeIndex;
            else currentNodeIndex = currentDialogue.inProgressNodeIndex;
        }
        else currentNodeIndex = currentDialogue.startNodeIndex;
    }

    public void JumpToNode(int nodeIndex)
    {
        ClearChoices();
        if (currentDialogue == null || nodeIndex < 0 || nodeIndex >= currentDialogue.nodes.Count)
        {
            EndDialogue();
            return;
        }

        currentNodeIndex = nodeIndex;
        currentNode = currentDialogue.nodes[nodeIndex];

        // Подготовка к печати
        fullCurrentText = currentNode.npcText;
        dialogueText.text = "";

        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeLineRoutine());

        ProcessNodeEvents(currentNode);
    }

    private IEnumerator TypeLineRoutine()
    {
        isTyping = true;

        foreach (char c in fullCurrentText.ToCharArray())
        {
            dialogueText.text += c;

            // Проигрываем индивидуальный звук
            if (c != ' ' && currentSound != null)
            {
                // Настраиваем высоту голоса этого NPC
                // Добавляем чуть-чуть рандома, чтобы голос звучал живее
                audioSource.pitch = currentPitch + Random.Range(-0.05f, 0.05f);
                audioSource.PlayOneShot(currentSound);
            }

            yield return new WaitForSeconds(currentSpeed);
        }

        OnTypingFinished();
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        dialogueText.text = fullCurrentText;
        OnTypingFinished();
    }

    private void OnTypingFinished()
    {
        isTyping = false;
        if (currentNode != null && currentNode.choices.Count > 0)
        {
            foreach (var choice in currentNode.choices)
            {
                GameObject btnObj = Instantiate(choicePrefab, choicesParent);
                DialogueChoiceUI choiceUI = btnObj.GetComponent<DialogueChoiceUI>();
                choiceUI.Setup(choice, currentDialogue);
                activeChoiceButtons.Add(btnObj);
            }
        }
    }

    private void ProcessNodeEvents(DialogueNode node)
    {
        if (currentDialogue.quest == null || QuestManager.Instance == null) return;
        string qName = currentDialogue.quest.questName;

        if (node.takeQuest && !QuestManager.Instance.HasActiveQuest(qName) && !QuestManager.Instance.IsQuestCompleted(qName))
            QuestManager.Instance.AcceptQuest(currentDialogue.quest);
        
        if (node.turnInQuest && QuestManager.Instance.HasActiveQuest(qName) && QuestManager.Instance.IsQuestReadyToTurnIn(qName))
            QuestManager.Instance.TurnInQuest(qName);
    }

    private void ClearChoices()
    {
        foreach (var btn in activeChoiceButtons) Destroy(btn);
        activeChoiceButtons.Clear();
    }

    public void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        ClearChoices();
        currentDialogue = null;
    }
}