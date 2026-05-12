using UnityEngine;
using System.Collections.Generic;

public class Hitbox : MonoBehaviour
{
    [Header("Settings (Audio)")]
    public AudioClip hitSound; 
    public AudioClip BlockSound;
    public AudioClip parrySound;
    public AudioClip dodgedSound;

    [Header("Status Effects")]
    public List<StatusEffectData> effectsToApply = new List<StatusEffectData>();

    
    protected float damageAmount; 
    [HideInInspector] public AudioSource mainAudioSource; 
    [HideInInspector] public float currentImpactPower = 1f;
    protected Collider myCollider;
    protected List<Collider> hitColliders = new List<Collider>();

    private void Awake()
    {
        myCollider = GetComponent<Collider>();
    }

    public void SetDamage(float amount)
    {
        damageAmount = amount;
    }

    public void StartAttack()
    {
        hitColliders.Clear();
        if (myCollider != null) myCollider.enabled = true;
    }

    public void EndAttack()
    {
        if (myCollider != null) myCollider.enabled = false;
    }

    public void ForceResetHitbox()
    {
        hitColliders.Clear();
        if (myCollider != null) myCollider.enabled = false;
    }

    // --- ЛОГИКА УДАРА ИГРОКА ПО ВРАГУ ---
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (myCollider == null || !myCollider.enabled) return;

        EnemyHealthSystem enemyHealth = other.GetComponentInParent<EnemyHealthSystem>();
        if (enemyHealth == null) return;

        // --- УМНАЯ ЗАЩИТА ОТ ДВОЙНОГО УДАРА (по корню объекта) ---
        foreach (Collider c in hitColliders)
        {
            if (c != null && c.transform.root == other.transform.root) return;
        }
        hitColliders.Add(other);

        StatsContainer enemyStats = enemyHealth.GetComponentInParent<StatsContainer>();
        StatusEffectManager enemyEffectManager = enemyHealth.GetComponentInParent<StatusEffectManager>();

        bool attackLanded = false; 

        if (enemyStats != null)
        {
            bool isDodging = enemyStats.DodgeInviсible;
            bool isParrying = enemyStats.IsParry;
            bool isBlocking = enemyStats.IsBlock;
            // Передаем позицию центра игрока
            // Пытаемся найти позицию физического тела атакующего
            Vector3 attackerPos = transform.position; // Запасной вариант (позиция меча)

            // Ищем контроллер или агента "выше" по иерархии от меча
            var parentCC = GetComponentInParent<CharacterController>();
            var parentAgent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();

            if (parentCC != null) attackerPos = parentCC.transform.position;
            else if (parentAgent != null) attackerPos = parentAgent.transform.position;

            // --- 1. УКЛОНЕНИЕ ---
            if (isDodging)
            {
                if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
            }
            // --- 2. ПАРИРОВАНИЕ ---
            else if (isParrying)
            {
                float efficiency = 1f; // 100% срез урона при парировании (или можно взять enemyStats.ParryEfficiency.Value)
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                if (dmg > 0) enemyHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
                // Импакт не накладывается
            }
            // --- 3. БЛОК ---
            else if (isBlocking)
            {
                float efficiency = 0.5f; // 50% срез (или можно взять enemyStats.BlockEfficiency.Value)
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                if (dmg > 0) enemyHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);

                enemyStats.TakeImpact(currentImpactPower, true, attackerPos); // <--- НАКЛАДЫВАЕМ ИМПАКТ (В БЛОК)
                attackLanded = true; 
            }
            // --- 4. ПРОСТОЙ УДАР ---
            else
            {
                enemyHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);

                enemyStats.TakeImpact(currentImpactPower, false, attackerPos); // <--- НАКЛАДЫВАЕМ ИМПАКТ (ЧИСТЫЙ)
                attackLanded = true; 
            }

            // --- 5. ЭФФЕКТЫ ---
            if (enemyEffectManager != null && effectsToApply.Count > 0)
            {
                ApplyEffectsLogic(enemyEffectManager, attackLanded, isDodging, isParrying, isBlocking);
            }
        }
        else
        {
            // ЕСЛИ ВРАГ ТУПОЙ (Без StatsContainer)
            enemyHealth.GetDamage(Mathf.RoundToInt(damageAmount));
            if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
        }
    }

    // Вынес логику эффектов в отдельный защищенный метод, чтобы оба класса могли им пользоваться
    protected void ApplyEffectsLogic(StatusEffectManager manager, bool landed, bool dodge, bool parry, bool block)
    {
        if (effectsToApply == null || effectsToApply.Count == 0) return;

        foreach (var effect in effectsToApply)
        {
            if (effect == null) continue;

            bool apply = false;
            if (landed) 
            {
                apply = true;
                if (block && !effect.canIgnoreBlock) apply = false;
            } 
            else 
            {
                if (dodge && effect.canIgnoreDodge) apply = true;
                if (parry && effect.canIgnoreParry) apply = true;
            }
            
            if (apply) manager.ApplyEffect(effect);
        }
    }
}