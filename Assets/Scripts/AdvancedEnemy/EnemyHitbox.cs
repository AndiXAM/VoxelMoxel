using UnityEngine;

public class EnemyHitbox : Hitbox 
{
    protected override void OnTriggerEnter(Collider other)
    {
        if (myCollider == null || !myCollider.enabled) return;

        HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
        if (targetHealth == null) return;

        foreach (Collider c in hitColliders)
        {
            if (c != null && c.transform.root == other.transform.root) return;
        }
        hitColliders.Add(other);

        StatsContainer playerStats = targetHealth.GetComponentInParent<StatsContainer>();
        StatusEffectManager targetEffectManager = targetHealth.GetComponentInParent<StatusEffectManager>();

        if (playerStats != null)
        {
            bool isDodging = playerStats.DodgeInviсible;
            bool isParrying = playerStats.IsParry;
            bool isBlocking = playerStats.IsBlock;
            bool attackLanded = false;

            // --- ВЫЧИСЛЕНИЕ НАПРАВЛЕНИЯ ОТБРОСА ---
            // Позиция тела игрока (нашей капсулы)
            Vector3 victimPos = other.transform.position; 
            
            // Ищем позицию тела врага (его капсулы с NavMeshAgent)
            Vector3 attackerPos = transform.position; // Фоллбэк
            var agent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) attackerPos = agent.transform.position;

            // Считаем чистый вектор "от Врага к Игроку"
            Vector3 pushDir = victimPos - attackerPos;
            pushDir.y = 0; // Игнорируем высоту
            pushDir.Normalize();
            // ----------------------------------------

            WeaponFlasher playerFlasher = targetHealth.GetComponentInParent<WeaponFlasher>();

            float maxHp = (playerStats != null && playerStats.MaxHealth != null) ? playerStats.MaxHealth.Value : 100f;
            float damagePercent = Mathf.Clamp01(damageAmount / maxHp);

            if (isDodging)
            {
                if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
                DamageFlasher playerBodyFlasher = targetHealth.GetComponentInParent<DamageFlasher>();
                if (playerBodyFlasher != null) playerBodyFlasher.FlashDodge();
            }
            else if (isParrying)
            {
                float efficiency = 1f; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                if (dmg > 0) targetHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
                
                playerStats.ParryFatige = false; 

                // ИГРОК СПАРИРОВАЛ: вспыхивает ТОЛЬКО меч игрока белым!
                if (playerFlasher != null) playerFlasher.Flash(Color.white, damagePercent);
            }
            else if (isBlocking)
            {
                float efficiency = 0.5f; 
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                if (dmg > 0) targetHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);
                
                playerStats.TakeImpact(currentImpactPower, true, pushDir);
                attackLanded = true; 

                // ИГРОК ЗАБЛОКИРОВАЛ: моргает ТОЛЬКО меч игрока черным!
                if (playerFlasher != null) playerFlasher.Flash(Color.black, damagePercent);
            }
            else
            {
                targetHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
                
                // Передаем ГОТОВЫЙ вектор pushDir!
                playerStats.TakeImpact(currentImpactPower, false, pushDir);

                attackLanded = true; 
            }

            if (targetEffectManager != null && effectsToApply.Count > 0)
            {
                ApplyEffectsLogic(targetEffectManager, attackLanded, isDodging, isParrying, isBlocking);
            }
        }
    }
}