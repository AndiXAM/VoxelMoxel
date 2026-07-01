using System.Collections.Generic;
using UnityEngine;

public class SkillTreeUIManager : MonoBehaviour, ISaveable
{
    [Header("Panels")]
    public GameObject menuCanvas;
    public GameObject classGraphPanel;
    public GameObject skillGraphPanel;

    [Header("References")]
    public UITooltip tooltip;
    public ClassData baseClass;

    public Inventory playerInventory; 
    
    // Ссылка на статы игрока (для экипировки классов)
    public StatsContainer playerStats; 

    // Списки разблокированного
    private List<ClassData> unlockedClasses = new List<ClassData>();
    private List<SkillData> unlockedSkills = new List<SkillData>();

    private UITreeNode[] allNodes;

    private void Start()
    {
        // Ищем все кнопки
        allNodes = GetComponentsInChildren<UITreeNode>(true);
        foreach (var node in allNodes)
        {
            node.uiManager = this;
            node.InitializeNode(); 
        }

        if (SaveManager.Instance == null) // выдача базового класса для отладки
        {
            // Даем базовый класс при старте
            if (baseClass != null && !unlockedClasses.Contains(baseClass))
            {
                unlockedClasses.Add(baseClass);
                
                // Сразу надеваем базовый класс, если ничего не надето
                if (playerStats != null && playerStats.equippedClass == null)
                {
                    playerStats.EquipClass(baseClass);
                }
            }
        }
        

        UpdateAllNodesVisuals();
        
        // Выключаем меню
        menuCanvas.SetActive(false);
        if (tooltip != null) tooltip.HideTooltip();
    }

    private void Update()
    {
        // Переключатель TAB
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool isMenuOpen = !menuCanvas.activeSelf;
            menuCanvas.SetActive(isMenuOpen);

            if (isMenuOpen)
            {
                ShowClassGraph();
                UpdateAllNodesVisuals();
            }
            else
            {
                tooltip.HideTooltip();
            }
        }
    }

    // ================= ЛОГИКА КЛАССОВ =================

    public bool IsClassUnlocked(ClassData classData)
    {
        return unlockedClasses.Contains(classData);
    }

    public bool CanUnlockClass(ClassData classData)
    {
        if (classData == null) return false;

        // Если этот класс открывается ТОЛЬКО по квесту - игрок сам его купить не может!
        if (classData.isQuestRewardOnly) 
        {
            return false;
        }

        // Если требуемых классов нет, то можно открыть (например, стартовые)
        if (classData.requiredClasses == null || classData.requiredClasses.Length == 0) 
            return true;

        // Проверяем, открыты ли ВСЕ требуемые классы
        foreach (var req in classData.requiredClasses)
        {
            if (!unlockedClasses.Contains(req))
                return false;
        }
        return true;
    }

    public void OnClassNodeClicked(ClassData classData)
    {
        if (!IsClassUnlocked(classData))
        {
            // 1 КЛИК: ПЫТАЕМСЯ РАЗБЛОКИРОВАТЬ
            if (CanUnlockClass(classData))
            {
                unlockedClasses.Add(classData);
                Debug.Log($"Разблокирован класс: {classData.className}");
            }
            else
            {
                // Добавляем сообщение для ясности
                if (classData.isQuestRewardOnly)
                {
                    Debug.Log($"Класс {classData.className} можно получить только за выполнение задания!");
                }
                else
                {
                    Debug.Log("Не выполнены требования для открытия этого класса.");
                }
            }
        }
        else if (playerStats.equippedClass != classData)
        {
            // 2 КЛИК: НАДЕТЬ (Если уже открыт)
            playerStats.EquipClass(classData);
        }
        else
        {
            // 3 КЛИК: ОТКРЫТЬ НАВЫКИ (Если уже надет)
            ShowSkillGraph(classData);
        }
        
        UpdateAllNodesVisuals();
    }

    // ================= ЛОГИКА НАВЫКОВ =================

    public bool IsSkillUnlocked(SkillData skillData)
    {
        return unlockedSkills.Contains(skillData);
    }

    public bool CanUnlockSkill(SkillData skillData)
    {
        if (skillData == null || skillData.requiredSkills == null) return true;

        foreach (var req in skillData.requiredSkills)
        {
            if (!unlockedSkills.Contains(req))
                return false;
        }
        return true;
    }

    public void OnSkillNodeClicked(SkillData skillData)
    {
        if (IsSkillUnlocked(skillData))
        {
            // --- ЛОГИКА ЭКИПИРОВКИ (2-Й КЛИК) ---
            Debug.Log("Навык уже открыт! Пытаемся добавить в инвентарь...");

            // Если у навыка есть физический предмет и мы подключили инвентарь
            if (skillData.grantedItem != null && playerInventory != null)
            {
                // Проверяем, есть ли уже этот предмет в хотбаре
                if (!playerInventory.HasItem(skillData.grantedItem))
                {
                    // Проверяем, есть ли свободное место (по желанию, если у тебя items.Count ограничен)
                    if (playerInventory.hotbarSlotsCount > playerInventory.GetItemCount())
                    {
                        // Добавляем!
                        playerInventory.AddItem(skillData.grantedItem);
                        Debug.Log($"Навык/Оружие {skillData.grantedItem.itemName} добавлено в хотбар!");
                    }
                }
                else
                {
                    Debug.Log("Этот навык уже находится в хотбаре!");
                }
            }
        }
        else if (CanUnlockSkill(skillData))
        {
            // --- ЛОГИКА РАЗБЛОКИРОВКИ (1-Й КЛИК) ---
            unlockedSkills.Add(skillData);
            Debug.Log($"Навык открыт: {skillData.skillName}");
            UpdateAllNodesVisuals();
        }
    }

    // ================= УПРАВЛЕНИЕ ПАНЕЛЯМИ И ЦВЕТАМИ =================

    public void UpdateAllNodesVisuals()
    {
        foreach (var node in allNodes)
        {
            if (node.classData != null)
            {
                node.UpdateVisuals(IsClassUnlocked(node.classData), CanUnlockClass(node.classData));
            }
            else if (node.skillData != null)
            {
                node.UpdateVisuals(IsSkillUnlocked(node.skillData), CanUnlockSkill(node.skillData));
            }
        }

        if (playerInventory != null)
        {
            bool hasWeaponUnlock = false;
            foreach (var skill in unlockedSkills)
            {
                if (skill != null && skill.unlocksWeaponSlot)
                {
                    hasWeaponUnlock = true;
                    break;
                }
            }
            playerInventory.SetWeaponSlotUnlockState(hasWeaponUnlock);
        }
    }

    public void ShowClassGraph()
    {
        if (tooltip != null) tooltip.HideTooltip();
        classGraphPanel.SetActive(true);
        skillGraphPanel.SetActive(false);
    }

    public void ShowSkillGraph(ClassData classData)
    {
        if (tooltip != null) tooltip.HideTooltip();
        classGraphPanel.SetActive(false);
        skillGraphPanel.SetActive(true);
        
        // Прячем навыки, которые не относятся к выбранному классу
        foreach (var node in allNodes)
        {
            if (node.skillData != null)
            {
                if (classData != null && classData.classSkills.Contains(node.skillData))
                {
                    node.gameObject.SetActive(true); 
                }
                else
                {
                    node.gameObject.SetActive(false); 
                }
            }
        }
    }

    public void UnlockClassFromQuest(ClassData newClass)
    {
        if (!unlockedClasses.Contains(newClass)) unlockedClasses.Add(newClass);
    }

    // --- ИНТЕРФЕЙС СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        data.unlockedClassesNames.Clear();
        foreach (var c in unlockedClasses) data.unlockedClassesNames.Add(c.name);

        data.unlockedSkillsNames.Clear();
        foreach (var s in unlockedSkills) data.unlockedSkillsNames.Add(s.name);
    }

    public void LoadData(SaveData data)
    {
        unlockedClasses.Clear();
        foreach (string cName in data.unlockedClassesNames)
        {
            ClassData loadedClass = SaveManager.Instance.database.GetClassByName(cName);
            if (loadedClass != null) unlockedClasses.Add(loadedClass);
        }

        unlockedSkills.Clear();
        foreach (string sName in data.unlockedSkillsNames)
        {
            SkillData loadedSkill = SaveManager.Instance.database.GetSkillByName(sName);
            if (loadedSkill != null) unlockedSkills.Add(loadedSkill);
        }

        UpdateAllNodesVisuals();
    }
}