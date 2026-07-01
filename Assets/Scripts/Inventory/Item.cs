using UnityEngine;
public enum EquipmentSlot
{
    Helmet,
    Chestplate,
    Boots,
    Weapon 
}

[System.Serializable]
public class InventorySlotData
{
    public Item item;   // Какой предмет лежит в слоте
    public int amount;  // Сколько штук

    // Конструктор
    public InventorySlotData(Item item, int amount)
    {
        this.item = item;
        this.amount = amount;
    }

    public void AddAmount(int value)
    {
        amount += value;
    }

    public void RemoveAmount(int value)
    {
        amount -= value;
        if (amount <= 0)
        {
            item = null;
            amount = 0;
        }
    }
    
    public bool IsEmpty => item == null || amount <= 0;
}


public abstract class Item : ScriptableObject 
{
    public string itemName;
    public Sprite icon;
    public GameObject prefab; 
    
    [Header("Настройки стаков")]
    [Tooltip("Максимальное количество предметов в одном слоте (1 = не стакается)")]
    public int maxStackSize = 1; 
}