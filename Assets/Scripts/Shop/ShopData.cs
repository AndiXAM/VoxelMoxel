using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ShopItem
{
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public Sprite icon;
    public int price;
    public Item itemAsset; // Ссылка на ваш базовый ScriptableObject предмета (Item.cs)
}

[CreateAssetMenu(fileName = "New Shop", menuName = "RPG/Shop Data")]
public class ShopData : ScriptableObject
{
    public string shopName = "Торговец";
    public List<ShopItem> goods = new List<ShopItem>();
}