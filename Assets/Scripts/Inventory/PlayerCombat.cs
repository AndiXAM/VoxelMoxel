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

    private GameObject leftHandModelObject; 
    
    private Collider leftHandHitbox; // Хитбокс левой руки

    private RuntimeAnimatorController defaultAnimatorController;

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
        if (isSwinging) return;
        HandleBlock();
        HandleComboReset();
        if (Input.GetMouseButtonDown(0) && !isBlocking) TryAttack();
    }

    

    private void HandleBlock()
    {
        if (currentWeaponData == null) return; 
        
        bool blockInput = Input.GetKey(KeyCode.F);

        // Если состояние изменилось (нажали ИЛИ отпустили)
        if (blockInput != isBlocking)
        {
            isBlocking = blockInput;
            
            // 1. Синхронизируем флаг блока в статах с нашим локальным состоянием
            playerStats.IsBlock = isBlocking;
            
            // 2. Анимация
            if (characterAnimator != null) 
            {
                characterAnimator.SetBool("IsBlocking", isBlocking);
            }

            // 3. Запускаем парирование ТОЛЬКО если мы НАЖАЛИ блок (а не отпустили)
            if (isBlocking)
            {
                StartCoroutine(ParryRoutine());
            }
        }
        
        // Строчку playerStats.IsBlock = false; отсюда УДАЛЯЕМ полностью!
    }

    IEnumerator ParryRoutine()
    {
        // Если нет усталости (кулдауна) на парирование
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
        // Примерно так должен выглядеть твой кулдаун
        yield return new WaitForSeconds(2f); // Ждем 2 секунды
        playerStats.ParryFatige = false;     // Снова можем парировать
    }

    private void HandleComboReset()
    {
        float resetTime = (currentWeaponData != null) ? currentWeaponData.resetTime : 1.0f;
        if (Time.time - lastAttackTime > resetTime) currentComboStep = 0;
    }

    private void TryAttack()
    {
        if (currentWeaponData == null) return;
        if (Time.time - lastAttackTime < currentWeaponData.attackCooldown) return;
        StartCoroutine(PerformAttackRoutine(currentWeaponData.comboLength));
    }

    private IEnumerator PerformAttackRoutine(int maxCombo)
    {
        isSwinging = true;
        lastAttackTime = Time.time;

        if (stateFlags != null) stateFlags.isSwingingWeapon = true;

        // 1. ЗАПУСКАЕМ АНИМАЦИЮ (Замах пошел!)
        if (characterAnimator != null)
        {
            characterAnimator.SetInteger("ComboStep", currentComboStep);
            characterAnimator.SetTrigger("Attack");
        }

        // --- ВЫБОР РУКИ ---
        Collider activeCollider = null;
        bool useLeftHand = currentWeaponData != null && currentWeaponData.isDualWeapon && (currentComboStep % 2 != 0);

        if (useLeftHand && leftHandHitbox != null) activeCollider = leftHandHitbox;
        else if (currentWeaponData != null) activeCollider = currentWeaponData.weaponHitbox;

        Hitbox activeHitboxScript = null;

        if (activeCollider != null && activeCollider.gameObject != null)
        {
            activeHitboxScript = activeCollider.GetComponent<Hitbox>();
            
            // ПРИНУДИТЕЛЬНО ОЧИЩАЕМ ПАМЯТЬ ХИТБОКСА (Перед новым ударом)
            if (activeHitboxScript != null) activeHitboxScript.ForceResetHitbox();
        }

        // 2. ЖДЕМ ВРЕМЯ ЗАМАХА (Windup)
        float windupTime = currentWeaponData != null ? currentWeaponData.attackWindup : 0.1f;
        yield return new WaitForSeconds(windupTime);

        // 3. УДАР! (Играем звук и включаем хитбокс)
        // Звук лучше играть именно здесь, когда меч "вжикает" по воздуху, а не в начале замаха
        if (currentWeaponData != null && audioSource && currentWeaponData.attackSound)
        {
            audioSource.PlayOneShot(currentWeaponData.attackSound);
        }

        if (activeCollider != null && activeCollider.gameObject != null)
        {
            // Включаем "опасную зону"
            if (activeHitboxScript != null) activeHitboxScript.StartAttack();
            else activeCollider.enabled = true;

            // 4. ЖДЕМ ВРЕМЯ ДЛИТЕЛЬНОСТИ АТАКИ (Сколько времени меч летит сквозь врага)
            float activeTime = currentWeaponData != null ? currentWeaponData.attackDuration : 0.2f;
            yield return new WaitForSeconds(activeTime);

            // 5. ВЫКЛЮЧАЕМ ХИТБОКС
            if (activeCollider != null && activeCollider.gameObject != null)
            {
                if (activeHitboxScript != null) activeHitboxScript.EndAttack();
                else activeCollider.enabled = false;
            }
        }
        else
        {
            // Если мы деремся без оружия (или хитбокс потерялся), просто ждем длительность удара
            float activeTime = currentWeaponData != null ? currentWeaponData.attackDuration : 0.2f;
            yield return new WaitForSeconds(activeTime);
        }

        // 6. ЗАВЕРШЕНИЕ
        isSwinging = false;
        if (stateFlags != null) stateFlags.isSwingingWeapon = false;
        
        currentComboStep++; 
        if (maxCombo > 0 && currentComboStep >= maxCombo) currentComboStep = 0;
    }
    // --- СМЕНА ОРУЖИЯ ---
    public void EquipWeapon(Weapon weaponAsset)
    {
        StopAllCoroutines(); 
        isSwinging = false;
        ClearWeaponObjects();

        if (weaponAsset == null || weaponAsset.prefab == null)
        {
            ClearWeapon();
            return;
        }

        currentWeaponData = weaponAsset;

        // 1. СОЗДАЕМ ПРАВОЕ ОРУЖИЕ (Главный префаб)
        currentWeaponObject = Instantiate(currentWeaponData.prefab); 
        currentWeaponObject.transform.SetParent(rightHand, true);   
        ResetTransform(currentWeaponObject);
        
        // --- Автопоиск хитбокса ПРАВОЙ руки ---
        Collider rightCol = FindHitbox(currentWeaponObject);
        if (rightCol != null)
        {
            // ЗАПИСЫВАЕМ ССЫЛКУ в старую переменную, чтобы не ломать PerformAttackRoutine!
            currentWeaponData.weaponHitbox = rightCol; 
            SetupHitboxScript(rightCol, currentWeaponData.damage, currentWeaponData.impactPower);
        }

        // 2. СОЗДАЕМ ЛЕВОЕ ОРУЖИЕ (Если двойное и указан отдельный префаб)
        // ТЕПЕРЬ МЫ ИСПОЛЬЗУЕМ offHandPrefab ВМЕСТО leftHandChildObject
        if (currentWeaponData.isDualWeapon && currentWeaponData.offHandPrefab != null && leftHand != null)
        {
            leftHandModelObject = Instantiate(currentWeaponData.offHandPrefab);
            leftHandModelObject.transform.SetParent(leftHand, true);
            ResetTransform(leftHandModelObject);

            // --- Автопоиск хитбокса ЛЕВОЙ руки ---
            leftHandHitbox = FindHitbox(leftHandModelObject);
            if (leftHandHitbox != null)
            {
                SetupHitboxScript(leftHandHitbox, currentWeaponData.damage, currentWeaponData.impactPower);
            }
        }
        else
        {
            leftHandModelObject = null;
            leftHandHitbox = null;
        }

        // 3. Аниматор и сброс
        if (characterAnimator != null && currentWeaponData.weaponAnimatorOverride != null)
            characterAnimator.runtimeAnimatorController = currentWeaponData.weaponAnimatorOverride;
        
        currentComboStep = 0;
        isSwinging = false;
        isBlocking = false;
        
        UpdateAnimatorCombatState();
    }

    // --- Вспомогательные методы (добавь их в конец скрипта PlayerCombat, если их еще нет) ---

    private Collider FindHitbox(GameObject weaponObj)
    {
        Collider[] cols = weaponObj.GetComponentsInChildren<Collider>();
        foreach (var c in cols)
        {
            if (c.isTrigger) return c; 
        }
        return null;
    }

    private void SetupHitboxScript(Collider col, float damage, float impact) // Добавили параметр
    {
        col.enabled = false; 
        Hitbox hitScript = col.GetComponent<Hitbox>();
        
        if (hitScript != null) 
        {
            hitScript.SetDamage(damage);
            hitScript.currentImpactPower = impact; // Передаем импакт!
            hitScript.mainAudioSource = this.audioSource;
        }
    }

    public void ClearWeapon()
    {
        StopAllCoroutines(); 
        isSwinging = false;
        ClearWeaponObjects();
        currentWeaponData = null;
        leftHandModelObject = null;
        leftHandHitbox = null;

        if (characterAnimator != null)
            characterAnimator.runtimeAnimatorController = defaultAnimatorController;
            
        currentComboStep = 0;
        isSwinging = false;
        isBlocking = false;

        UpdateAnimatorCombatState();
    }

    private void ClearWeaponObjects()
    {
        // Достаточно удалить корневой объект правой руки?
        // НЕТ, мы же оторвали левую руку и перенесли её. Её тоже надо удалить.
        
        if (currentWeaponObject != null) Destroy(currentWeaponObject);
        
        // Удаляем левую часть отдельно, т.к. она теперь ребенок LeftHand, а не currentWeaponObject
        if (leftHandModelObject != null) Destroy(leftHandModelObject);
    }

    private void ResetTransform(GameObject obj)
    {
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
    }
    
    // ... Остальные методы (UpdateAnimatorCombatState и т.д.) без изменений ...
    private void UpdateAnimatorCombatState()
    {
        if (characterAnimator != null)
        {
            bool hasWeapon = (currentWeaponData != null);
            characterAnimator.SetBool("WeaponEquipped", hasWeapon);
        }
    }
}