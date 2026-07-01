using UnityEngine;
using UnityEngine.UI;

public class EquipmentSlotUI : SlotUI
{
    [Header("Настройки Снаряжения")]
    public EquipmentSlot allowedSlotType; // Какой тип брони разрешен в этом слоте

    [Tooltip("Полупрозрачный силуэт брони на заднем фоне слота")]
    public Image placeholderIcon; 

    // Переопределяем метод отрисовки предмета
    public override void SetItem(InventorySlotData slotData)
    {
        // Вызываем базовую отрисовку иконки предмета и количества из SlotUI
        base.SetItem(slotData);

        // Если слот пустой — показываем силуэт снаряжения. Если занят — скрываем его.
        if (placeholderIcon != null)
        {
            placeholderIcon.enabled = (slotData == null || slotData.IsEmpty);
        }
    }
}