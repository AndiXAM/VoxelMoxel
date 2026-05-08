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
    
    private Collider rightHandHitbox; // <-- Теперь коллайдер тут
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

    // UPDATE УДАЛЕН. Оружие выдается ОДИН РАЗ в Start.

    private void EquipWeapon(Weapon weaponAsset)
    {
        if (weaponAsset == null || weaponAsset.prefab == null) return;

        currentWeaponData = weaponAsset;

        // --- Правая рука ---
        currentRightWeaponObj = Instantiate(weaponAsset.prefab); // Создаем БЕЗ родителя сначала
        currentRightWeaponObj.transform.SetParent(rightHand, true); // СТАВИМ TRUE! 
        ResetTransform(currentRightWeaponObj);
        
        rightHandHitbox = FindHitbox(currentRightWeaponObj); // Ищем хитбокс
        if (rightHandHitbox != null)
        {
            SetupHitboxScript(rightHandHitbox, weaponAsset.damage);
        }

        // --- Левая рука ---
        if (weaponAsset.isDualWeapon && weaponAsset.offHandPrefab != null && leftHand != null)
        {
            currentLeftWeaponObj = Instantiate(weaponAsset.offHandPrefab);
            currentLeftWeaponObj.transform.SetParent(leftHand, true); // ТУТ БЫЛО rightHand и false! Исправил на leftHand и true
            ResetTransform(currentLeftWeaponObj);
            
            leftHandHitbox = FindHitbox(currentLeftWeaponObj);
            if (leftHandHitbox != null) SetupHitboxScript(leftHandHitbox, weaponAsset.damage);
        }

        // --- Аниматор ---
        if (animator != null && weaponAsset.weaponAnimatorOverride != null)
        {
            animator.runtimeAnimatorController = weaponAsset.weaponAnimatorOverride;
        }
        
        currentComboStep = 0;
        isSwinging = false;
    }

    public void TryAttack()
    {
        if (currentWeaponData == null || isSwinging) return;
        if (Time.time - lastAttackTime < currentWeaponData.attackCooldown) return;

        // Сброс хитбоксов перед атакой
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

        // Задержка до начала урона
        yield return new WaitForSeconds(currentWeaponData.attackWindup);

        if (audioSource && currentWeaponData.attackSound)
            audioSource.PlayOneShot(currentWeaponData.attackSound);

        // ВКЛЮЧАЕМ УРОН
        if (activeHitboxScript != null) activeHitboxScript.StartAttack();
        else if (activeCollider != null) activeCollider.enabled = true;

        // Длительность удара
        yield return new WaitForSeconds(currentWeaponData.attackDuration);

        // ВЫКЛЮЧАЕМ УРОН
        if (activeHitboxScript != null) activeHitboxScript.EndAttack();
        else if (activeCollider != null) activeCollider.enabled = false;

        isSwinging = false;
        currentComboStep++; 
        if (maxCombo > 0 && currentComboStep >= maxCombo) currentComboStep = 0;
    }

    private Collider FindHitbox(GameObject weaponObj)
    {
        if (weaponObj == null) return null;

        // Ищем включая неактивные объекты
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
}