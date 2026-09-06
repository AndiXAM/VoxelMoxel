using UnityEngine;
using System.Collections;

public class DamageFlasher : MonoBehaviour
{
    [Header("Цвета вспышек")]
    public Color damageColor = Color.red;                         // Цвет при получении урона
    public Color dodgeColor = new Color(0.2f, 0.75f, 1.0f, 1.0f); // Приятный голубой цвет при увороте

    [Header("Тайминги")]
    public float flashDuration = 0.12f;  // Сколько длится пиковая вспышка
    public float fadeDuration = 0.25f;   // Как долго цвет возвращается в норму

    private Renderer[] renderers;
    private Color[] originalColors;
    private Coroutine flashCoroutine;

    private void Start()
    {
        // Находим все части 3D-модели (тело, броня)
        renderers = GetComponentsInChildren<Renderer>();
        originalColors = new Color[renderers.Length];
        
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i].material.HasProperty("_BaseColor"))
                originalColors[i] = renderers[i].material.GetColor("_BaseColor");
            else if (renderers[i].material.HasProperty("_Color"))
                originalColors[i] = renderers[i].material.color;
            else
                originalColors[i] = Color.white;
        }
    }

    // 1. Вызов красной вспышки при получении урона
    public void Flash(float damagePercent)
    {
        float intensity = Mathf.Clamp(damagePercent, 0.2f, 1f);
        StartFlash(damageColor, intensity);
    }

    // 2. Вызов приятной голубой вспышки при успешном увороте
    public void FlashDodge(float intensity = 0.85f)
    {
        StartFlash(dodgeColor, intensity);
    }

    private void StartFlash(Color targetColor, float intensity)
    {
        if (renderers == null || renderers.Length == 0) return;

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(targetColor, intensity));
    }

    private IEnumerator FlashRoutine(Color targetColor, float intensity)
    {
        // --- ФАЗА 1: Мгновенная яркая вспышка ---
        SetColor(Color.Lerp(Color.white, targetColor, intensity));
        yield return new WaitForSeconds(flashDuration);

        // --- ФАЗА 2: Плавное затухание к оригиналу ---
        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;

                Color currentColor = Color.Lerp(targetColor, originalColors[i], progress);
                Color finalColor = Color.Lerp(originalColors[i], currentColor, intensity);

                SetMaterialColor(renderers[i].material, finalColor);
            }
            yield return null;
        }

        // Возвращаем оригинальные цвета
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null) 
                SetMaterialColor(renderers[i].material, originalColors[i]);
        }
        flashCoroutine = null;
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
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
        else if (mat.HasProperty("_Color")) mat.color = c;
    }
}