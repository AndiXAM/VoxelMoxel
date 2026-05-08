using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ClassStatBonus
{
    public StatType statType;     
    public float value;           
    public StatModType modType;   
}

[CreateAssetMenu(fileName = "New Class", menuName = "RPG/Class Data")]
public class ClassData : ScriptableObject
{
    public string className;
    [TextArea(3, 5)] public string description;

    [Header("Visuals")]
    public Sprite icon;
    public Sprite backgroundImage;
    public Sprite outlineImage;

    [Header("Requirements")]
    public ClassData[] requiredClasses;
    
    // --- НОВОЕ ПОЛЕ ---
    [Tooltip("Если включено, игрок не сможет открыть класс по клику. Только через награду квеста!")]
    public bool isQuestRewardOnly = false;

    [Header("Class Stats Bonuses")]
    public List<ClassStatBonus> statBonuses = new List<ClassStatBonus>();

    [Header("Class Content")]
    public List<SkillData> classSkills = new List<SkillData>(); 
}