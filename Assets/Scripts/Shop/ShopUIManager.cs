using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ShopUIManager : MonoBehaviour
{
    public static ShopUIManager Instance { get; private set; }

    [Header("Ссылки на UI Панели")]
    public GameObject shopPanel; 
    public TextMeshProUGUI shopTitleText; // Заголовок магазина (например, Smith's Swords)
    public Transform itemsParent;        // Ссылка на Content внутри Scroll Rect
    public GameObject itemButtonPrefab;  // Префаб кнопки товара (ShopItemButtonPrefab)

    [Header("Детали выбранного товара (Справа)")]
    public GameObject detailPanel; // Папка-родитель правых элементов (чтобы скрыть, если пустой магазин)
    public Image detailIcon;
    public TextMeshProUGUI detailName;
    public TextMeshProUGUI detailDescription;
    public Button buyButton;

    [Header("Кнопка выхода")]
    public Button closeButton;

    [Header("Ссылки на Системы")]
    public Inventory playerInventory; // Ссылка на инвентарь игрока для передачи предметов

    [Header("Настройки Валюты")]
    public MiscItem coinAsset;

    private List<GameObject> spawnedButtons = new List<GameObject>();
    private ShopItem selectedItem;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Изначально скрываем магазин и правое окно деталей
        if (shopPanel != null) shopPanel.SetActive(false);
        if (detailPanel != null) detailPanel.SetActive(false);

        // Подписываемся на события кнопок
        if (closeButton != null) closeButton.onClick.AddListener(CloseShop);
        if (buyButton != null) buyButton.onClick.AddListener(BuySelectedItem);
    }

    private void Start()
    {
        // Попробуем автоматически найти инвентарь игрока на сцене, если забыли указать его в инспекторе
        if (playerInventory == null)
        {
            playerInventory = FindFirstObjectByType<Inventory>();
        }
    }

    // Вызывается автоматически при завершении диалога с NPC
    public void OpenShop(ShopData shopData)
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(true);
            
            if (shopTitleText != null) shopTitleText.text = shopData.shopName;

            // Очищаем старые кнопки перед созданием новых
            ClearShopList();

            // Создаем кнопки для каждого товара
            foreach (var item in shopData.goods)
            {
                GameObject btnObj = Instantiate(itemButtonPrefab, itemsParent);
                ShopItemButton btnScript = btnObj.GetComponent<ShopItemButton>();
                
                if (btnScript != null)
                {
                    btnScript.Setup(item, this);
                }
                
                spawnedButtons.Add(btnObj);
            }

            //  Вместо выбора первого элемента сбрасываем правую панель в пустое состояние ---
            ResetDetailsPanel();
        }
    }

    // Вызывается при нажатии на кнопку товара слева
     public void SelectShopItem(ShopItem item)
    {
        selectedItem = item;

        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailIcon != null)
        {
            detailIcon.sprite = item.icon;
            detailIcon.color = Color.white; // Возвращаем нормальный цвет иконке
        }
        
        if (detailName != null) detailName.text = item.itemName;
        if (detailDescription != null) detailDescription.text = item.description;

        // Показываем кнопку покупки, так как элемент выбран
        if (buyButton != null) buyButton.gameObject.SetActive(true); 
    }

    // Вызывается при нажатии на кнопку «Buy»
    // Вызывается при нажатии на кнопку «Buy»
    private void BuySelectedItem()
    {
        if (selectedItem == null) return;

        if (playerInventory == null)
        {
            Debug.LogError("[МАГАЗИН] Ссылка на Inventory игрока не найдена!");
            return;
        }

        if (coinAsset == null)
        {
            Debug.LogError("[МАГАЗИН] В инспекторе ShopUIManager не назначен Coin Asset!");
            return;
        }

        if (selectedItem.itemAsset == null)
        {
            Debug.LogWarning($"[МАГАЗИН] У товара {selectedItem.itemName} отсутствует ссылка на Item Asset!");
            return;
        }

        // Шаг 1. Считаем, сколько монет у игрока во всем инвентаре
        int playerCoins = playerInventory.GetItemTotalCount(coinAsset);
        int price = selectedItem.price;

        // Шаг 2. Проверяем, хватает ли монет
        if (playerCoins >= price)
        {
            // Шаг 3. Списываем монеты
            if (playerInventory.RemoveItemTotal(coinAsset, price))
            {
                // Шаг 4. Выдаем купленный предмет игроку
                playerInventory.AddItem(selectedItem.itemAsset, 1);
                Debug.Log($"[МАГАЗИН] Успешная покупка: {selectedItem.itemName} за {price} монет.");
            }
        }
        else
        {
            Debug.LogWarning($"[МАГАЗИН] Недостаточно монет для покупки! Нужно: {price}, у вас: {playerCoins}");
        }
    }

    public void ResetDetailsPanel()
    {
        selectedItem = null;

        // Панель деталей активна, но её элементы очищены
        if (detailPanel != null) detailPanel.SetActive(true);

        if (detailIcon != null)
        {
            detailIcon.sprite = null; // Очищаем картинку
            detailIcon.color = new Color(0.5f, 0.5f, 0.5f, 1f); // Устанавливаем серый цвет подложки
        }

        if (detailName != null) detailName.text = "";
        if (detailDescription != null) detailDescription.text = "";

        // Скрываем кнопку покупки, пока ничего не выбрано
        if (buyButton != null) buyButton.gameObject.SetActive(false); 
    }

    private void ClearShopList()
    {
        foreach (var btn in spawnedButtons)
        {
            Destroy(btn);
        }
        spawnedButtons.Clear();
        selectedItem = null;
    }

    public void CloseShop()
    {
        if (shopPanel != null)
        {
            shopPanel.SetActive(false);
        }
    }
}