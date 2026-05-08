using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Header("Настройки времени")]
    [Range(0, 24)] public float timeOfDay = 12f; // Текущее время (12 = полдень)
    public float timeSpeed = 1f; // Скорость течения времени

    [Header("Ссылки")]
    public Light directionalLight; // Сюда перетащить солнце
    public Material skyboxMaterial; // Сюда перетащить CubeSky

    [Header("Градиенты цветов (Настройка суток)")]
    // Градиенты позволяют настроить цвета для утра, дня, вечера и ночи!
    public Gradient skyTopColor;
    public Gradient skyBottomColor; // Он же цвет тумана
    public Gradient lightColor;

    public Gradient ambientColor;
    
    [Header("Яркость света")]
    public AnimationCurve lightIntensity; // График: днем светло (1), ночью темно (0.1)

    void Update()
    {
        // 1. Двигаем время
        if (Application.isPlaying) {
            timeOfDay += Time.deltaTime * timeSpeed;
            if (timeOfDay >= 24f) timeOfDay = 0f;
        }

        UpdateEnvironment();
    }

    void OnValidate()
    {
        // Позволяет крутить ползунок времени в редакторе и сразу видеть результат!
        if (directionalLight != null && skyboxMaterial != null) UpdateEnvironment();
    }

    void UpdateEnvironment()
    {
        // Получаем значение от 0 до 1 (где 0 это 00:00, а 1 это 24:00)
        float t = timeOfDay / 24f;

        // 2. Вращаем солнце (Directional Light)
        // В 12:00 угол 90 (светит сверху), в 00:00 угол -90 (светит снизу)
        float sunAngle = (timeOfDay - 6f) / 12f * 180f;
        // 1. Вращаем солнце по времени
        Quaternion timeRotation = Quaternion.Euler(sunAngle, 0f, 0f);
        // 2. Наклоняем саму орбиту (сбоку и под углом), чтобы в 12:00 оно светило чуть сбоку!
        Quaternion orbitTilt = Quaternion.Euler(0f, -30f, 30f); 

        directionalLight.transform.localRotation = orbitTilt * timeRotation;

        // 3. Обновляем цвета света
        directionalLight.color = lightColor.Evaluate(t);
        directionalLight.intensity = lightIntensity.Evaluate(t);

        // 4. Обновляем цвета Неба
        Color top = skyTopColor.Evaluate(t);
        Color bottom = skyBottomColor.Evaluate(t);
        
        skyboxMaterial.SetColor("_TopColor", top);
        skyboxMaterial.SetColor("_BottomColor", bottom);

        // 5. ИДЕАЛЬНЫЙ ТУМАН - синхронизируем с горизонтом неба!
        RenderSettings.fogColor = bottom;

        RenderSettings.ambientLight = ambientColor.Evaluate(t);
    }
} 