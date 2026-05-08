using UnityEngine;

[CreateAssetMenu(fileName = "New Item Reward", menuName = "RPG/Quest/Item Reward")]
public class ItemReward : QuestReward
{
    public Item itemToGive;
    
    public override void GiveReward()
    {
        Inventory inv = Object.FindFirstObjectByType<Inventory>(); 
        if (inv != null) inv.AddItem(itemToGive);
        Debug.Log($"Награда получена: Предмет {itemToGive.itemName}");
    }
}