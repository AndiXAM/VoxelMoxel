using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Image))]
public class DragItemUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public SlotUI mySlot; // Ссылка на слот, в котором мы лежим
    
    private Image myImage;
    private GameObject dragGhost; // "Призрак", который летает за мышкой

    private void Awake()
    {
        myImage = GetComponent<Image>();
        mySlot = GetComponentInParent<SlotUI>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // 1. Защита: Если в слоте ничего нет, тащить нельзя!
        if (myImage.sprite == null || !myImage.enabled)
        {
            eventData.pointerDrag = null; // Отменяем перетаскивание
            return;
        }

        // 2. Создаем "Призрака" (копию картинки) на самом верхнем слое Canvas
        Canvas canvas = GetComponentInParent<Canvas>();
        dragGhost = new GameObject("DragGhost");
        dragGhost.transform.SetParent(canvas.transform, false);
        dragGhost.transform.SetAsLastSibling(); // Поверх всего

        // 3. Настраиваем картинку Призрака
        Image ghostImage = dragGhost.AddComponent<Image>();
        ghostImage.sprite = myImage.sprite;
        ghostImage.raycastTarget = false; // ВАЖНО: Призрак не должен блокировать мышку!
        
        // Делаем призрака того же размера, что и оригинал
        RectTransform ghostRect = dragGhost.GetComponent<RectTransform>();
        ghostRect.sizeDelta = GetComponent<RectTransform>().sizeDelta;

        // 4. Прячем ОРИГИНАЛЬНУЮ картинку в слоте (делаем прозрачной)
        myImage.color = new Color(1, 1, 1, 0.5f); // Полупрозрачная, чтобы было видно, откуда тащим
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Призрак следует за мышкой
        if (dragGhost != null)
        {
            dragGhost.transform.position = Input.mousePosition;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        // 1. Уничтожаем Призрака в любом случае
        if (dragGhost != null)
        {
            Destroy(dragGhost);
        }

        // 2. Возвращаем оригинальной картинке нормальный цвет
        myImage.color = Color.white;

        // (Сама логика обмена предметами произойдет в SlotUI.OnDrop)
    }
}