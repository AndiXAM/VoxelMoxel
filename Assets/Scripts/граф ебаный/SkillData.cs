using UnityEngine;

[CreateAssetMenu(fileName = "New Skill", menuName = "RPG/Skill Data")]
public class SkillData : ScriptableObject
{
    public string skillName;
    [TextArea(3, 5)] public string description;
    public bool isPassive;

    [Header("Visuals (Картинки)")]
    public Sprite icon;
    public Sprite backgroundImage;
    public Sprite outlineImage;

    [Header("Requirements")]
    public SkillData[] requiredSkills; 

    // --- НОВОЕ ПОЛЕ ---
    [Header("Gameplay (Экипировка)")]
    [Tooltip("Какой предмет добавится в инвентарь при экипировке этого навыка?")]
    public Item grantedItem; 
}