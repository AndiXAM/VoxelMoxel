using UnityEngine;

public class Inventory : MonoBehaviour, ISaveable
{
    [Header("Настройки Размера")]
    public int hotbarSlotsCount = 9;      
    public int mainInventoryRows = 3;     
    public int mainInventoryColumns = 9;  
    
    public int TotalSlots => hotbarSlotsCount + (mainInventoryRows * mainInventoryColumns);
    
    // Снаряжение занимает еще 4 слота в конце массива slotsData
    public int TotalSlotsWithEquipment => TotalSlots + 4;
    public int selectedSlotIndex = 0;
    
    [Header("UI Ссылки")]
    public GameObject inventoryWindow;    
    public Transform hotbarParent;        
    public Transform mainInventoryParent; 
    public GameObject slotPrefab;

    [Header("Слоты Снаряжения на UI")]
    public EquipmentSlotUI helmetSlotUI;
    public EquipmentSlotUI chestplateSlotUI;
    public EquipmentSlotUI bootsSlotUI;
    public EquipmentSlotUI weaponSlotUI; // Ссылка на новый слот оружия в инвентаре UI
    [HideInInspector] public bool isWeaponSlotUnlocked = false; // Открыт ли слот пассивкой?
    
    [Header("Ссылки на Системы")]
    public PlayerCombat playerCombat;
    public PlayerConsumables playerConsumables;
    public StatsContainer statsContainer; // Ссылка на характеристики для наложения бонусов брони

    public PlayerSkillHandler playerSkills;

    [Header("Тестовые предметы")]
    public Item TESTWEAPON; 
    public Item TESTCONS;

    public InventorySlotData[] slotsData; // Массив контейнеров (теперь включает снаряжение)
    private SlotUI[] uiSlots;

    // Переменные для отслеживания текущего надетого снаряжения (чтобы вовремя снимать баффы)
    private Equipment equippedHelmet;
    private Equipment equippedChestplate;
    private Equipment equippedBoots;
    private Weapon currentVisualWeaponInHand; // Память: какой меч сейчас физически в руке
    

    private void Start()
    {
        // Выделяем память с учетом 3 слотов снаряжения
        slotsData = new InventorySlotData[TotalSlotsWithEquipment];
        uiSlots = new SlotUI[TotalSlotsWithEquipment];

        for (int i = 0; i < slotsData.Length; i++)
        {
            slotsData[i] = new InventorySlotData(null, 0);
        }

        if (inventoryWindow != null) inventoryWindow.SetActive(false);

        InitializeSlots(); 
        UpdateUI();
        UpdateSelectedWeapon();
    }

    private void InitializeSlots()
    {
        // 1. Создаем стандартные ячейки хотбара и инвентаря
        for (int i = 0; i < TotalSlots; i++)
        {
            Transform parentToUse = (i < hotbarSlotsCount) ? hotbarParent : mainInventoryParent;

            if (parentToUse == null)
            {
                Debug.LogError($"[ИНВЕНТАРЬ] Не назначен Hotbar Parent или Main Inventory Parent в инспекторе!");
                return;
            }

            GameObject slotObj = Instantiate(slotPrefab, parentToUse);
            SlotUI slot = slotObj.GetComponent<SlotUI>();
            
            slot.Initialize(i, this);
            uiSlots[i] = slot; 
        }

        // 2. Инициализируем статичные слоты снаряжения, которые мы настроили в UI вручную
        if (helmetSlotUI != null)
        {
            helmetSlotUI.Initialize(TotalSlots, this); // Индекс: TotalSlots
            uiSlots[TotalSlots] = helmetSlotUI;
        }
        if (chestplateSlotUI != null)
        {
            chestplateSlotUI.Initialize(TotalSlots + 1, this); // Индекс: TotalSlots + 1
            uiSlots[TotalSlots + 1] = chestplateSlotUI;
        }
        if (bootsSlotUI != null)
        {
            bootsSlotUI.Initialize(TotalSlots + 2, this); // Индекс: TotalSlots + 2
            uiSlots[TotalSlots + 2] = bootsSlotUI;
        }
        if (weaponSlotUI != null)
        {
            weaponSlotUI.Initialize(TotalSlots + 3, this); // Индекс: TotalSlots + 3
            uiSlots[TotalSlots + 3] = weaponSlotUI;
            weaponSlotUI.gameObject.SetActive(isWeaponSlotUnlocked); // Скрываем/показываем на UI
        }
    }

    private void Update()
    {
        HandleSlotSelection();
        HandleInventoryToggle();
    }

    private void HandleInventoryToggle()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (inventoryWindow != null)
            {
                bool isOpen = !inventoryWindow.activeSelf;
                inventoryWindow.SetActive(isOpen);
            }
        }
    }

    private void HandleSlotSelection()
    {
        int previousSlot = selectedSlotIndex;
        
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

    // --- ВАЛИДАЦИЯ ПЕРЕМЕЩЕНИЯ ПРЕДМЕТОВ ---
    private bool CanPlaceInSlot(Item item, int targetSlotIndex)
    {
        // Пустоту всегда можно положить (освободить слот)
        if (item == null) return true;

        // Если это ячейка шлема
        if (targetSlotIndex == TotalSlots)
        {
            return item is Equipment equip && equip.slotType == EquipmentSlot.Helmet;
        }
        // Если это ячейка нагрудника
        if (targetSlotIndex == TotalSlots + 1)
        {
            return item is Equipment equip && equip.slotType == EquipmentSlot.Chestplate;
        }
        // Если это ячейка ботинок
        if (targetSlotIndex == TotalSlots + 2)
        {
            return item is Equipment equip && equip.slotType == EquipmentSlot.Boots;
        }
         // Если это ячейка оружия класса
        if (targetSlotIndex == TotalSlots + 3)
        {
            if (!isWeaponSlotUnlocked) return false; // Слот закрыт пассивкой!
            return item is Weapon; // Класть можно только оружие
        }
        // В любые обычные ячейки (хотбар и инвентарь) можно класть абсолютно всё
        return true;
    }

    // --- ЛОГИКА DRAG AND DROP ---
    public void SwapItems(int fromIndex, int toIndex)
    {
        InventorySlotData fromSlot = slotsData[fromIndex];
        InventorySlotData toSlot = slotsData[toIndex];

        // ВАЛИДАЦИЯ: Проверяем, соответствуют ли предметы типам слотов снаряжения
        if (!CanPlaceInSlot(fromSlot.item, toIndex) || !CanPlaceInSlot(toSlot.item, fromIndex))
        {
            Debug.LogWarning("[ИНВЕНТАРЬ] Неподходящий тип снаряжения для этого слота!");
            return; // Отменяем перемещение
        }

        // Логика слияния стаков
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
                    return; 
                }
            }
        }

        // Смена предметов местами
        InventorySlotData temp = new InventorySlotData(slotsData[toIndex].item, slotsData[toIndex].amount);
        
        slotsData[toIndex].item = slotsData[fromIndex].item;
        slotsData[toIndex].amount = slotsData[fromIndex].amount;
        
        slotsData[fromIndex].item = temp.item;
        slotsData[fromIndex].amount = temp.amount;

        // Если в перемещении участвовали слоты снаряжения, пересчитываем характеристики персонажа
        if (fromIndex >= TotalSlots || toIndex >= TotalSlots)
        {
            UpdateEquipmentStats();
            
            // Если изменилось оружие в экипировке класса, обновляем визуал оружия в руках
            if (fromIndex == TotalSlots + 3 || toIndex == TotalSlots + 3)
            {
                UpdateSelectedWeapon();
            }
        }

        UpdateUI();

        if (fromIndex == selectedSlotIndex || toIndex == selectedSlotIndex)
        {
            UpdateSelectedWeapon();
        }
    }

    // --- ПЕРЕСЧЕТ ХАРАКТЕРИСТИК ОТ БРОНИ ---
    private void UpdateEquipmentStats()
    {
        if (statsContainer == null) return;

        // 1. Снимаем старые бонусы
        RemoveEquipmentModifiers(equippedHelmet);
        RemoveEquipmentModifiers(equippedChestplate);
        RemoveEquipmentModifiers(equippedBoots);

        // 2. Получаем новые ссылки на надетое снаряжение
        equippedHelmet = slotsData[TotalSlots].item as Equipment;
        equippedChestplate = slotsData[TotalSlots + 1].item as Equipment;
        equippedBoots = slotsData[TotalSlots + 2].item as Equipment;

        // 3. Накладываем новые бонусы характеристик
        ApplyEquipmentModifiers(equippedHelmet);
        ApplyEquipmentModifiers(equippedChestplate);
        ApplyEquipmentModifiers(equippedBoots);
    }

    private void ApplyEquipmentModifiers(Equipment equip)
    {
        if (equip == null || statsContainer == null) return;

        // Просто проходим по списку и накладываем все статы (включая броню)
        foreach (var modifier in equip.statModifiers)
        {
            Stat stat = statsContainer.GetStat(modifier.statType);
            if (stat != null)
            {
                stat.AddModifier(new StatModifier(modifier.value, modifier.modType, equip));
            }
        }
    }


    private void RemoveEquipmentModifiers(Equipment equip)
    {
        if (equip == null || statsContainer == null) return;

        // Удаляем все модификаторы по источнику предмета снаряжения
        foreach (var modifier in equip.statModifiers)
        {
            Stat stat = statsContainer.GetStat(modifier.statType);
            if (stat != null)
            {
                stat.RemoveAllModifiersFromSource(equip);
            }
        }
    }

    public void AddItem(Item newItem, int amountToAdd = 1)
    {
        if (newItem == null) return;

        int amountLeft = amountToAdd;

        if (newItem.maxStackSize > 1)
        {
            // Пытаемся найти неполный стак (проверяем только стандартные слоты инвентаря, исключая снаряжение!)
            for (int i = 0; i < TotalSlots; i++)
            {
                if (!slotsData[i].IsEmpty && slotsData[i].item.itemName == newItem.itemName)
                {
                    int spaceLeft = newItem.maxStackSize - slotsData[i].amount;
                    if (spaceLeft > 0)
                    {
                        int amountToPush = Mathf.Min(spaceLeft, amountLeft);
                        slotsData[i].AddAmount(amountToPush);
                        amountLeft -= amountToPush;

                        if (amountLeft <= 0) 
                        {
                            UpdateUI();
                            return;
                        }
                    }
                }
            }
        }

        while (amountLeft > 0)
        {
            int emptyIndex = -1;
            // Ищем свободные ячейки только в обычном инвентаре (не в снаряжении)
            for (int i = 0; i < TotalSlots; i++)
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
                break; 
            }
        }

        UpdateUI();
        UpdateSelectedWeapon();
    }

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
                
                if (slotIndex >= TotalSlots)
                {
                    UpdateEquipmentStats();
                }
                
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
        for (int i = 0; i < TotalSlots; i++) // Считаем только заполненные ячейки основного инвентаря
        {
            if (!slotsData[i].IsEmpty) count++;
        }
        return count; 
    }

    public Item GetSelectedItem()
    {
        return slotsData[selectedSlotIndex].IsEmpty ? null : slotsData[selectedSlotIndex].item;
    }

    private void UpdateSelectedWeapon()
    {
        Item currentItem = GetSelectedItem();
        Weapon neededVisualWeapon = GetVisualWeaponForItem(currentItem);

        // Проверяем: изменилась ли сама 3D-модель меча в руках?
        bool visualWeaponChanged = (neededVisualWeapon != currentVisualWeaponInHand);

        if (currentItem is Weapon weapon)
        {
            if (playerConsumables != null) playerConsumables.ClearHand();
            if (playerSkills != null) playerSkills.ClearSkillHand(visualWeaponChanged);
            playerCombat.EquipWeapon(weapon, visualWeaponChanged);
        }
        else if (currentItem is Consumable consumable)
        {
            playerCombat.ClearWeapon();
            if (playerSkills != null) playerSkills.ClearSkillHand(true);
            if (playerConsumables != null) playerConsumables.EquipConsumable(consumable);
        }
        else if (currentItem is SkillItem skillItem)
        {
            playerCombat.ClearWeapon(visualWeaponChanged);
            if (playerConsumables != null) playerConsumables.ClearHand();
            if (playerSkills != null) playerSkills.EquipSkill(skillItem, visualWeaponChanged);
        }
        else
        {
            playerCombat.ClearWeapon();
            if (playerConsumables != null) playerConsumables.ClearHand();
            if (playerSkills != null) playerSkills.ClearSkillHand(true);
        }

        currentVisualWeaponInHand = neededVisualWeapon;
    }

    public void UpdateUI()
    {
        for (int i = 0; i < uiSlots.Length; i++)
        {
            if (uiSlots[i] != null)
            {
                uiSlots[i].SetItem(slotsData[i]); 
            }
        }
        UpdateUISelection();
    }

    private void UpdateUISelection()
    {
        if (uiSlots == null) return;

        for (int i = 0; i < TotalSlots; i++) // Выделение рамкой работает только для хотбара/инвентаря
        {
            if (uiSlots[i] != null)
            {
                uiSlots[i].SetSelected(i == selectedSlotIndex);
            }
        }
    }

    // --- СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        data.selectedSlotIndex = this.selectedSlotIndex;

        // Внимание: Массивы сохранения автоматически запишут и слоты снаряжения, так как slotsData.Length теперь равен TotalSlots + 3!
        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty)
            {
                data.inventoryItemNames[i] = slotsData[i].item.name; 
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
            if (i >= slotsData.Length) break; // Защита на случай несовпадения версий сейвов

            if (!string.IsNullOrEmpty(data.inventoryItemNames[i]))
            {
                Item loadedItem = SaveManager.Instance.database.GetItemByName(data.inventoryItemNames[i]);
                
                if (loadedItem != null) slotsData[i] = new InventorySlotData(loadedItem, data.inventoryItemAmounts[i]);
                else slotsData[i] = new InventorySlotData(null, 0);
            }
            else
            {
                slotsData[i] = new InventorySlotData(null, 0);
            }
        }

        // Пересчитываем баффы статов на случай, если при загрузке на персонаже было снаряжение
        UpdateEquipmentStats();

        UpdateUI();
        UpdateSelectedWeapon(); 
    }

    // 1. Возвращает суммарное количество конкретного предмета во всех слотах инвентаря
    public int GetItemTotalCount(Item itemToCheck)
    {
        if (itemToCheck == null) return 0;
        
        int total = 0;
        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty && slotsData[i].item.itemName == itemToCheck.itemName)
            {
                total += slotsData[i].amount;
            }
        }
        return total;
    }

    // 2. Списывает определенное количество предметов из инвентаря (поддерживает списывание из разных стаков)
    // Возвращает true, если списание прошло успешно, и false, если предметов не хватило.
    public bool RemoveItemTotal(Item itemToRemove, int amountToRemove)
    {
        if (itemToRemove == null || amountToRemove <= 0) return false;

        // Проверяем, хватает ли вообще предметов перед началом списания
        int totalHas = GetItemTotalCount(itemToRemove);
        if (totalHas < amountToRemove) return false;

        int leftToRemove = amountToRemove;

        // Постепенно забираем предметы из заполненных слотов
        for (int i = 0; i < slotsData.Length; i++)
        {
            if (!slotsData[i].IsEmpty && slotsData[i].item.itemName == itemToRemove.itemName)
            {
                if (slotsData[i].amount > leftToRemove)
                {
                    // В этом слоте предметов больше, чем осталось списать
                    slotsData[i].RemoveAmount(leftToRemove);
                    leftToRemove = 0;
                    break; // Списание завершено
                }
                else
                {
                    // Забираем весь этот стак целиком и ищем дальше
                    leftToRemove -= slotsData[i].amount;
                    slotsData[i].RemoveAmount(slotsData[i].amount);
                }
            }
        }

        // Обновляем визуальное отображение
        UpdateEquipmentStats();
        UpdateUI();
        UpdateSelectedWeapon();

        return true;
    }
    public void SetWeaponSlotUnlockState(bool unlocked)
    {
        isWeaponSlotUnlocked = unlocked;
        if (weaponSlotUI != null)
        {
            weaponSlotUI.gameObject.SetActive(unlocked);
        }
    }

    // Безопасное получение оружия из слота класса
    public Weapon GetEquippedWeaponInSlot()
    {
        if (slotsData == null || slotsData.Length <= TotalSlots + 3) return null;
        return slotsData[TotalSlots + 3].item as Weapon;
    }

    private Weapon GetVisualWeaponForItem(Item item)
    {
        if (item is Weapon w)
        {
            if (w.isSkillWeapon) return GetEquippedWeaponInSlot();
            return w;
        }
        if (item is SkillItem s && s.requiresEquippedWeapon)
        {
            return GetEquippedWeaponInSlot();
        }
        return null; // Для зелий, пустых слотов и магии без меча
    }
}