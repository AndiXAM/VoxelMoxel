using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Equipment", menuName = "RPG/Items/Equipment")]
public class Equipment : Item
{
    [Header("Настройки Снаряжения")]
    public EquipmentSlot slotType; // Куда надевается предмет

    [Header("Модификаторы Характеристик")]
    [Tooltip("Бонусы к характеристикам при экипировке этого предмета (сюда же добавляем Armor)")]
    public List<EquipmentStatBonus> statModifiers = new List<EquipmentStatBonus>();
}