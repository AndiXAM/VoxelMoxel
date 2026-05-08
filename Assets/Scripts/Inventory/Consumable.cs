using UnityEngine;

[CreateAssetMenu(fileName = "New Consumable", menuName = "RPG/Items/Consumable")]
public class Consumable : Item
{
    [Header("Consumable Settings")]
    public float healAmount ; // Сколько лечим
    public float consumeDuration = 1.0f; // Как долго пьем/едим
    public bool destroyOnUse = true; // Исчезает ли после использования?
    

}
