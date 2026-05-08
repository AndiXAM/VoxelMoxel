using UnityEngine;

[CreateAssetMenu(fileName = "New Kill Objective", menuName = "RPG/Quest/Kill Objective")]
public class KillObjective : QuestObjective
{
    [Tooltip("Какой именно тип врага нужно убить?")]
    public EnemyData targetEnemy; // <--- ТЕПЕРЬ МЫ ИЩЕМ ФАЙЛ, А НЕ ТЕКСТ!
    
    public int requiredAmount = 1;
}