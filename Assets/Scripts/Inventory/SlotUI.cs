using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro; // Для текста

public class SlotUI : MonoBehaviour, IDropHandler
{
    [Header("Ссылки")]
    public Image selectionBorder;
    public Image itemIcon; 
    public TextMeshProUGUI amountText; // Ссылка на текст с цифрой!
    
    [HideInInspector] public int slotIndex;
    [HideInInspector] public Inventory inventoryManager;

    public void Initialize(int index, Inventory manager)
    {
        slotIndex = index;
        inventoryManager = manager;
    }

    // ТЕПЕРЬ МЫ ПРИНИМАЕМ КОНТЕЙНЕР ДАННЫХ
    public void SetItem(InventorySlotData slotData)
    {
        if (slotData != null && !slotData.IsEmpty)
        {
            if (itemIcon != null)
            {
                itemIcon.enabled = true;
                itemIcon.sprite = slotData.item.icon;
            }

            // Показываем цифру ТОЛЬКО если предмет может стакаться (maxStack > 1) 
            // ИЛИ если ты хочешь показывать цифру всегда, убери это условие.
            if (amountText != null)
            {
                if (slotData.item.maxStackSize > 1 && slotData.amount > 1)
                {
                    amountText.text = slotData.amount.ToString();
                    amountText.enabled = true;
                }
                else
                {
                    amountText.enabled = false;
                }
            }
        }
        else
        {
            // Слот пустой - выключаем всё
            if (itemIcon != null) itemIcon.enabled = false;
            if (amountText != null) amountText.enabled = false;
        }
    }

    public void SetSelected(bool isSelected)
    {
        // selectionBorder - это твоя серая фоновая картинка
        if (selectionBorder != null) 
        {
            selectionBorder.enabled = isSelected;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject draggedObject = eventData.pointerDrag;
        if (draggedObject == null) return;

        DragItemUI draggedItemScript = draggedObject.GetComponent<DragItemUI>();

        if (draggedItemScript != null && inventoryManager != null)
        {
            int fromIndex = draggedItemScript.mySlot.slotIndex;
            int toIndex = this.slotIndex;

            if (fromIndex == toIndex) return;

            inventoryManager.SwapItems(fromIndex, toIndex);
        }
    }
}