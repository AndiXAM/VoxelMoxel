using UnityEngine;

public class EnemyHitbox : Hitbox 
{
    protected override void OnTriggerEnter(Collider other)
    {
        // 1. Базовая проверка
        if (myCollider == null || !myCollider.enabled) return;

        // 2. Ищем здоровье ИГРОКА (маркер живого существа)
        HealthSystem targetHealth = other.GetComponentInParent<HealthSystem>();
        if (targetHealth == null) return;

        // 3. ЗАЩИТА ОТ ДВОЙНОГО УДАРА (по CharacterController + CapsuleCollider)
        foreach (Collider c in hitColliders)
        {
            if (c != null && c.transform.root == other.transform.root) return;
        }
        hitColliders.Add(other);

        // 4. Получаем компоненты игрока
        StatsContainer playerStats = targetHealth.GetComponentInParent<StatsContainer>();
        StatusEffectManager targetEffectManager = targetHealth.GetComponentInParent<StatusEffectManager>();

        if (playerStats != null)
        {
            bool isDodging = playerStats.DodgeInviсible;
            bool isParrying = playerStats.IsParry;
            bool isBlocking = playerStats.IsBlock;
            bool attackLanded = false;
            // Пытаемся найти позицию физического тела атакующего
            Vector3 attackerPos = transform.position; // Запасной вариант (позиция меча)

            // Ищем контроллер или агента "выше" по иерархии от меча
            var parentCC = GetComponentInParent<CharacterController>();
            var parentAgent = GetComponentInParent<UnityEngine.AI.NavMeshAgent>();

            if (parentCC != null) attackerPos = parentCC.transform.position;
            else if (parentAgent != null) attackerPos = parentAgent.transform.position;

            // --- ЛОГИКА УРОНА И ИМПАКТА ---
            if (isDodging)
            {
                if (mainAudioSource != null && dodgedSound != null) mainAudioSource.PlayOneShot(dodgedSound);
            }
            else if (isParrying)
            {
                float efficiency = 1f; 
                // Защита от отсутствия стата в контейнере
                if (playerStats.GetStat(StatType.Armor) != null) // замени на проверку ParryEfficiency, если он есть в Enum
                {
                    // efficiency = playerStats.ParryEfficiency.Value; 
                }

                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency)); 
                if (dmg > 0) targetHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && parrySound != null) mainAudioSource.PlayOneShot(parrySound);
                
                // Импакт НЕ НАКЛАДЫВАЕТСЯ при парировании (игрок не ошеломляется)
                
                // Снимаем усталость с игрока (поощряем за идеальное парирование)
                playerStats.ParryFatige = false; 
            }
            else if (isBlocking)
            {
                float efficiency = 0.5f; // базовое значение
                // efficiency = playerStats.BlockEfficiency.Value; 
                
                int dmg = Mathf.RoundToInt(damageAmount * (1f - efficiency));
                if (dmg > 0) targetHealth.GetDamage(dmg);
                
                if (mainAudioSource != null && BlockSound != null) mainAudioSource.PlayOneShot(BlockSound);
                
                // ВРАГ НАКЛАДЫВАЕТ ИМПАКТ НА ИГРОКА (В БЛОК)
                playerStats.TakeImpact(currentImpactPower, true, attackerPos);

                attackLanded = true; 
            }
            else
            {
                targetHealth.GetDamage(Mathf.RoundToInt(damageAmount));
                
                if (mainAudioSource != null && hitSound != null) mainAudioSource.PlayOneShot(hitSound);
                
                // ВРАГ НАКЛАДЫВАЕТ ИМПАКТ НА ИГРОКА (ЧИСТЫЙ УДАР)
                playerStats.TakeImpact(currentImpactPower, false, attackerPos);

                attackLanded = true; 
            }

            // --- ЭФФЕКТЫ ---
            if (targetEffectManager != null && effectsToApply.Count > 0)
            {
                // Вызываем метод из базового класса Hitbox.cs
                ApplyEffectsLogic(targetEffectManager, attackLanded, isDodging, isParrying, isBlocking);
            }
        }
    }
}