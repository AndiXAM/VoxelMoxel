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

            // --- ВЫЧИСЛЕНИЕ НАПРАВЛЕНИЯ ОТБРОСА ---
            // Позиция тела врага (капсулы)
            Vector3 victimPos = other.transform.position; 
            
            // Ищем позицию тела игрока (нашей капсулы с CharacterController)
            Vector3 attackerPos = transform.position; // Фоллбэк
            var cc = GetComponentInParent<CharacterController>();
            if (cc != null) attackerPos = cc.transform.position;

            // Считаем чистый вектор "от Игрока к Врагу"
            Vector3 pushDir = victimPos - attackerPos;
            pushDir.y = 0; // Игнорируем высоту
            pushDir.Normalize();
            // ----------------------------------------

            if (isDodging)
            {
                if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
            }
            else if (isParrying)
            {
                float efficiency = 1f; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                if (dmg > 0) enemyHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
            }
            else if (isBlocking)
            {
                float efficiency = 0.5f; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                if (dmg > 0) enemyHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);

                // Передаем ГОТОВЫЙ вектор pushDir!
                enemyStats.TakeImpact(currentImpactPower, true, pushDir); 
                attackLanded = true; 
            }
            else
            {
                enemyHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);

                // Передаем ГОТОВЫЙ вектор pushDir!
                enemyStats.TakeImpact(currentImpactPower, false, pushDir); 
                attackLanded = true; 
            }

            if (enemyEffectManager != null && effectsToApply.Count > 0)
            {
                ApplyEffectsLogic(enemyEffectManager, attackLanded, isDodging, isParrying, isBlocking);
            }
        }
        else
        {
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