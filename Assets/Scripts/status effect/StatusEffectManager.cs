using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ActiveStatusEffect
{
    public StatusEffectData data;
    public float timeRemaining;
    public int currentStacks;
    public float nextTickTime; 
}

[RequireComponent(typeof(StatsContainer))] // Менеджеру нужны статы для работы!
public class StatusEffectManager : MonoBehaviour
{
    public List<ActiveStatusEffect> activeEffects = new List<ActiveStatusEffect>();

    private HealthSystem healthSystem;
    private StatsContainer statsContainer; // Ссылка на наши статы

    public event Action OnStatusEffectsChanged;

    private void Awake() // Лучше использовать Awake для поиска компонентов
    {
        // 1. Ищем статы на этом же объекте (в корне)
        statsContainer = GetComponent<StatsContainer>();
        
        // 2. Ищем здоровье В ЛЮБОМ дочернем объекте (на капсуле)
        healthSystem = GetComponentInChildren<HealthSystem>();

        // Опционально: выведем предупреждения, если что-то забыли прицепить
        if (statsContainer == null) 
            Debug.LogWarning($"[StatusEffectManager] Ошибка: StatsContainer не найден на {gameObject.name}!");
            
        if (healthSystem == null) 
            Debug.LogWarning($"[StatusEffectManager] Ошибка: HealthSystem не найден внутри {gameObject.name}!");
    }

    private void Update()
    {
        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveStatusEffect effect = activeEffects[i];
            effect.timeRemaining -= Time.deltaTime;

            // Логика урона со временем
            if (effect.data.isDamageOverTime && healthSystem != null)
            {
                effect.nextTickTime -= Time.deltaTime;
                if (effect.nextTickTime <= 0)
                {
                    int tickDamage = effect.data.damagePerTick * effect.currentStacks;
                    healthSystem.GetDamage(tickDamage);
                    effect.nextTickTime = effect.data.tickInterval;
                }
            }

            // --- ОКОНЧАНИЕ ЭФФЕКТА ---
            if (effect.timeRemaining <= 0)
            {
                RemoveEffect(effect);
            }
        }
    }

    public void ApplyEffect(StatusEffectData effectData)
    {
        if (effectData == null) return;

        ActiveStatusEffect existingEffect = activeEffects.Find(e => e.data == effectData);

        if (existingEffect != null)
        {
            // ЭФФЕКТ УЖЕ ЕСТЬ
            if (effectData.isStackable && existingEffect.currentStacks < effectData.maxStacks)
            {
                existingEffect.currentStacks++;
                
                // Если стаки увеличились, нужно пересчитать бонусы к статам!
                // Проще всего удалить старые и наложить новые, умноженные на стаки.
                RemoveStatModifiers(existingEffect);
                ApplyStatModifiers(existingEffect);
            }

            if (effectData.refreshDurationOnNewStack)
            {
                existingEffect.timeRemaining = effectData.duration;
            }
        }
        else
        {
            // НОВЫЙ ЭФФЕКТ
            ActiveStatusEffect newEffect = new ActiveStatusEffect
            {
                data = effectData,
                timeRemaining = effectData.duration,
                currentStacks = 1,
                nextTickTime = effectData.tickInterval 
            };
            activeEffects.Add(newEffect);

            // Накладываем бонусы/штрафы к статам!
            ApplyStatModifiers(newEffect);
        }

        OnStatusEffectsChanged?.Invoke(); 
    }

    public void RemoveEffect(ActiveStatusEffect effect)
    {
        // 1. Очищаем статы от влияния этого эффекта
        RemoveStatModifiers(effect);

        // 2. Удаляем из списка активных
        activeEffects.Remove(effect);
        
        OnStatusEffectsChanged?.Invoke();
    }

    // --- ЛОГИКА МОДИФИКАЦИИ СТАТОВ ---

    private void ApplyStatModifiers(ActiveStatusEffect effect)
    {
        if (statsContainer == null || effect.data.statModifiers.Count == 0) return;

        foreach (var bonus in effect.data.statModifiers)
        {
            Stat statToModify = statsContainer.GetStat(bonus.statType);
            if (statToModify != null)
            {
                // Умножаем силу эффекта на количество стаков!
                // (Например: -5 к броне * 3 стака = -15 к броне)
                float finalValue = bonus.value * effect.currentStacks;

                // Создаем модификатор. В качестве Source передаем САМ ОБЪЕКТ ЭФФЕКТА,
                // чтобы потом мы могли точно знать, чьи бонусы удалять.
                StatModifier mod = new StatModifier(finalValue, bonus.modType, effect);
                statToModify.AddModifier(mod);
            }
        }
    }

    private void RemoveStatModifiers(ActiveStatusEffect effect)
    {
        if (statsContainer == null || effect.data.statModifiers.Count == 0) return;

        foreach (var bonus in effect.data.statModifiers)
        {
            Stat statToModify = statsContainer.GetStat(bonus.statType);
            if (statToModify != null)
            {
                // Удаляем все модификаторы, источником которых является этот конкретный эффект
                statToModify.RemoveAllModifiersFromSource(effect);
            }
        }
    }
}