using UnityEngine;
using System.Collections.Generic;

public class QuestManager : MonoBehaviour, ISaveable
{
    public static QuestManager Instance { get; private set; }

    // Класс-обертка для хранения прогресса активного квеста
    [System.Serializable]
    public class ActiveQuest
    {
        public QuestData questData;
        // Словарь для хранения прогресса по каждой цели (Цель -> Количество)
        public Dictionary<QuestObjective, int> progress = new Dictionary<QuestObjective, int>();

        public ActiveQuest(QuestData data)
        {
            questData = data;
            // Инициализируем прогресс нулями
            foreach (var obj in questData.objectives)
            {
                progress.Add(obj, 0);
            }
        }

        public bool IsComplete()
        {
            foreach (var obj in questData.objectives)
            {
                if (obj is KillObjective killObj)
                {
                    // Если текущий прогресс меньше требуемого - квест еще не готов
                    if (progress[obj] < killObj.requiredAmount) return false;
                }
                // (Здесь можно добавить проверки для других типов целей)
            }
            return true;
        }
    }

    public List<ActiveQuest> activeQuests = new List<ActiveQuest>();
    public List<QuestData> completedQuests = new List<QuestData>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject); // Исправил на Destroy(gameObject)
    }

    public void AcceptQuest(QuestData quest)
    {
        // Проверяем, не брали ли мы его уже
        if (!HasActiveQuest(quest.questName) && !IsQuestCompleted(quest.questName))
        {
            ActiveQuest newQuest = new ActiveQuest(quest);
            activeQuests.Add(newQuest);
            Debug.Log($"Квест принят: {quest.questName}");
        }
    }

    // Отправка событий (Убийство, Сбор)
    public void OnEventOccurred(object eventData)
    {
        // Проходим с конца, чтобы можно было безопасно удалять
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            ActiveQuest activeQ = activeQuests[i];

            // Проверяем каждую цель в этом квесте
            foreach (var objective in activeQ.questData.objectives)
            {
                // Если это цель на убийство
                if (objective is KillObjective killObj)
                {
                    // ИСПРАВЛЕНИЕ: Проверяем, что событие это EnemyData и оно совпадает с целью квеста!
                    if (eventData is EnemyData killedEnemy && killedEnemy == killObj.targetEnemy)
                    {
                        activeQ.progress[objective]++;
                        Debug.Log($"Прогресс квеста '{activeQ.questData.questName}': убито {activeQ.progress[objective]} / {killObj.requiredAmount}");
                    }
                }
            }
            
            // Заметь: здесь БОЛЬШЕ НЕТ вызова CompleteQuest(activeQ), 
            // так как теперь квесты сдаются НПС через диалог!
        }
    }

    private void CompleteQuest(ActiveQuest activeQ)
    {
        activeQuests.Remove(activeQ);
        completedQuests.Add(activeQ.questData);

        Debug.Log($"КВЕСТ ВЫПОЛНЕН: {activeQ.questData.questName}!");

        foreach (var reward in activeQ.questData.rewards)
        {
            reward.GiveReward();
        }
    }

    public bool HasActiveQuest(string questName)
    {
        return activeQuests.Exists(q => q.questData.questName == questName);
    }

    public bool IsQuestCompleted(string questName)
    {
        return completedQuests.Exists(q => q.questName == questName);
    }

    // Метод проверяет, выполнены ли все условия квеста (но он еще не сдан НПС)
    public bool IsQuestReadyToTurnIn(string questName)
    {
        ActiveQuest q = activeQuests.Find(x => x.questData.questName == questName);
        if (q != null)
        {
            return q.IsComplete();
        }
        return false;
    }

    // Вызывается Менеджером Диалогов, когда НПС забирает квест и дает награду
    public void TurnInQuest(string questName)
    {
        ActiveQuest q = activeQuests.Find(x => x.questData.questName == questName);
        if (q != null && q.IsComplete())
        {
            CompleteQuest(q); // Этот метод уже есть в твоем коде (выдает награды и переносит в completed)
        }
    }

    // --- ИНТЕРФЕЙС СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        // Сохраняем выполненные
        data.completedQuestsNames.Clear();
        foreach (var q in completedQuests) data.completedQuestsNames.Add(q.name);

        // Сохраняем активные (с прогрессом)
        data.activeQuestsData.Clear();
        foreach (var activeQ in activeQuests)
        {
            ActiveQuestSave qSave = new ActiveQuestSave();
            qSave.questName = activeQ.questData.name;
            
            // Превращаем прогресс из словаря в массив чисел
            List<int> progressList = new List<int>();
            foreach (var val in activeQ.progress.Values) progressList.Add(val);
            qSave.progressValues = progressList.ToArray();

            data.activeQuestsData.Add(qSave);
        }
    }

    public void LoadData(SaveData data)
    {
        // Загружаем выполненные
        completedQuests.Clear();
        foreach (var name in data.completedQuestsNames)
        {
            QuestData q = SaveManager.Instance.database.GetQuestByName(name);
            if (q != null) completedQuests.Add(q);
        }

        // Загружаем активные
        activeQuests.Clear();
        foreach (var qSave in data.activeQuestsData)
        {
            QuestData questAsset = SaveManager.Instance.database.GetQuestByName(qSave.questName);
            if (questAsset != null)
            {
                ActiveQuest activeQ = new ActiveQuest(questAsset);
                
                // Восстанавливаем числа прогресса в словарь
                // (Важно, чтобы порядок целей в QuestData не менялся)
                int i = 0;
                var keys = new List<QuestObjective>(activeQ.progress.Keys);
                foreach (var key in keys)
                {
                    if (i < qSave.progressValues.Length)
                        activeQ.progress[key] = qSave.progressValues[i];
                    i++;
                }
                activeQuests.Add(activeQ);
            }
        }
    }
}