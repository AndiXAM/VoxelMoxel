using UnityEngine;
using UnityEngine.UI;

public class UIStarfield : MonoBehaviour
{
    [Header("Настройки фона")]
    public int starCount = 50; // Количество квадратиков
    public Vector2 sizeRange = new Vector2(2f, 6f); // Размер (от и до)
    public Color starColor = new Color(1f, 1f, 1f, 0.5f); // Белый, полупрозрачный

    [Header("Движение")]
    public float baseSpeed = 15f; // Скорость полета
    public float wobbleSpeed = 2f; // Скорость покачивания
    public float wobbleAmount = 10f; // Сила покачивания

    private RectTransform rectTransform;
    private RectTransform[] stars;
    private Vector2[] starDirections;
    private float[] randomOffsets;

    private void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        stars = new RectTransform[starCount];
        starDirections = new Vector2[starCount];
        randomOffsets = new float[starCount];

        GenerateStars();
    }

    private void GenerateStars()
    {
        // Создаем пустой объект-контейнер для звезд, чтобы не мусорить в иерархии
        GameObject container = new GameObject("StarsContainer");
        container.transform.SetParent(transform, false);
        container.transform.SetAsFirstSibling(); // Ставим в самый низ, чтобы звезды были ПОД кнопками

        for (int i = 0; i < starCount; i++)
        {
            // Создаем квадратик
            GameObject starObj = new GameObject($"Star_{i}");
            starObj.transform.SetParent(container.transform, false);

            Image img = starObj.AddComponent<Image>();
            img.color = starColor;

            RectTransform starRect = starObj.GetComponent<RectTransform>();
            
            // Случайный размер
            float size = Random.Range(sizeRange.x, sizeRange.y);
            starRect.sizeDelta = new Vector2(size, size);

            // Случайная начальная позиция внутри нашего окна
            float startX = Random.Range(-rectTransform.rect.width / 2f, rectTransform.rect.width / 2f);
            float startY = Random.Range(-rectTransform.rect.height / 2f, rectTransform.rect.height / 2f);
            starRect.anchoredPosition = new Vector2(startX, startY);

            // Случайное направление медленного дрейфа
            starDirections[i] = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)).normalized;
            
            // Смещение для синусоиды (чтобы они не качались синхронно)
            randomOffsets[i] = Random.Range(0f, 100f);

            stars[i] = starRect;
        }
    }

    private void Update()
    {
        float widthHalf = rectTransform.rect.width / 2f;
        float heightHalf = rectTransform.rect.height / 2f;

        for (int i = 0; i < stars.Length; i++)
        {
            // 1. Основное движение (дрейф)
            Vector2 currentPos = stars[i].anchoredPosition;
            currentPos += starDirections[i] * baseSpeed * Time.deltaTime;

            // 2. Покачивание (волна)
            // Добавляем перпендикулярное смещение через синус
            Vector2 perpDir = new Vector2(-starDirections[i].y, starDirections[i].x);
            Vector2 wobble = perpDir * Mathf.Sin(Time.time * wobbleSpeed + randomOffsets[i]) * wobbleAmount * Time.deltaTime;
            currentPos += wobble;

            // 3. Бесконечный цикл (Телепортация, если вылетели за край)
            // Плюс небольшой запас (10px), чтобы исчезновение было за границей RectMask2D
            if (currentPos.x > widthHalf + 10f) currentPos.x = -widthHalf - 10f;
            else if (currentPos.x < -widthHalf - 10f) currentPos.x = widthHalf + 10f;

            if (currentPos.y > heightHalf + 10f) currentPos.y = -heightHalf - 10f;
            else if (currentPos.y < -heightHalf - 10f) currentPos.y = heightHalf + 10f;

            // Применяем позицию
            stars[i].anchoredPosition = currentPos;
        }
    }
}