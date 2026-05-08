using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UITreeNode : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Данные (Назначь что-то одно)")]
    public ClassData classData;
    public SkillData skillData;

    // Скрытые ссылки
    private Image backgroundImage;
    private Image iconImage;
    private Image outlineImage;

    [HideInInspector] public SkillTreeUIManager uiManager;

    public void InitializeNode()
    {
        // 1. АВТОМАТИЧЕСКИЙ ПОИСК КОМПОНЕНТОВ
        Transform bgTransform = transform.Find("Background");
        if (bgTransform != null) backgroundImage = bgTransform.GetComponent<Image>();

        Transform iconTransform = transform.Find("Icon");
        if (iconTransform != null) iconImage = iconTransform.GetComponent<Image>();

        Transform outlineTransform = transform.Find("Outline");
        if (outlineTransform != null) outlineImage = outlineTransform.GetComponent<Image>();

        // 2. ПРИМЕНЕНИЕ КАРТИНОК ИЗ ДАННЫХ
        if (classData != null)
        {
            ApplySprite(backgroundImage, classData.backgroundImage);
            ApplySprite(iconImage, classData.icon);
            ApplySprite(outlineImage, classData.outlineImage);
        }
        else if (skillData != null)
        {
            ApplySprite(backgroundImage, skillData.backgroundImage);
            ApplySprite(iconImage, skillData.icon);
            ApplySprite(outlineImage, skillData.outlineImage);
        }
        else
        {
            Debug.LogWarning($"Узел {gameObject.name} пустой! Назначь ему ClassData или SkillData.");
        }
    }

    // НОВЫЙ МЕТОД: Умно ставит картинку или прячет белый квадрат
    private void ApplySprite(Image imgComponent, Sprite spriteToApply)
    {
        if (imgComponent == null) return; // Если такого объекта вообще нет на кнопке - игнорируем

        if (spriteToApply != null)
        {
            imgComponent.sprite = spriteToApply;
            imgComponent.enabled = true; // Включаем видимость
        }
        else
        {
            imgComponent.sprite = null;
            imgComponent.enabled = false; // Выключаем видимость (прячем белый квадрат)
        }
    }

    // --- Обработка мыши ---
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (uiManager == null) return;

        if (classData != null)
        {
            uiManager.tooltip.ShowTooltip(classData.className, classData.description, uiManager.IsClassUnlocked(classData), uiManager.CanUnlockClass(classData));
        }
        else if (skillData != null)
        {
            string type = skillData.isPassive ? "(Passive)" : "(Active)";
            uiManager.tooltip.ShowTooltip($"{skillData.skillName}\n<size=80%>{type}</size>", skillData.description, uiManager.IsSkillUnlocked(skillData), uiManager.CanUnlockSkill(skillData));
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (uiManager != null) uiManager.tooltip.HideTooltip();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (uiManager == null) return;
        
        if (classData != null) 
        {
            uiManager.OnClassNodeClicked(classData);
        }
        else if (skillData != null) 
        {
            uiManager.tooltip.HideTooltip();
            uiManager.OnSkillNodeClicked(skillData);
        }
    }

    // --- Обновление цветов (заблокировано/открыто) ---
    public void UpdateVisuals(bool isUnlocked, bool canUnlock)
    {
        Color mainColor = (isUnlocked || canUnlock) ? Color.white : new Color(0.3f, 0.3f, 0.3f);
        
        if (iconImage != null) iconImage.color = mainColor;
        if (backgroundImage != null) backgroundImage.color = mainColor;

        if (outlineImage != null)
        {
            // Проверяем, надет ли этот класс сейчас?
            bool isEquipped = (classData != null && uiManager.playerStats != null && uiManager.playerStats.equippedClass == classData);

            if (isEquipped) 
            {
                outlineImage.color = Color.cyan; // НАДЕТО (Голубой цвет, выделяется)
            }
            else if (isUnlocked) 
            {
                outlineImage.color = Color.white; // ПРОСТО ОТКРЫТО
            }
            else if (canUnlock) 
            {
                outlineImage.color = Color.yellow; // ДОСТУПНО
            }
            else 
            {
                outlineImage.color = new Color(0.3f, 0.3f, 0.3f); // ЗАКРЫТО
            }
        }
    }
}