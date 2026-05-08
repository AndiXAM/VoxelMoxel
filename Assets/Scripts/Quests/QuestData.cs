using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Quest", menuName = "RPG/Quest/Quest Data")]
public class QuestData : ScriptableObject
{
    public string questName;
    [TextArea(3, 5)] public string description;

    public List<QuestObjective> objectives = new List<QuestObjective>();
    public List<QuestReward> rewards = new List<QuestReward>();
}