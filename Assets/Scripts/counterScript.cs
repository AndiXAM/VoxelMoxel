using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class TitlsdfeScript : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float yOffset = 200f;
    [SerializeField] private TextMeshProUGUI targetText;

    [Header("Positions")]
    [SerializeField] private Vector2 topPosition;
    [SerializeField] private Vector2 bottomPosition;

    private RectTransform rectTransform;
    private Coroutine moveCoroutine;
    private string lastTextValue;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        // Явная установка стартовых позиций
        topPosition = rectTransform.anchoredPosition;
        bottomPosition = topPosition + new Vector2(0, -yOffset);

        // Принудительная инициализация позиции
        lastTextValue = targetText.text.Trim();
        rectTransform.anchoredPosition = lastTextValue == "0" ? topPosition : bottomPosition;
    }

    void Update()
    {
        string currentText = targetText.text.Trim();
        
        if(currentText != lastTextValue)
        {
            if(moveCoroutine != null) StopCoroutine(moveCoroutine);
            
            Vector2 target = currentText == "0" ? topPosition : bottomPosition;
            moveCoroutine = StartCoroutine(AnimateMovement(target));
            
            lastTextValue = currentText;
        }
    }

    private IEnumerator AnimateMovement(Vector2 targetPosition)
    {
        Vector2 startPosition = rectTransform.anchoredPosition;
        float elapsed = 0f;

        while(elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.SmoothStep(0, 1, elapsed / moveDuration);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, progress);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPosition;
        moveCoroutine = null;
    }
}