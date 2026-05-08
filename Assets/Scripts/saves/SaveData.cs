using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ActiveQuestSave
{
    public string questName;
    public int[] progressValues; // Сохраним числа из словаря просто массивом
}


[System.Serializable]
public class SaveData
{
    public string saveName;
    public string lastSceneName;
    public string lastPlayDate;

    // --- ФИЗИКА И СТАТЫ ---
    public float[] playerPosition = new float[3];
    public int currentHealth;
    
    // Имя файла ScriptableObject (Класса)
    public string equippedClassName; 

    // --- ИНВЕНТАРЬ ---
    // Для каждого из 36 слотов храним Имя предмета и Количество
    public string[] inventoryItemNames = new string[36]; 
    public int[] inventoryItemAmounts = new int[36];
    public int selectedSlotIndex;

    // --- ПРОГРЕССИЯ ---
    public List<string> unlockedClassesNames = new List<string>();
    public List<string> unlockedSkillsNames = new List<string>();
    public List<string> completedQuestsNames = new List<string>();

    public List<ActiveQuestSave> activeQuestsData = new List<ActiveQuestSave>();

    

    public SaveData()
    {
        saveName = "New Save";
        lastSceneName = "GameScene"; 
        lastPlayDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        // Массивы уже инициализированы нулями в объявлении выше
    }
}