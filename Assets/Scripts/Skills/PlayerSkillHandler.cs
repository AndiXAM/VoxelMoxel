using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PlayerSkillHandler : MonoBehaviour
{

    [Header("Камера Игрока")]
    public Transform playerCamera; // Ссылка на камеру, кэшируется при старте

    [Header("Ссылки на Системы")]
    public Inventory inventory;
    public Transform rightHand;
    public Transform leftHand;
    public Animator characterAnimator;
    public AudioSource audioSource;
    public StatsContainer playerStats;

    [Header("Скрипт-витрина для врагов")]
    public PlayerStateFlags stateFlags;

    private SkillItem currentSkillItem;
    private GameObject visualWeaponObject;
    private bool isCasting = false;

    private RuntimeAnimatorController defaultAnimatorController;
    private AnimatorOverrideController skillOverride;

    private Dictionary<SkillItem, float> cooldownTimers = new Dictionary<SkillItem, float>();

    private void Awake()
    {
        // Запоминаем изначальный контроллер кулаков
        if (characterAnimator != null)
        {
            defaultAnimatorController = characterAnimator.runtimeAnimatorController;
        }
    }

    private void Start()
    {
        if (inventory == null) inventory = GetComponentInParent<Inventory>();
        if (playerStats == null) playerStats = GetComponentInParent<StatsContainer>();
        if (stateFlags == null) stateFlags = GetComponentInParent<PlayerStateFlags>();
        if (stateFlags == null) stateFlags = GetComponent<PlayerStateFlags>();

        // Автоматически берем камеру из Character.cs или MainCamera ровно 1 раз при старте
        if (playerCamera == null)
        {
            Character characterScript = GetComponent<Character>();
            if (characterScript == null) characterScript = GetComponentInParent<Character>();

            if (characterScript != null && characterScript.Camera != null)
                playerCamera = characterScript.Camera;
            else if (Camera.main != null)
                playerCamera = Camera.main.transform;
        }
    }

    private void Update()
    {
        if (currentSkillItem != null && Input.GetMouseButtonDown(0) && !isCasting)
        {
            TryCastCurrentSkill();
        }
    }

    // --- 1. ЭКИПИРОВКА: ПРАВИЛЬНАЯ СТОЙКА МЕЧА ИЗ СЛОТА ---
    public void EquipSkill(SkillItem newSkill, bool rebuildVisuals = true)
    {
        if (rebuildVisuals)
        {
            ClearSkillHand(true);
        }
        else
        {
            ClearSkillHand(false); // Оставляем визуал меча в руке
        }

        if (newSkill == null) return;

        currentSkillItem = newSkill;

        if (currentSkillItem.requiresEquippedWeapon && inventory != null)
        {
            Weapon slotWeapon = inventory.GetEquippedWeaponInSlot();
            if (slotWeapon != null && slotWeapon.prefab != null && rightHand != null)
            {
                // Спавним меч только если его еще нет в руке
                if (rebuildVisuals || visualWeaponObject == null)
                {
                    visualWeaponObject = Instantiate(slotWeapon.prefab);
                    visualWeaponObject.transform.SetParent(rightHand, true);
                    visualWeaponObject.transform.localPosition = Vector3.zero;
                    visualWeaponObject.transform.localRotation = Quaternion.identity;
                }

                if (characterAnimator != null)
                {
                    skillOverride = new AnimatorOverrideController(defaultAnimatorController);

                    if (slotWeapon.weaponAnimatorOverride != null)
                    {
                        var weaponOverrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                        slotWeapon.weaponAnimatorOverride.GetOverrides(weaponOverrides);
                        skillOverride.ApplyOverrides(weaponOverrides);
                    }

                    characterAnimator.runtimeAnimatorController = skillOverride;
                    characterAnimator.SetBool("WeaponEquipped", true);
                }
            }
        }
    }

    public void ClearSkillHand(bool destroyVisuals = true)
    {
        if (destroyVisuals && visualWeaponObject != null)
        {
            Destroy(visualWeaponObject);
            visualWeaponObject = null;

            if (characterAnimator != null)
            {
                characterAnimator.SetBool("WeaponEquipped", false);
                
                if (defaultAnimatorController != null)
                {
                    characterAnimator.runtimeAnimatorController = defaultAnimatorController;
                    skillOverride = new AnimatorOverrideController(defaultAnimatorController);
                    characterAnimator.runtimeAnimatorController = skillOverride;
                }
            }
        }

        currentSkillItem = null;
        isCasting = false;
    }

    public void TryCastCurrentSkill()
    {
        if (currentSkillItem == null || isCasting) return;

        if (IsOnCooldown(currentSkillItem)) return;

        Weapon slotWeapon = null;
        if (inventory != null) slotWeapon = inventory.GetEquippedWeaponInSlot();

        if (currentSkillItem.requiresEquippedWeapon && slotWeapon == null)
        {
            Debug.LogWarning($"[СПОСОБНОСТЬ] Требуется оружие в слоте экипировки!");
            return;
        }

        StartCoroutine(CastSkillRoutine(currentSkillItem, slotWeapon));
    }

    // --- 2. КАСТ: ОТКЛЮЧЕНИЕ 2-ГО СЛОЯ, ЧТОБЫ ОН НЕ ПЕРЕКРЫВАЛ РУКИ ---
    private IEnumerator CastSkillRoutine(SkillItem skill, Weapon slotWeapon)
    {
        isCasting = true;
        cooldownTimers[skill] = Time.time + skill.cooldown;

         // --- Включаем флаг атаки для ИИ врагов ---
        if (stateFlags != null) stateFlags.isSwingingWeapon = true;

        if (skill.skillAnimation != null && characterAnimator != null && skillOverride != null)
        {
            skillOverride["Skill_Placeholder"] = skill.skillAnimation;

            if (characterAnimator.layerCount > 1 && skill.bodyMask == SkillBodyMask.FullBody)
            {
                characterAnimator.SetLayerWeight(1, 0f);
            }

            characterAnimator.CrossFadeInFixedTime("SkillAction", 0.03f, 0);
        }

        if (audioSource != null && skill.castSound != null) audioSource.PlayOneShot(skill.castSound);
        if (skill.castVFXPrefab != null) Instantiate(skill.castVFXPrefab, transform.position, transform.rotation);

        skill.ExecuteSkill(this, slotWeapon);

        // Ждем длительность анимации
        yield return new WaitForSeconds(skill.activeDuration);

        // --- ПЛАВНЫЙ ВОЗВРАТ В СТОЙКУ (БЕЗ ТЕЛЕПОРТАЦИИ И ЗАСТЫВАНИЙ) ---
        if (characterAnimator != null)
        {
            characterAnimator.SetBool("WeaponEquipped", true);

            // 1. Плавно размораживаем ноги на 0-м слое (возвращаем их в Armature|Idle)
            characterAnimator.CrossFadeInFixedTime("Armature|Idle", 0.2f, 0);

            // 2. Плавно возвращаем руки во 2-м слое за 0.2 секунды
            if (characterAnimator.layerCount > 1)
            {
                StartCoroutine(SmoothRestoreLayerWeight(1, 0.2f));
            }
        }

        // --- Выключаем флаг атаки для ИИ врагов ---
        if (stateFlags != null) stateFlags.isSwingingWeapon = false;

        isCasting = false;

        //  Накладываем усталость бега на 0.75 сек после способности ---
        Character playerChar = GetComponentInParent<Character>();
        if (playerChar != null) playerChar.ApplyRunFatigue(0.75f);
        // ------------------------------------------------------------------------------
    }
    private bool IsOnCooldown(SkillItem skill)
    {
        if (cooldownTimers.TryGetValue(skill, out float readyTime)) return Time.time < readyTime;
        return false;
    }

    // Плавно возвращает влияние 2-го слоя (атак/рук) от 0 до 1, чтобы не было телепортации позы
    private IEnumerator SmoothRestoreLayerWeight(int layerIndex, float duration)
    {
        if (characterAnimator == null || characterAnimator.layerCount <= layerIndex) yield break;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float weight = Mathf.Lerp(0f, 1f, timer / duration);
            characterAnimator.SetLayerWeight(layerIndex, weight);
            yield return null;
        }
        characterAnimator.SetLayerWeight(layerIndex, 1f);
    }
}

