using UnityEngine;

[CreateAssetMenu(fileName = "New Class Reward", menuName = "RPG/Quest/Class Reward")]
public class ClassReward : QuestReward
{
    public ClassData classToUnlock;

    public override void GiveReward()
    {
        SkillTreeUIManager ui = Object.FindFirstObjectByType<SkillTreeUIManager>();
        if (ui != null)
        {
            ui.UnlockClassFromQuest(classToUnlock); 
            Debug.Log($"Награда получена: Доступ к классу {classToUnlock.className}");
        }
    }
}