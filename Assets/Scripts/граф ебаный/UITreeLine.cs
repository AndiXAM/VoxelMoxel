using UnityEngine;
using UnityEngine.UI;

// Наследуемся от MaskableGraphic, чтобы скрипт сам мог рисовать полигоны в UI
public class UITreeLine : MaskableGraphic
{
    [Header("Связь")]
    public UITreeNode startNode;
    public UITreeNode endNode;

    [Header("Настройки Линии")]
    public float lineThickness = 4f;
    [Range(2, 50)] public int segments = 20; // На сколько кусочков поделена линия (чем больше, тем более гладкая волна)
    
    public Color lockedColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);
    public Color unlockedColor = new Color(1f, 1f, 1f, 0.8f);

    [Header("Анимация (Энергетическая Волна)")]
    public bool animateWave = true;
    public float waveAmplitude = 15f; // Насколько высоко поднимается волна
    public float waveFrequency = 3f;  // Количество изгибов на линии
    public float waveSpeed = 5f;      // Скорость бега волны

    private void Update()
    {
        UpdateColorLogic();

        // Заставляем Unity перерисовывать линию каждый кадр для создания анимации
        if (animateWave)
        {
            SetVerticesDirty(); 
        }
    }

        protected override void Start()
    {
        base.Start();
        // Закидываем линию на самый задний план (под кнопки)
        rectTransform.SetAsFirstSibling();
    }

    private void UpdateColorLogic()
    {
        if (endNode == null || endNode.uiManager == null) return;

        bool isUnlocked = false;
        bool canUnlock = false;

        if (endNode.classData != null)
        {
            isUnlocked = endNode.uiManager.IsClassUnlocked(endNode.classData);
            canUnlock = endNode.uiManager.CanUnlockClass(endNode.classData);
        }
        else if (endNode.skillData != null)
        {
            isUnlocked = endNode.uiManager.IsSkillUnlocked(endNode.skillData);
            canUnlock = endNode.uiManager.CanUnlockSkill(endNode.skillData);
        }

        // Меняем цвет самого компонента (this.color - это встроенное свойство Graphic)
        this.color = (isUnlocked || canUnlock) ? unlockedColor : lockedColor;
    }

    // ВСТРОЕННЫЙ МЕТОД UNITY: Здесь мы вручную рисуем геометрию нашей линии
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); // Очищаем старую геометрию

        if (startNode == null || endNode == null) return;

        // Переводим мировые координаты кнопок в локальные координаты линии
        Vector2 startPos = rectTransform.InverseTransformPoint(startNode.transform.position);
        Vector2 endPos = rectTransform.InverseTransformPoint(endNode.transform.position);

        Vector2 dir = (endPos - startPos).normalized;
        // Вектор, перпендикулярный линии (нужен для толщины и изгиба волны)
        Vector2 perp = new Vector2(-dir.y, dir.x); 

        // Рисуем линию по сегментам
        for (int i = 0; i <= segments; i++)
        {
            // t идет от 0 (начало) до 1 (конец)
            float t = i / (float)segments; 
            
            // Базовая точка на прямой между узлами
            Vector2 basePos = Vector2.Lerp(startPos, endPos, t);

            float currentAmplitude = 0f;
            if (animateWave)
            {
                // Считаем саму волну
                float wave = Mathf.Sin(Time.time * waveSpeed + t * Mathf.PI * waveFrequency);
                
                // МАГИЯ (Envelope): Умножаем на синус от t. 
                // Это сделает так, что волна будет равна 0 в начале и в конце, 
                // и максимальна в середине. Концы линии намертво прилипнут к кнопкам!
                float envelope = Mathf.Sin(t * Mathf.PI); 
                
                currentAmplitude = wave * waveAmplitude * envelope;
            }

            // Итоговая позиция точки с учетом волны
            Vector2 pointPos = basePos + perp * currentAmplitude;

            // Создаем две вершины (верхний и нижний край толщины линии)
            UIVertex v1 = UIVertex.simpleVert;
            v1.color = this.color;
            v1.position = pointPos + perp * (lineThickness / 2f);

            UIVertex v2 = UIVertex.simpleVert;
            v2.color = this.color;
            v2.position = pointPos - perp * (lineThickness / 2f);

            vh.AddVert(v1);
            vh.AddVert(v2);

            // Соединяем вершины треугольниками, чтобы получилась полоска
            if (i > 0)
            {
                int startIndex = (i - 1) * 2;
                vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
                vh.AddTriangle(startIndex + 1, startIndex + 3, startIndex + 2);
            }
        }
    }
}