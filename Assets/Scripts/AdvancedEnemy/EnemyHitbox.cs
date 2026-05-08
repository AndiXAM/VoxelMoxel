using UnityEngine;

public class EnemyHitbox : Hitbox 
{
    protected override void OnTriggerEnter(Collider other)
    {
        // 1. Базовая проверка
        if (myCollider == null || !myCollider.enabled) return;

        // 2. Ищем здоровье (маркер живого существа)
        HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
        if (targetHealth == null) return;

        // 3. ЗАЩИТА ОТ ДВОЙНОГО УДАРА (по CharacterController + CapsuleCollider)
        // Проверяем, не били ли мы уже этот объект (по его корню/root)
        foreach (Collider c in hitColliders)
        {
            if (c != null && c.transform.root == other.transform.root) return;
        }
        hitColliders.Add(other);

        // 4. Получаем компоненты
        StatsContainer playerStats = targetHealth.GetComponentInParent<StatsContainer>();
        StatusEffectManager targetEffectManager = targetHealth.GetComponentInParent<StatusEffectManager>();

        if (playerStats != null)
        {
            bool isDodging = playerStats.DodgeInviсible;
            bool isParrying = playerStats.IsParry;
            bool isBlocking = playerStats.IsBlock;
            bool attackLanded = false;

            // --- ЛОГИКА УРОНА ---
            if (isDodging)
            {
                if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
            }
            else if (isParrying)
            {
                float efficiency = playerStats.ParryEfficiency.Value; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                
                if (dmg > 0) targetHealth.GetDamage(dmg);
                if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
                
            }
            else if (isBlocking)
            {
                float efficiency = playerStats.BlockEfficiency.Value; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                
                if (dmg > 0) targetHealth.GetDamage(dmg);
                if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);
                
                attackLanded = true; 
            }
            else
            {
                targetHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
                
                attackLanded = true; 
            }

            // --- ЭФФЕКТЫ ---
            if (targetEffectManager != null && effectsToApply.Count > 0)
            {
                ApplyEffectsLogic(targetEffectManager, attackLanded, isDodging, isParrying, isBlocking);
            }
        }
    }

    private void ApplyEffectsLogic(StatusEffectManager manager, bool landed, bool dodge, bool parry, bool block)
    {
        foreach (var effect in effectsToApply)
        {
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