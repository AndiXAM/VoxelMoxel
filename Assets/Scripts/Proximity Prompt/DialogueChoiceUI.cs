using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueChoiceUI : MonoBehaviour
{
    public TextMeshProUGUI choiceText;
    private Button button;
    
    private DialogueChoice currentChoice;
    private DialogueData currentDialogue; // Вернули ссылку на файл диалога

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnChoiceClicked);
    }

    public void Setup(DialogueChoice choice, DialogueData dialogue)
    {
        currentChoice = choice;
        currentDialogue = dialogue; // Запоминаем файл диалога
        choiceText.text = choice.choiceText;
    }

    private void OnChoiceClicked()
    {
        // 1. Отрабатываем квестовые события (если они включены именно в этом ОТВЕТЕ)
        if (currentDialogue != null && currentDialogue.quest != null && QuestManager.Instance != null)
        {
            string qName = currentDialogue.quest.questName;

            if (currentChoice.takeQuest && !QuestManager.Instance.HasActiveQuest(qName) && !QuestManager.Instance.IsQuestCompleted(qName))
            {
                QuestManager.Instance.AcceptQuest(currentDialogue.quest);
            }
            
            if (currentChoice.turnInQuest && QuestManager.Instance.HasActiveQuest(qName) && QuestManager.Instance.IsQuestReadyToTurnIn(qName))
            {
                QuestManager.Instance.TurnInQuest(qName);
            }
        }

        // 2. Прыгаем к следующему узлу (или выходим, если -1)
        DialogueManager.Instance.JumpToNode(currentChoice.nextNodeIndex);
    }
}