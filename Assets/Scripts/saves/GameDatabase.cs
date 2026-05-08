using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "GameDatabase", menuName = "RPG/Game Database")]
public class GameDatabase : ScriptableObject
{
    [Header("Все предметы в игре")]
    public List<Item> allItems = new List<Item>();

    [Header("Все классы в игре")]
    public List<ClassData> allClasses = new List<ClassData>();

    [Header("Все навыки в игре")]
    public List<SkillData> allSkills = new List<SkillData>();

    [Header("Все квесты в игре")]
    public List<QuestData> allQuests = new List<QuestData>();

    // --- МЕТОДЫ ДЛЯ БЫСТРОГО ПОИСКА ---

    public Item GetItemByName(string itemName)
    {
        return allItems.Find(x => x != null && x.name == itemName); // Ищем по имени файла!
    }

    public ClassData GetClassByName(string className)
    {
        return allClasses.Find(x => x != null && x.name == className);
    }

    public SkillData GetSkillByName(string skillName)
    {
        return allSkills.Find(x => x != null && x.name == skillName);
    }

    public QuestData GetQuestByName(string questName)
    {
        return allQuests.Find(x => x != null && x.name == questName);
    }
}