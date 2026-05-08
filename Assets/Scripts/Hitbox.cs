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
        if (myCollider == null || !myCollider.enabled || hitColliders.Contains(other)) return;

        EnemyHealthSystem enemyHealth = other.GetComponentInParent<EnemyHealthSystem>();
        StatsContainer enemyStats = other.GetComponentInParent<StatsContainer>();
        StatusEffectManager enemyEffectManager = other.GetComponentInParent<StatusEffectManager>();

        if (enemyHealth != null)
        {
            bool attackLanded = false; // Флаг: прошел ли физический удар?

            if (enemyStats != null)
            {
                // ИЩЕМ СТАТУСЫ ВРАГА (Умный враг держит их в StatsContainer)
                bool isDodging = enemyStats.DodgeInviсible;
                bool isParrying = enemyStats.IsParry;
                bool isBlocking = enemyStats.IsBlock;

                // --- 1. УКЛОНЕНИЕ ---
                if (isDodging)
                {
                    if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
                }
                // --- 2. ПАРИРОВАНИЕ ---
                else if (isParrying)
                {
                    float efficiency = 1f; // 100% срез
                    // Если у врага есть ParryEfficiency, раскомментируй строку ниже:
                    // efficiency = enemyStats.ParryEfficiency.Value;

                    int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                    if (dmg > 0) enemyHealth.GetDamage(dmg);
                    
                    if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
                }
                // --- 3. БЛОК ---
                else if (isBlocking)
                {
                    float efficiency = 0.5f; // 50% срез
                    // efficiency = enemyStats.BlockEfficiency.Value;

                    int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                    if (dmg > 0) enemyHealth.GetDamage(dmg);
                    
                    if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);
                    
                    attackLanded = true; 
                }
                // --- 4. ПРОСТОЙ УДАР ---
                else
                {
                    enemyHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                    if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
                    
                    attackLanded = true; 
                }
            }
            // ЕСЛИ ВРАГ ТУПОЙ
            else
            {
                enemyHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
                
                attackLanded = true;
            }

            // --- 5. ЛОГИКА НАЛОЖЕНИЯ ЭФФЕКТОВ НА ВРАГА ---
            if (enemyEffectManager != null && effectsToApply.Count > 0)
            {
                // Снова читаем статусы для эффектов
                bool isDodging = enemyStats != null ? enemyStats.DodgeInviсible : false;
                bool isParrying = enemyStats != null ? enemyStats.IsParry : false;
                bool isBlocking = enemyStats != null ? enemyStats.IsBlock : false;

                foreach (var effect in effectsToApply)
                {
                    bool shouldApply = false;

                    if (attackLanded)
                    {
                        shouldApply = true;
                        
                        // Если враг в блоке, а эффект НЕ пробивает блок -> не накладываем
                        if (isBlocking && !effect.canIgnoreBlock) shouldApply = false;
                    }
                    else
                    {
                        if (isDodging && effect.canIgnoreDodge) shouldApply = true;
                        if (isParrying && effect.canIgnoreParry) shouldApply = true;
                    }

                    if (shouldApply) enemyEffectManager.ApplyEffect(effect);
                }
            }

            hitColliders.Add(other);
        }
    }
}