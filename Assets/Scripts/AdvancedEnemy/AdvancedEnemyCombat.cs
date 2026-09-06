using UnityEngine;
using System.Collections;

public class AdvancedEnemyCombat : MonoBehaviour
{
    [Header("Оружие врага")]
    public Weapon startingWeapon; 

    [Header("Ссылки")]
    public Transform rightHand;
    public Transform leftHand; 
    public Animator animator;
    public AudioSource audioSource;

    // Внутренние данные (Храним ссылки здесь, а не в ScriptableObject!)
    private Weapon currentWeaponData;
    private GameObject currentRightWeaponObj;
    private GameObject currentLeftWeaponObj;
    
    private Collider rightHandHitbox; 
    private Collider leftHandHitbox;

    [HideInInspector] public bool isSwinging = false; 
    private float lastAttackTime = 0f;
    private int currentComboStep = 0;

    private void Start()
    {
        if (startingWeapon != null)
        {
            EquipWeapon(startingWeapon);
        }
    }

        private void Update()
    {
        HandleComboReset();
    }

    private void HandleComboReset()
    {

        float resetTime = (currentWeaponData != null) ? currentWeaponData.resetTime : 1.0f;
        
        if (!isSwinging && Time.time - lastAttackTime > resetTime)
        {
            currentComboStep = 0;
        }
    }

    public void EquipWeapon(Weapon weaponAsset)
    {
        if (weaponAsset == null || weaponAsset.prefab == null) return;

        currentWeaponData = weaponAsset;

        // --- Правая рука ---
        currentRightWeaponObj = Instantiate(weaponAsset.prefab); 
        currentRightWeaponObj.transform.SetParent(rightHand, true); 
        ResetTransform(currentRightWeaponObj);
        
        rightHandHitbox = FindHitbox(currentRightWeaponObj); 
        if (rightHandHitbox != null)
        {
            SetupHitboxScript(rightHandHitbox, weaponAsset.damage);
        }

        // --- Левая рука ---
        if (weaponAsset.isDualWeapon && weaponAsset.offHandPrefab != null && leftHand != null)
        {
            currentLeftWeaponObj = Instantiate(weaponAsset.offHandPrefab);
            currentLeftWeaponObj.transform.SetParent(leftHand, true); 
            ResetTransform(currentLeftWeaponObj);
            
            leftHandHitbox = FindHitbox(currentLeftWeaponObj);
            if (leftHandHitbox != null) SetupHitboxScript(leftHandHitbox, weaponAsset.damage);
        }

        // --- Аниматор ---
        if (animator != null)
        {
            // Если у оружия ЕСТЬ файл переопределения - подменяем контроллер
            if (weaponAsset.weaponAnimatorOverride != null)
            {
                animator.runtimeAnimatorController = weaponAsset.weaponAnimatorOverride;
            }

            // Устанавливаем скорость атаки ВСЕГДА (даже если это стандартные кулаки!)
            animator.SetFloat("WeaponAttackSpeed", weaponAsset.animSpeedMultiplier);
        }
        
        currentComboStep = 0;
        isSwinging = false;
    }

    public void TryAttack()
    {
        if (currentWeaponData == null || isSwinging) return;

        // --- ИСПРАВЛЕНИЕ: Делим кулдаун на скорость оружия! ---
        float speedMult = currentWeaponData.animSpeedMultiplier > 0 ? currentWeaponData.animSpeedMultiplier : 1f;
        float actualCooldown = currentWeaponData.attackCooldown / speedMult;

        if (Time.time - lastAttackTime < actualCooldown) return;

        ResetHitbox(rightHandHitbox);
        ResetHitbox(leftHandHitbox);

        StartCoroutine(PerformAttackRoutine(currentWeaponData.comboLength));
    }

    private void ResetHitbox(Collider col)
    {
        if (col == null) return;
        EnemyHitbox eh = col.GetComponent<EnemyHitbox>();
        if (eh != null) eh.ForceResetHitbox();
    }

    private IEnumerator PerformAttackRoutine(int maxCombo)
    {
        isSwinging = true;
        lastAttackTime = Time.time;

        // Выбор активного коллайдера
        bool useLeftHand = currentWeaponData.isDualWeapon && (currentComboStep % 2 != 0);
        Collider activeCollider = useLeftHand ? leftHandHitbox : rightHandHitbox;

        if (animator != null)
        {
            animator.SetInteger("ComboStep", currentComboStep);
            animator.SetTrigger("Attack");
        }

        EnemyHitbox activeHitboxScript = activeCollider != null ? activeCollider.GetComponent<EnemyHitbox>() : null;

        // --- УМНЫЙ ПЕРЕСЧЕТ СКОРОСТИ ДЛЯ ВРАГА ---
        float speedMult = currentWeaponData != null ? currentWeaponData.animSpeedMultiplier : 1f;
        if (speedMult <= 0) speedMult = 1f;

        // 1. ЗАМАХ (Замедлен/Ускорен)
        float windupTime = (currentWeaponData != null ? currentWeaponData.attackWindup : 0.1f) / speedMult;
        yield return new WaitForSeconds(windupTime);

        if (audioSource && currentWeaponData.attackSound)
            audioSource.PlayOneShot(currentWeaponData.attackSound);

        // 2. ВКЛЮЧАЕМ УРОН (Замедлен/Ускорен)
        if (activeHitboxScript != null) activeHitboxScript.StartAttack();
        else if (activeCollider != null) activeCollider.enabled = true;

        float activeTime = (currentWeaponData != null ? currentWeaponData.attackDuration : 0.2f) / speedMult;
        yield return new WaitForSeconds(activeTime);

        // 3. ВЫКЛЮЧАЕМ УРОН
        if (activeHitboxScript != null) activeHitboxScript.EndAttack();
        else if (activeCollider != null) activeCollider.enabled = false;

        // 4. ОТКАТ / ВОЗВРАТ В СТОЙКУ (Замедлен/Ускорен)
        // Не дает ИИ-агенту начать бежать за игроком, пока рука возвращается в Idle-позу
        float recoveryTime = (currentWeaponData != null ? currentWeaponData.recoveryDelay : 0.2f) / speedMult;
        yield return new WaitForSeconds(recoveryTime);

        // 5. ЗАВЕРШЕНИЕ
        isSwinging = false;
        currentComboStep++; 
        if (maxCombo > 0 && currentComboStep >= maxCombo) currentComboStep = 0;
    }

    private Collider FindHitbox(GameObject weaponObj)
    {
        if (weaponObj == null) return null;

        EnemyHitbox[] hitboxes = weaponObj.GetComponentsInChildren<EnemyHitbox>(true);
        if (hitboxes.Length > 0)
        {
            Collider col = hitboxes[0].GetComponent<Collider>();
            if (col != null) return col;
        }
        
        Debug.LogError($"[ВРАГ] В оружии {weaponObj.name} не найден EnemyHitbox!");
        return null;
    }

    private void SetupHitboxScript(Collider col, float damage)
    {
        col.enabled = false; 
        EnemyHitbox hitScript = col.GetComponent<EnemyHitbox>();
        if (hitScript != null) 
        {
            hitScript.SetDamage(damage);
            hitScript.mainAudioSource = this.audioSource;
        }
    }

    private void ResetTransform(GameObject obj)
    {
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }

    public Renderer[] GetWeaponRenderers()
    {
        var rends = new System.Collections.Generic.List<Renderer>();
        if (currentRightWeaponObj != null) rends.AddRange(currentRightWeaponObj.GetComponentsInChildren<Renderer>());
        if (currentLeftWeaponObj != null) rends.AddRange(currentLeftWeaponObj.GetComponentsInChildren<Renderer>());
        return rends.ToArray();
    }
}