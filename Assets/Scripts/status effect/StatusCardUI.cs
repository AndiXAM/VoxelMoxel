using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class StatusCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Компоненты")]
    public Image background;
    public Image icon;
    public Image cooldownSweep; // Затемнение (как в скиллах доты)
    public TextMeshProUGUI stacksText;

    private ActiveStatusEffect myEffect;

    // Всплывающее окно
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipName;
    public TextMeshProUGUI tooltipDesc;
    
    [Header("Настройки Тултипа")]
    public float tooltipOffsetLeft = 200f; // Смещение влево от мыши

    public void Setup(ActiveStatusEffect effect)
    {
        myEffect = effect;
        icon.sprite = effect.data.icon;
        if (effect.data.background != null) background.sprite = effect.data.background;
        background.color = effect.data.colorTint;

        // Показываем цифру стаков, только если их больше 1
        if (effect.currentStacks > 1)
        {
            stacksText.text = effect.currentStacks.ToString();
            stacksText.enabled = true;
        }
        else
        {
            stacksText.enabled = false;
        }

        tooltipPanel.SetActive(false);
    }

    private void Update()
    {
        if (myEffect != null && cooldownSweep != null)
        {
            // Круговое затемнение, показывающее оставшееся время
            cooldownSweep.fillAmount = myEffect.timeRemaining / myEffect.data.duration;
        }

        // Если тултип включен, заставляем его висеть слева от мыши
        if (tooltipPanel.activeSelf)
        {
            Vector2 mousePos = Input.mousePosition;
            tooltipPanel.transform.position = mousePos + new Vector2(-tooltipOffsetLeft, 0);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (myEffect == null) return;
        
        tooltipName.text = myEffect.data.effectName;
        tooltipDesc.text = myEffect.data.description;
        tooltipPanel.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltipPanel.SetActive(false);
    }
}