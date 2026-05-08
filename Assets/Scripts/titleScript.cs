using TMPro;
using UnityEngine;

public class TitleScript : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveDuration = 1f;
    [SerializeField] private float pauseDuration = 3f;
    [SerializeField] private float yOffset = 200f;

    public TextMeshProUGUI mainText;

    public TextMeshProUGUI subText;

    private RectTransform rectTransform;
    private Vector2 originalPosition;
    private Vector2 targetPosition;
    
    private enum AnimationState { Completed, MovingDown, Pausing, MovingUp }
    private AnimationState currentState = AnimationState.Completed;
    
    private float timer;
    private Vector2 startPosition;
    private Vector2 endPosition;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        originalPosition = rectTransform.anchoredPosition;
        targetPosition = originalPosition + new Vector2(0, -yOffset);
        
        // Устанавливаем начальную позицию явно
        rectTransform.anchoredPosition = originalPosition;
    }

    void Update()
    {
        switch(currentState)
        {
            case AnimationState.MovingDown:
            case AnimationState.MovingUp:
                UpdateMovement();
                break;
            
            case AnimationState.Pausing:
                UpdatePause();
                break;
        }
    }

    private void InitializeMovement(Vector2 from, Vector2 to)
    {
        startPosition = from;
        endPosition = to;
        timer = 0f;
    }

    private void UpdateMovement()
    {
        timer += Time.deltaTime;
        float progress = Mathf.SmoothStep(0f, 1f, timer / moveDuration);
        rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, progress);

        if(timer >= moveDuration)
        {
            if(currentState == AnimationState.MovingDown)
            {
                currentState = AnimationState.Pausing;
                timer = 0f;
            }
            else
            {
                currentState = AnimationState.Completed;
            }
        }
    }

    private void UpdatePause()
    {
        timer += Time.deltaTime;
        if(timer >= pauseDuration)
        {
            currentState = AnimationState.MovingUp;
            InitializeMovement(targetPosition, originalPosition);
        }
    }

    public void StartAnimation(string MText, string SText)
    {
        if(currentState != AnimationState.Completed) return;
        
        mainText.text = MText;
        subText.text = SText;

        currentState = AnimationState.MovingDown;
        InitializeMovement(originalPosition, targetPosition);
        
        // Принудительно устанавливаем начальную позицию
        rectTransform.anchoredPosition = originalPosition;
    }
}