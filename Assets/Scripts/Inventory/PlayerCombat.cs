using UnityEngine;
using System.Collections;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    public Transform rightHand;
    public Transform leftHand; 
    public Animator characterAnimator;
    public AudioSource audioSource;

    [Tooltip("Скрипт-витрина для врагов")]
    public PlayerStateFlags stateFlags;

    // --- Данные ---
    private Weapon currentWeaponData; // Скрипт на ПРАВОЙ руке (Корень)
    
    private GameObject currentWeaponObject;   // Объект корня (в правой руке)
    private GameObject leftHandModelObject;   // Объект левой руки
    
    private Collider leftHandHitbox; // Хитбокс левой руки

    // --- ТРЕЙЛЫ ОРУЖИЯ ---
    private TrailRenderer rightHandTrail;
    private TrailRenderer leftHandTrail;

    private RuntimeAnimatorController defaultAnimatorController;

    private Coroutine attackCoroutine; // Ссылка на запущенную корутину удара
    private bool canChainAttack = false; // Открыто ли окно для следующего комбо-удара?

    [SerializeField] StatsContainer playerStats;

    private int currentComboStep = 0; 
    private float lastAttackTime = 0f;
    private bool isSwinging = false; 
    private bool isBlocking = false; 

    // Awake вызывается самым первым, до всяких инвентарей
    private void Awake()
    {
        // Запоминаем базовый контроллер сразу же при рождении объекта
        if (characterAnimator != null)
        {
            defaultAnimatorController = characterAnimator.runtimeAnimatorController;
        }
    }

    private void Start()
    {
        // А вот обновлять стейты можно уже в Start
        UpdateAnimatorCombatState();
    }

    private void Update()
    {
        // Разрешаем ввод, если мы не бьем ИЛИ если мы находимся в окне комбо-среза
        if (isSwinging && !canChainAttack) return;

        HandleBlock();
        HandleComboReset();
        
        if (Input.GetMouseButtonDown(0) && !isBlocking)
        {
            TryAttack();
        }
    }

    private void HandleBlock()
    {
        if (currentWeaponData == null) return; 
        bool blockInput = Input.GetKey(KeyCode.F);
        
        if (blockInput != isBlocking)
        {
            isBlocking = blockInput;
            playerStats.IsBlock = isBlocking;
            
            if (isBlocking)
            {
                // --- МГНОВЕННЫЙ СРЕЗ ВОЗВРАТА РУКИ В БЛОК ---
                if (isSwinging && attackCoroutine != null)
                {
                    StopCoroutine(attackCoroutine); // Останавливаем корутину возврата
                    isSwinging = false;
                    canChainAttack = false;
                    attackCoroutine = null;
                }

                StartCoroutine(ParryRoutine());
            }
            
            if (characterAnimator != null) characterAnimator.SetBool("IsBlocking", isBlocking);
        }
    }

    IEnumerator ParryRoutine()
    {
        if (!playerStats.ParryFatige)
        {
            playerStats.IsParry = true; // Окно парирования открыто
            playerStats.ParryFatige = true;
            yield return new WaitForSeconds(0.5f); // Длительность окна парирования
            
            playerStats.IsParry = false; // Окно закрылось

            if (playerStats.ParryFatige)
            {
                StartCoroutine(ParryCooldownRoutine()); 
            }
        }
    }

    IEnumerator ParryCooldownRoutine()
    {
        yield return new WaitForSeconds(2f); // Ждем 2 секунды
        playerStats.ParryFatige = false;     // Снова можем парировать
    }

    private void HandleComboReset()
    {
        // Считываем статы (включая resetTime) с актуального оружия
        Weapon activeWeapon = GetActiveWeaponToUse();
        float resetTime = (activeWeapon != null) ? activeWeapon.resetTime : 1.0f;
        
        if (Time.time - lastAttackTime > resetTime) currentComboStep = 0;
    }

    public void TryAttack()
    {
        Weapon activeWeapon = GetActiveWeaponToUse(); 
        if (activeWeapon == null) return;
        if (isSwinging && !canChainAttack) return;

        float speedMult = activeWeapon.animSpeedMultiplier > 0 ? activeWeapon.animSpeedMultiplier : 1f;
        float actualCooldown = activeWeapon.attackCooldown / speedMult;

        if (Time.time - lastAttackTime < actualCooldown) return;

        if (attackCoroutine != null)
        {
            StopCoroutine(attackCoroutine);
            // Физический хитбокс выключаем у реального оружия в руках (currentWeaponData)
            if (currentWeaponData != null && currentWeaponData.weaponHitbox != null) 
                currentWeaponData.weaponHitbox.enabled = false;
                
            if (leftHandHitbox != null) leftHandHitbox.enabled = false;
        }

        attackCoroutine = StartCoroutine(PerformAttackRoutine(activeWeapon.comboLength));
    }

    private IEnumerator PerformAttackRoutine(int maxCombo)
    {
        Weapon activeWeapon = GetActiveWeaponToUse(); // Считываем статы
        if (activeWeapon == null) yield break;

        isSwinging = true;
        canChainAttack = false; 
        lastAttackTime = Time.time;

        // Сообщаем ИИ врагов, что игрок начал замах! ---
         if (stateFlags != null) stateFlags.isSwingingWeapon = true;

        if (characterAnimator != null)
        {
            characterAnimator.SetInteger("ComboStep", currentComboStep);
            characterAnimator.SetTrigger("Attack");
        }

        // Проигрываем звук динамического оружия (тяжелый двуручник звучит тяжело, кинжал - легко!)
        if (activeWeapon.attackSound != null && audioSource)
            audioSource.PlayOneShot(activeWeapon.attackSound);

        Collider activeCollider = null;
        bool useLeftHand = activeWeapon.isDualWeapon && (currentComboStep % 2 != 0);

        if (useLeftHand && leftHandHitbox != null) activeCollider = leftHandHitbox;
         else if (currentWeaponData != null && currentWeaponData.weaponHitbox != null) activeCollider = currentWeaponData.weaponHitbox;

        Hitbox activeHitboxScript = null;
        if (activeCollider != null && activeCollider.gameObject != null)
        {
            activeHitboxScript = activeCollider.GetComponent<Hitbox>();
            if (activeHitboxScript != null) activeHitboxScript.ForceResetHitbox();
        }

        float speedMult = activeWeapon.animSpeedMultiplier;
        if (speedMult <= 0) speedMult = 1f;

        // Накладываем усталость бега на 0.75 сек после удара ---
        Character playerChar = GetComponentInParent<Character>();
        if (playerChar != null) playerChar.ApplyRunFatigue(0.75f);
        // ------------------------------------------------------------------------

        // 1. ЗАМАХ
        float windupTime = activeWeapon.attackWindup / speedMult;
        yield return new WaitForSeconds(windupTime);

        // 2. УДАР
        if (activeCollider != null && activeCollider.gameObject != null)
        {
            if (activeHitboxScript != null) activeHitboxScript.StartAttack();
            else activeCollider.enabled = true;

            if (useLeftHand && leftHandTrail != null) leftHandTrail.emitting = true;
            else if (!useLeftHand && rightHandTrail != null) rightHandTrail.emitting = true;

            float activeTime = activeWeapon.attackDuration / speedMult;
            yield return new WaitForSeconds(activeTime);

            if (useLeftHand && leftHandTrail != null) leftHandTrail.emitting = false;
            else if (!useLeftHand && rightHandTrail != null) rightHandTrail.emitting = false;

            if (activeCollider != null && activeCollider.gameObject != null)
            {
                if (activeHitboxScript != null) activeHitboxScript.EndAttack();
                else activeCollider.enabled = false;
            }
        }
        else
        {
            float activeTime = activeWeapon.attackDuration / speedMult;
            yield return new WaitForSeconds(activeTime);
        }

        currentComboStep++; 
        if (maxCombo > 0 && currentComboStep >= maxCombo) currentComboStep = 0;

        canChainAttack = true; 

        // 3. ОТКАТ
        float recoveryTime = activeWeapon.recoveryDelay / speedMult;
        yield return new WaitForSeconds(recoveryTime);

        if (characterAnimator != null) characterAnimator.SetBool("Attack", false);

        isSwinging = false;
        canChainAttack = false;
        attackCoroutine = null;

         //  Снимаем флаг атаки по окончании удара ---
        if (stateFlags != null) stateFlags.isSwingingWeapon = false;

        
    }

    // --- СМЕНА ОРУЖИЯ ---
    public void EquipWeapon(Weapon weaponAsset, bool rebuildVisuals = true)
    {
        if (rebuildVisuals)
        {
            ClearWeaponObjects();
        }

        if (weaponAsset == null)
        {
            ClearWeapon();
            return;
        }

        Weapon visualWeapon = weaponAsset;
        if (weaponAsset.isSkillWeapon)
        {
            Inventory inv = FindObjectOfType<Inventory>();
            if (inv != null)
            {
                Weapon slotWeapon = inv.GetEquippedWeaponInSlot();
                if (slotWeapon != null) visualWeapon = slotWeapon;
            }
        }

        if (visualWeapon.prefab == null)
        {
            ClearWeapon();
            return;
        }

        currentWeaponData = weaponAsset;

        // Создаем модель ТОЛЬКО если она действительно изменилась
        if (rebuildVisuals)
        {
            currentWeaponObject = Instantiate(visualWeapon.prefab); 
            currentWeaponObject.transform.SetParent(rightHand, true);   
            ResetTransform(currentWeaponObject);
            
            Collider rightCol = FindHitbox(currentWeaponObject);
            if (rightCol != null)
            {
                currentWeaponData.weaponHitbox = rightCol; 
                SetupHitboxScript(rightCol, visualWeapon.damage, visualWeapon.impactPower);
            }

            rightHandTrail = currentWeaponObject.GetComponentInChildren<TrailRenderer>();
            if (rightHandTrail != null) rightHandTrail.emitting = false;

            // Левая рука (если есть)
            if (visualWeapon.isDualWeapon && visualWeapon.offHandPrefab != null && leftHand != null)
            {
                leftHandModelObject = Instantiate(visualWeapon.offHandPrefab);
                leftHandModelObject.transform.SetParent(leftHand, true);
                ResetTransform(leftHandModelObject);

                leftHandHitbox = FindHitbox(leftHandModelObject);
                if (leftHandHitbox != null) SetupHitboxScript(leftHandHitbox, visualWeapon.damage, visualWeapon.impactPower);

                leftHandTrail = leftHandModelObject.GetComponentInChildren<TrailRenderer>();
                if (leftHandTrail != null) leftHandTrail.emitting = false;
            }
            else
            {
                leftHandModelObject = null;
                leftHandHitbox = null;
                leftHandTrail = null; 
            }
        }

        if (characterAnimator != null)
        {
            if (visualWeapon.weaponAnimatorOverride != null)
                characterAnimator.runtimeAnimatorController = visualWeapon.weaponAnimatorOverride;
            else if (weaponAsset.weaponAnimatorOverride != null)
                characterAnimator.runtimeAnimatorController = weaponAsset.weaponAnimatorOverride;
            
            characterAnimator.SetFloat("WeaponAttackSpeed", visualWeapon.animSpeedMultiplier);
        }
        
        currentComboStep = 0;
        isSwinging = false;
        isBlocking = false;
        
        UpdateAnimatorCombatState();
    }

    public void ClearWeapon(bool destroyVisuals = true)
    {
        if (destroyVisuals)
        {
            ClearWeaponObjects();
            rightHandTrail = null;
            leftHandTrail = null;

            if (characterAnimator != null)
            {
                characterAnimator.runtimeAnimatorController = defaultAnimatorController;
                characterAnimator.SetFloat("WeaponAttackSpeed", 1f);
            }
        }

        currentWeaponData = null;
        currentComboStep = 0;
        isSwinging = false;
        isBlocking = false;

        if (stateFlags != null) stateFlags.isSwingingWeapon = false;

        UpdateAnimatorCombatState();
    }

    public void ClearWeapon()
    {
        ClearWeaponObjects();
        currentWeaponData = null;
        leftHandModelObject = null;
        leftHandHitbox = null;
        
        rightHandTrail = null;
        leftHandTrail = null;

        if (characterAnimator != null)
        {
            characterAnimator.runtimeAnimatorController = defaultAnimatorController;
            characterAnimator.SetFloat("WeaponAttackSpeed", 1f);
        }
            
        currentComboStep = 0;
        isSwinging = false;
        isBlocking = false;

        // Сбрасываем флаг, если убрали оружие
        if (stateFlags != null) stateFlags.isSwingingWeapon = false;

        UpdateAnimatorCombatState();
    }

    private void ClearWeaponObjects()
    {
        if (currentWeaponObject != null) Destroy(currentWeaponObject);
        if (leftHandModelObject != null) Destroy(leftHandModelObject); 
    }

    private void ResetTransform(GameObject obj)
    {
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }
    
    private void UpdateAnimatorCombatState()
    {
        if (characterAnimator != null)
        {
            bool hasWeapon = (currentWeaponData != null);
            characterAnimator.SetBool("WeaponEquipped", hasWeapon);
        }
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
        
        Hitbox[] playerHitboxes = weaponObj.GetComponentsInChildren<Hitbox>(true);
        if (playerHitboxes.Length > 0)
        {
            Collider col = playerHitboxes[0].GetComponent<Collider>();
            if (col != null) return col;
        }

        Debug.LogError($"[ИГРОК] В оружии {weaponObj.name} не найден Hitbox!");
        return null;
    }

    private void SetupHitboxScript(Collider col, float damage, float impact)
    {
        col.enabled = false; 
        
        EnemyHitbox enemyScript = col.GetComponent<EnemyHitbox>();
        if (enemyScript != null)
        {
            enemyScript.SetDamage(damage);
            enemyScript.currentImpactPower = impact;
            enemyScript.mainAudioSource = this.audioSource;
            return;
        }

        Hitbox hitScript = col.GetComponent<Hitbox>();
        if (hitScript != null) 
        {
            hitScript.SetDamage(damage);
            hitScript.currentImpactPower = impact;
            hitScript.mainAudioSource = this.audioSource;
        }
    }

    private Weapon GetActiveWeaponToUse()
    {
        if (currentWeaponData != null && currentWeaponData.isSkillWeapon)
        {
            Inventory inv = FindFirstObjectByType <Inventory>();
            if (inv != null)
            {
                Weapon slotWeapon = inv.GetEquippedWeaponInSlot();
                if (slotWeapon != null) return slotWeapon;
            }
        }
        return currentWeaponData;
    }

    public Renderer[] GetWeaponRenderers()
    {
        var rends = new System.Collections.Generic.List<Renderer>();
        if (currentWeaponObject != null) rends.AddRange(currentWeaponObject.GetComponentsInChildren<Renderer>());
        if (leftHandModelObject != null) rends.AddRange(leftHandModelObject.GetComponentsInChildren<Renderer>());
        return rends.ToArray();
    }
}