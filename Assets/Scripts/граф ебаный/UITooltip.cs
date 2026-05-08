using UnityEngine;
using TMPro;
using Unity.VisualScripting; // Используем TextMeshPro для красивого текста

public class UITooltip : MonoBehaviour
{
    public GameObject tooltipPanel;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI statusText;

    private RectTransform TransformtooltipPanel;

    public Vector2 offset = new Vector2(20f, -20f);
    private void Start()
    {
        HideTooltip(); // Прячем при старте
        TransformtooltipPanel = tooltipPanel.GetComponent<RectTransform>();
        TransformtooltipPanel.anchoredPosition = new Vector2(0f, -0f);
    }


    private void Update()
    {
         TransformtooltipPanel = tooltipPanel.GetComponent<RectTransform>();
        Vector2 mousePosition = Input.mousePosition;
        RectTransform parentRect = TransformtooltipPanel.parent as RectTransform;

        if (parentRect == null)
        {
            Debug.LogError("У панели нет родителя с RectTransform!");
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect,
            mousePosition,
            null, // для Screen Space - Overlay; для Camera передайте камеру Canvas
            out Vector2 localPoint))
        {
            // Устанавливаем позицию с учётом смещения
            TransformtooltipPanel.anchoredPosition = localPoint + offset;
        }
    }

    public void ShowTooltip(string itemName, string desc, bool isUnlocked, bool canUnlock)
    {
        tooltipPanel.SetActive(true);
        nameText.text = itemName;
        descriptionText.text = desc;

        if (isUnlocked)
        {
            statusText.text = "Unlocked";
        }
        else if (canUnlock)
        {
            statusText.text = "Can be unlocked";
        }
        else
        {
            statusText.text = "Locked";
        }
    }

    public void HideTooltip()
    {
        tooltipPanel.SetActive(false);
    }

    private void OnDisable()
    {
    // Перемещаем панель далеко за пределы экрана (например, в левый нижний угол)
    // Предполагаем, что tooltipPanel – это RectTransform панели
    if (TransformtooltipPanel != null)
        {
            // Координаты за экраном (например, -5000 по X и Y)
            TransformtooltipPanel.anchoredPosition = new Vector2(-5000f, -5000f);
        }
    }
}