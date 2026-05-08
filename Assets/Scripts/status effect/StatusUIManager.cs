using UnityEngine;

public class StatusUIManager : MonoBehaviour
{
    public StatusEffectManager targetManager; // Чьи баффы рисуем? (Игрока)
    public GameObject cardPrefab;             // Префаб карточки
    public Transform cardsParent;             // Контейнер для карточек

    private void Start()
    {
        if (targetManager != null)
        {
            // Подписываемся на изменения в "крови" игрока
            targetManager.OnStatusEffectsChanged += RedrawCards;
        }
    }

    private void RedrawCards()
    {
        // 1. Удаляем все старые карточки
        foreach (Transform child in cardsParent)
        {
            Destroy(child.gameObject);
        }

        // 2. Рисуем новые
        foreach (var effect in targetManager.activeEffects)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardsParent);
            StatusCardUI cardUI = cardObj.GetComponent<StatusCardUI>();
            cardUI.Setup(effect);
        }
    }
}