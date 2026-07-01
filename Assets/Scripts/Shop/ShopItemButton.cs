using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ShopItemButton : MonoBehaviour
{
    [Header("UI Ссылки")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI priceText;

    private Button button;
    private ShopItem currentItem;
    private ShopUIManager shopManager;

    private void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.AddListener(OnButtonClicked);
        }
    }

    // Заполнение данных кнопки
    public void Setup(ShopItem item, ShopUIManager manager)
    {
        currentItem = item;
        shopManager = manager;

        if (iconImage != null) iconImage.sprite = item.icon;
        if (nameText != null) nameText.text = item.itemName;
        
        // Форматируем цену (добавляем разделитель тысяч, например 2,400g)
        if (priceText != null) priceText.text = $"{item.price:N0}g"; 
    }

    private void OnButtonClicked()
    {
        if (shopManager != null && currentItem != null)
        {
            // Передаем менеджеру информацию о том, что этот товар выбран
            shopManager.SelectShopItem(currentItem);
        }
    }
}