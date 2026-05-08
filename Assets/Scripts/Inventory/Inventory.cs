using UnityEngine;

// (Сюда вставляем класс InventorySlotData из Шага 2, если еще не вставил)

public class Inventory : MonoBehaviour, ISaveable
{
    [Header("Настройки Размера")]
    public int hotbarSlotsCount = 9;      
    public int mainInventoryRows = 3;     
    public int mainInventoryColumns = 9;  
    
    public int TotalSlots => hotbarSlotsCount + (mainInventoryRows * mainInventoryColumns);
    public int selectedSlotIndex = 0;
    
    [Header("UI Ссылки")]
    public GameObject inventoryWindow;    
    public Transform hotbarParent;        
    public Transform mainInventoryParent; 
    public GameObject slotPrefab;
    
    [Header("Ссылки на Системы")]
    public PlayerCombat playerCombat;
    public PlayerConsumables playerConsumables;

    [Header("Тестовые предметы")]
    public Item TESTWEAPON; 
    public Item TESTCONS;

    // --- ВАЖНОЕ ИЗМЕНЕНИЕ ЗДЕСЬ ---
    public InventorySlotData[] slotsData; // Массив КОНТЕЙНЕРОВ
    private SlotUI[] uiSlots;

    private void Start()
    {
        slotsData = new InventorySlotData[TotalSlots];
        uiSlots = new SlotUI[TotalSlots];

        for (int i = 0; i < slotsData.Length; i++)
        {
            slotsData[i] = new InventorySlotData(null, 0);
        }

        if (inventoryWindow != null) inventoryWindow.SetActive(false);

        // 1. СНАЧАЛА СОЗДАЕМ СЛОТЫ (КРИТИЧЕСКИ ВАЖНО!)
        InitializeSlots(); 
        
        // 2. ТОЛЬКО ПОТОМ ДОБАВЛЯЕМ ПРЕДМЕТЫ
        if (TESTWEAPON != null) AddItem(TESTWEAPON, 1);
        if (TESTCONS != null) AddItem(TESTCONS, 5); 
        
        UpdateUI();
        UpdateSelectedWeapon();
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < TotalSlots; i++)
        {
            Transform parentToUse = (i < hotbarSlotsCount) ? hotbarParent : mainInventoryParent;

            // Если родителя нет - выдаем ошибку, чтобы сразу найти причину
            if (parentToUse == null)
            {
                Debug.LogError($"[ИНВЕНТАРЬ] Не назначен Hotbar Parent или Main Inventory Parent в инспекторе!");
                return;
            }

            GameObject slotObj = Instantiate(slotPrefab, parentToUse);
            SlotUI slot = slotObj.GetComponent<SlotUI>();
            
            slot.Initialize(i, this);
            
            // ВАЖНО: Записываем созданный слот в массив!
            uiSlots[i] = slot; 
        }
    }

    private void Update()
    {
        HandleSlotSelection();
        HandleInventoryToggle();
    }

    private void HandleInventoryToggle()
    {
        // Открытие/Закрытие инвентаря на кнопку I
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryWindow != null)
            {
                bool isOpen = !inventoryWindow.activeSelf;
                inventoryWindow.SetActive(isOpen);
                
                // (Опционально) Если хочешь, чтобы при открытом инвентаре мышка освобождалась,
                // а камера останавливалась, здесь нужно менять Cursor.lockState
            }
        }
    }

    private void HandleSlotSelection()
    {
        int previousSlot = selectedSlotIndex;
        
        // Цифры 1-9 переключают ТОЛЬКО слоты хотбара (индексы от 0 до 8)
        for (int i = 0; i < hotbarSlotsCount; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                selectedSlotIndex = i;
            }
        }
        
        if (previousSlot != selectedSlotIndex)
        {
            UpdateUISelection();
            UpdateSelectedWeapon();
        }
    }


    // --- ЛОГИКА DRAG AND DROP ---
    public void SwapItems(int fromIndex, int toIndex)
    {
        InventorySlotData fromSlot = slotsData[fromIndex];
        InventorySlotData toSlot = slotsData[toIndex];

        // ЛОГИКА СЛИЯНИЯ СТАКОВ (Если перетащили зелье на зелье)
        if (!fromSlot.IsEmpty && !toSlot.IsEmpty && fromSlot.item.itemName == toSlot.item.itemName)
        {
            if (toSlot.item.maxStackSize > 1)
            {
                int spaceLeft = toSlot.item.maxStackSize - toSlot.amount;
                if (spaceLeft > 0)
                {
                    int amountToMove = Mathf.Min(spaceLeft, fromSlot.amount);
                    toSlot.AddAmount(amountToMove);
                    fromSlot.RemoveAmount(amountToMove);
                    
                    UpdateUI();
                    if (fromIndex == selectedSlotIndex || toIndex == selectedSlotIndex) UpdateSelectedWeapon();
                    return; // Успешно слили, выходим
                }
            }
        }

        // Если предметы разные или стак полон - ПРОСТО МЕНЯЕМ МЕСТАМИ
        InventorySlotData temp = new InventorySlotData(slotsData[toIndex].item, slotsData[toIndex].amount);
        
        slotsData[toIndex].item = slotsData[fromIndex].item;
        slotsData[toIndex].amount = slotsData[fromIndex].amount;
        
        slotsData[fromIndex].item = temp.item;
        slotsData[fromIndex].amount = temp.amount;

        UpdateUI();

        if (fromIndex == selectedSlotIndex || toIndex == selectedSlotIndex)
        {
            UpdateSelectedWeapon();
        }
    }

    // --- ЛОГИКА ДОБАВЛЕНИЯ ПРЕДМЕТОВ ---
    public void AddItem(Item newItem, int amountToAdd = 1)
    {
        if (newItem == null) return;

        int amountLeft = amountToAdd;

        // 1. Пытаемся найти неполный стак ТАКОГО ЖЕ предмета
        if (newItem.maxStackSize > 1)
        {
            for (int i = 0; i < slotsData.Length; i++)
            {
                if (!slotsData[i].IsEmpty && slotsData[i].item.itemName == newItem.itemName)
                {
                    int spaceLeft = newItem.maxStackSize - slotsData[i].amount;
                    if (spaceLeft > 0)
                    {
                        int amountToPush = Mathf.Min(spaceLeft, amountLeft);
                        slotsData[i].AddAmount(amountToPush);
                        amountLeft -= amountToPush;

                        if (amountLeft <= 0) // Все поместилось!
                        {
                            UpdateUI();
                            return;
                        }
                    }
                }
            }
        }

        // 2. Если остались предметы (или они не стакаются) - ищем ПУСТЫЕ слоты
        while (amountLeft > 0)
        {
            int emptyIndex = -1;
            for (int i = 0; i < slotsData.Length; i++)
            {
                if (slotsData[i].IsEmpty)
                {
                    emptyIndex = i;
                    break;
                }
            }

            if (emptyIndex != -1)
            {
                int amountToPush = Mathf.Min(newItem.maxStackSize, amountLeft);
                slotsData[emptyIndex].item = newItem;
                slotsData[emptyIndex].amount = amountToPush;
                amountLeft -= amountToPush;
            }
            else
            {
                Debug.LogWarning("Инвентарь полон! Не влезло: " + amountLeft);
                break; // Мест нет, выходим из цикла
            }
        }

        UpdateUI();
        UpdateSelectedWeapon();
    }

    // --- УДАЛЕНИЕ (Для расходников) ---
    public void RemoveCurrentItem(int amountToRemove = 1)
    {
        if (!slotsData[selectedSlotIndex].IsEmpty)
        {
            slotsData[selectedSlotIndex].RemoveAmount(amountToRemove);
            
            UpdateUI();
            
            if (slotsData[selectedSlotIndex].IsEmpty)
            {
                playerCombat.ClearWeapon();
                if (playerConsumables != null) playerConsumables.ClearHand();
            }
            UpdateSelectedWeapon(); 
        }
    }

    public void RemoveItemAtSlot(int slotIndex, int amountToRemove = 1)
    {
        if (slotIndex >= 0 && slotIndex < slotsData.Length)
        {
            if (!slotsData[slotIndex].IsEmpty)
            {
                slotsData[slotIndex].RemoveAmount(amountToRemove); 
                UpdateUI();              
                
                if (slotIndex == selectedSlotIndex && slotsData[slotIndex].IsEmpty)
                {
                    playerCombat.ClearWeapon();
                    if (playerConsumables != null) playerConsumables.ClearHand();
                    UpdateSelectedWeapon(); 
                }
            }
        }
    }

    // --- ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ---
    public bool HasItem(Item itemToCheck)
    {
        if (itemToCheck == null) return false;
        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty && slotsData[i].item.itemName == itemToCheck.itemName) return true; 
        }
        return false;
    }

    public int GetItemCount()
    {
        int count = 0;
        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty) count++;
        }
        return count; // Считает занятые СЛОТЫ, а не общее количество предметов
    }

    public Item GetSelectedItem()
    {
        return slotsData[selectedSlotIndex].IsEmpty ? null : slotsData[selectedSlotIndex].item;
    }

    private void UpdateSelectedWeapon()
    {
        Item currentItem = GetSelectedItem();

        if (currentItem is Weapon weapon)
        {
            if(playerConsumables != null) playerConsumables.ClearHand();
            playerCombat.EquipWeapon(weapon);
        }
        else if (currentItem is Consumable consumable)
        {
            playerCombat.ClearWeapon();
            if(playerConsumables != null) playerConsumables.EquipConsumable(consumable);
        }
        else
        {
            playerCombat.ClearWeapon();
            if(playerConsumables != null) playerConsumables.ClearHand();
        }
    }

    public void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            uiSlots[i].SetItem(slotsData[i]); // ПЕРЕДАЕМ КОНТЕЙНЕР!
        }
        UpdateUISelection();
    }

    private void UpdateUISelection()
    {
        if (uiSlots == null) return;

        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] != null)
            {
                // Передаем true ТОЛЬКО если индекс слота (i) совпадает с выбранным (selectedSlotIndex)
                uiSlots[i].SetSelected(i == selectedSlotIndex);
            }
        }
    }

    // --- ИНТЕРФЕЙС СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        data.selectedSlotIndex = this.selectedSlotIndex;

        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty)
            {
                data.inventoryItemNames[i] = slotsData[i].item.name; // Имя файла (ScriptableObject)
                data.inventoryItemAmounts[i] = slotsData[i].amount;
            }
            else
            {
                data.inventoryItemNames[i] = "";
                data.inventoryItemAmounts[i] = 0;
            }
        }
    }

    public void LoadData(SaveData data)
    {
        this.selectedSlotIndex = data.selectedSlotIndex;

        for (int i = 0; i < data.inventoryItemNames.Length; i++)
        {
            if (!string.IsNullOrEmpty(data.inventoryItemNames[i]))
            {
                // БЕРЕМ ИЗ БАЗЫ ДАННЫХ!
                Item loadedItem = SaveManager.Instance.database.GetItemByName(data.inventoryItemNames[i]);
                
                if (loadedItem != null) slotsData[i] = new InventorySlotData(loadedItem, data.inventoryItemAmounts[i]);
                else slotsData[i] = new InventorySlotData(null, 0);
            }
            else
            {
                slotsData[i] = new InventorySlotData(null, 0);
            }
        }

        UpdateUI();
        UpdateSelectedWeapon(); 
    }
}