using UnityEngine;
using System.Collections;

public class DamageFlasher : MonoBehaviour
{
    [Header("Настройки вспышки")]
    public Color flashColor = Color.red; // Цвет при получении урона
    public float flashDuration = 0.15f;  // Сколько длится жесткая вспышка
    public float fadeDuration = 0.3f;    // Как долго цвет возвращается в норму

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    private void Start()
    {
        // Находим все части 3D-модели (тело, броня, оружие) в этом объекте и его детях
        renderers = GetComponentsInChildren<Renderer>();
        
        // Запоминаем их оригинальные цвета
        originalColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            // Проверяем, есть ли у материала вообще цвет (чтобы не сломать партиклы)
            if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else if (renderers[i].material.HasProperty("_BaseColor")) // Для URP
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            else
                originalColors[i] = Color.white; // Фоллбэк
        }
    }

    // Метод, который мы будем вызывать при получении урона
    // damagePercent - это число от 0.0 до 1.0 (какую долю ХП снесли)
    public void Flash(float damagePercent)
    {
        if (renderers == null || renderers.Length == 0) return;

        // Ограничиваем процент (чтобы не было ядерного свечения при оверкилле)
        float intensity = Mathf.Clamp(damagePercent, 0.2f, 1f); 
        // Минимальная интенсивность 0.2f гарантирует, что даже царапину будет видно

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(intensity));
    }

    private IEnumerator FlashRoutine(float intensity)
    {
        // --- ФАЗА 1: Мгновенная вспышка ---
        SetColor(Color.Lerp(Color.white, flashColor, intensity));
        yield return new WaitForSeconds(flashDuration);

        // --- ФАЗА 2: Плавное затухание (Возврат к оригиналу) ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            // Возвращаем каждый материал к его изначальному цвету
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                Color currentColor = Color.Lerp(flashColor, originalColors[i], progress);
                
                // Смешиваем текущий цвет с интенсивностью удара, 
                // чтобы слабые удары затухали быстрее
                Color finalColor = Color.Lerp(originalColors[i], currentColor, intensity);
                
                SetMaterialColor(renderers[i].material, finalColor);
            }
            yield return null;
        }

        // Страховка: жестко возвращаем оригинальные цвета в конце
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) SetMaterialColor(renderers[i].material, originalColors[i]);
        }
    }

    private void SetColor(Color c)
    {
        foreach (Renderer r in renderers)
        {
            if (r != null) SetMaterialColor(r.material, c);
        }
    }

    private void SetMaterialColor(Material mat, Color c)
    {
        if (mat.HasProperty("_Color")) mat.color = c;
        else if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
    }
}