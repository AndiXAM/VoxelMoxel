using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponFlasher : MonoBehaviour
{
    [Header("Ссылки на Руки (авто-поиск, если не назначены)")]
    public Transform rightHand;
    public Transform leftHand;

    [Header("Базовая Длительность")]
    public float baseFlashDuration = 0.25f;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        // Автоматический поиск костей рук, если забыли назначить в инспекторе
        if (rightHand == null || leftHand == null)
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                string n = t.name.ToLower();
                if (rightHand == null && (n.Contains("righthand") || n.Contains("hand.r") || n.Contains("hand_r") || n.Contains("right_hand")))
                    rightHand = t;
                if (leftHand == null && (n.Contains("lefthand") || n.Contains("hand.l") || n.Contains("hand_l") || n.Contains("left_hand")))
                    leftHand = t;
            }
        }
    }

    public void Flash(Color flashColor, float damagePercent = 1f)
    {
        Renderer[] rends = GetCurrentWeaponRenderers();
        Flash(rends, flashColor, damagePercent);
    }

    public void Flash(Renderer[] weaponRenderers, Color flashColor, float damagePercent = 1f)
    {
        if (weaponRenderers == null || weaponRenderers.Length == 0)
            weaponRenderers = GetCurrentWeaponRenderers();

        if (weaponRenderers == null || weaponRenderers.Length == 0) return;

        float intensity = Mathf.Clamp(damagePercent, 0.1f, 1.0f);

        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(FlashRoutine(weaponRenderers, flashColor, intensity));
    }

    private Renderer[] GetCurrentWeaponRenderers()
    {
        List<Renderer> list = new List<Renderer>();
        if (rightHand != null) list.AddRange(rightHand.GetComponentsInChildren<Renderer>());
        if (leftHand != null) list.AddRange(leftHand.GetComponentsInChildren<Renderer>());
        return list.ToArray();
    }

    private IEnumerator FlashRoutine(Renderer[] renderers, Color flashColor, float intensity)
    {
        float duration = baseFlashDuration * (0.7f + intensity * 0.8f);
        float emissionBrightness = Mathf.Lerp(1.5f, 4.5f, intensity);

        List<Material> mats = new List<Material>();
        List<Color> originalBaseColors = new List<Color>();
        List<Color> originalEmissionColors = new List<Color>();

        foreach (var r in renderers)
        {
            if (r == null) continue;
            foreach (var m in r.materials)
            {
                if (m == null) continue;
                mats.Add(m);

                if (m.HasProperty("_BaseColor")) originalBaseColors.Add(m.GetColor("_BaseColor"));
                else if (m.HasProperty("_Color")) originalBaseColors.Add(m.GetColor("_Color"));
                else originalBaseColors.Add(Color.white);

                if (m.HasProperty("_EmissionColor")) originalEmissionColors.Add(m.GetColor("_EmissionColor"));
                else originalEmissionColors.Add(Color.black);

                m.EnableKeyword("_EMISSION");
            }
        }

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float progress = timer / duration;
            float factor = (progress < 0.35f) ? 1f : Mathf.Lerp(1f, 0f, (progress - 0.35f) / 0.65f);

            for (int i = 0; i < mats.Count; i++)
            {
                Material m = mats[i];
                if (m == null) continue;

                Color origBase = originalBaseColors[i];
                Color origEmission = originalEmissionColors[i];

                Color targetBase = (flashColor == Color.black) 
                    ? Color.Lerp(origBase, Color.black, Mathf.Lerp(0.7f, 1.0f, intensity)) 
                    : new Color(4.0f, 4.0f, 4.0f, 1.0f); // Заставляет текстуру гореть белым!

                Color targetEmission = (flashColor == Color.white) 
                    ? (Color.white * emissionBrightness) 
                    : Color.black;

                Color curBase = Color.Lerp(origBase, targetBase, factor);
                Color curEmission = Color.Lerp(origEmission, targetEmission, factor);

                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", curBase);
                if (m.HasProperty("_Color")) m.SetColor("_Color", curBase);
                if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", curEmission);
            }

            yield return null;
        }

        for (int i = 0; i < mats.Count; i++)
        {
            Material m = mats[i];
            if (m == null) continue;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", originalBaseColors[i]);
            if (m.HasProperty("_Color")) m.SetColor("_Color", originalBaseColors[i]);
            if (m.HasProperty("_EmissionColor")) m.SetColor("_EmissionColor", originalEmissionColors[i]);
        }

        flashCoroutine = null;
    }
}