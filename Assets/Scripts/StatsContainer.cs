using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class EquipmentStatBonus
{
    public StatType statType;     // Какой стат меняем (из StatsContainer.cs)
    public float value;           // На сколько меняем
    public StatModType modType;   // Тип модификатора (Flat, PercentAdd, PercentMult)
}

// --- 1. ОПРЕДЕЛЕНИЕ ТИПОВ СТАТОВ (Этого не хватало) ---
public enum StatType
{
    Health,
    MaxHealth,
    Damage,
    Armor,
    Speed,          // Скорость бега
    JumpHeight,     // Высота прыжка
    CriticalChance,  // Шанс крита
    BlockEfficiency,
    ParryEfficiency
}

// --- 2. ТИПЫ МОДИФИКАТОРОВ ---
public enum StatModType
{
    Flat = 100,       // Просто прибавить число (например +10 к урону)
    PercentAdd = 200, // Прибавить % от базы (например +10% силы)
    PercentMult = 300 // Умножить итоговое значение (например x1.5 урона)
}

// --- 3. КЛАСС МОДИФИКАТОРА ---
[System.Serializable]
public class StatModifier
{
    public float Value;
    public StatModType Type;
    public object Source; // Кто наложил эффект (меч, зелье), чтобы потом легко удалить

    public StatModifier(float value, StatModType type, object source = null)
    {
        Value = value;
        Type = type;
        Source = source;
    }
}

// --- 4. КЛАСС ХАРАКТЕРИСТИКИ ---
[System.Serializable]
public class Stat
{
    [SerializeField] public float BaseValue; 

    // --- НОВАЯ СТРОЧКА: СОБЫТИЕ ---
    public event Action OnValueChanged; 

    private readonly List<StatModifier> _modifiers = new List<StatModifier>();
    protected bool _isDirty = true;
    protected float _lastValue;

    public Stat(float baseValue = 0)
    {
        BaseValue = baseValue;
    }

    public float Value 
    {
        get 
        {
            if (_isDirty)
            {
                _lastValue = CalculateFinalValue();
                _isDirty = false;
            }
            return _lastValue;
        }
    }

    public void SetBaseValue(float value)
    {
        BaseValue = value;
        _isDirty = true;
        OnValueChanged?.Invoke(); // Сообщаем об изменении!
    }

    public void AddModifier(StatModifier mod)
    {
        _isDirty = true;
        _modifiers.Add(mod);
        OnValueChanged?.Invoke(); // Сообщаем об изменении!
    }

    public bool RemoveModifier(StatModifier mod)
    {
        if (_modifiers.Remove(mod))
        {
            _isDirty = true;
            OnValueChanged?.Invoke(); // Сообщаем об изменении!
            return true;
        }
        return false;
    }

    public bool RemoveAllModifiersFromSource(object source)
    {
        bool didRemove = false;
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].Source == source)
            {
                _isDirty = true;
                didRemove = true;
                _modifiers.RemoveAt(i);
            }
        }
        
        if (didRemove) 
        {
            OnValueChanged?.Invoke(); // Сообщаем об изменении!
        }
        
        return didRemove;
    }

    // Математика расчета (Base -> Flat -> Percent -> Mult)
    private float CalculateFinalValue()
    {
        float finalValue = BaseValue;
        float sumPercentAdd = 0;

        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];

            if (mod.Type == StatModType.Flat)
            {
                finalValue += mod.Value;
            }
            else if (mod.Type == StatModType.PercentAdd)
            {
                sumPercentAdd += mod.Value; // Складываем проценты (10% + 10% = 20%, а не 21%)
            }
        }

        // Применяем проценты
        finalValue *= 1 + sumPercentAdd;

        // Применяем мультипликаторы (редкие эффекты, x2 урона и т.д.)
        for (int i = 0; i < _modifiers.Count; i++)
        {
            StatModifier mod = _modifiers[i];
            if (mod.Type == StatModType.PercentMult)
            {
                finalValue *= mod.Value;
            }
        }

        // Округляем до 4 знаков, чтобы избежать ошибок float (типа 9.999999)
        return (float)Math.Round(finalValue, 4);
    }
}

// --- 5. КОНТЕЙНЕР СТАТОВ (Вешаем на Игрока) ---
public class StatsContainer : MonoBehaviour, ISaveable
{

    [Header("Settings")]
    public bool isPlayer = false; 
    // Объявляем переменные явно, чтобы видеть их в Инспекторе Unity!
    [Header("Main Stats")]
    public Stat Health = new Stat(100);
    public Stat MaxHealth = new Stat(100);
    public Stat Damage = new Stat(10);
    public Stat Armor = new Stat(0);

    public Stat BlockEfficiency = new Stat(0);
    public Stat ParryEfficiency = new Stat(0);

    
    [Header("Movement")]
    public Stat MoveSpeed = new Stat(6);
    public Stat JumpHeight = new Stat(2);
    public Stat CriticalChance = new Stat(0.05f);

    [Header("Status Effects")]
    public bool DodgeInviсible = false; // Неуязвимость при перекате

    public bool IsBlock = false;

    public bool IsParry = false;

    public bool ParryFatige = false;

    // Словарь нужен только если ты хочешь искать статы по Enum
    private Dictionary<StatType, Stat> _statMap;

    [Header("Current Class")]
    public ClassData equippedClass; // Текущий надетый класс

    [Header("Защита от Импакта (Stagger)")]
    public Stat ImpactResistance = new Stat(0); // По умолчанию 0



    // --- ПЕРЕМЕННЫЕ ИМПАКТА ДЛЯ ДВИЖЕНИЯ ---
    [HideInInspector] public float ImpactSpeedMultiplier = 1f;
    [HideInInspector] public Vector3 CurrentKnockbackVelocity = Vector3.zero;

    private Coroutine impactCoroutine;

    

    private void Awake()
    {
        // Заполняем словарь для удобного поиска (если нужно)
        _statMap = new Dictionary<StatType, Stat>
        {
            { StatType.Health, Health },
            { StatType.MaxHealth, MaxHealth },
            { StatType.Damage, Damage },
            { StatType.Armor, Armor },
            { StatType.Speed, MoveSpeed },
            { StatType.JumpHeight, JumpHeight },
            { StatType.CriticalChance, CriticalChance },
            {StatType.BlockEfficiency, BlockEfficiency},
            {StatType.ParryEfficiency, ParryEfficiency}
        };
    }

    // Метод получения стата по типу
    public Stat GetStat(StatType type)
    {
        if (_statMap != null && _statMap.ContainsKey(type))
            return _statMap[type];
        return null;
    }
    


    public void EquipClass(ClassData newClass)
    {
        if (newClass == null || equippedClass == newClass) return;

        // 1. УДАЛЯЕМ БОНУСЫ СТАРОГО КЛАССА
        if (equippedClass != null)
        {
            foreach (var bonus in equippedClass.statBonuses)
            {
                Stat stat = GetStat(bonus.statType);
                if (stat != null) 
                {
                    // Удаляем модификаторы, источником которых был старый класс
                    stat.RemoveAllModifiersFromSource(equippedClass);
                }
            }
        }

        // 2. МЕНЯЕМ КЛАСС
        equippedClass = newClass;
        Debug.Log($"Надет новый класс: {equippedClass.className}");

        // 3. ДОБАВЛЯЕМ БОНУСЫ НОВОГО КЛАССА
        foreach (var bonus in equippedClass.statBonuses)
        {
            Stat stat = GetStat(bonus.statType);
            if (stat != null)
            {
                // Создаем модификатор. ВАЖНО: передаем 'equippedClass' как Источник!
                StatModifier mod = new StatModifier(bonus.value, bonus.modType, equippedClass);
                stat.AddModifier(mod);
            }
        }
        
    }

    // --- ИНТЕРФЕЙС СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        if (!isPlayer) return;
        // Сохраняем только ИМЯ файла класса (например "Warrior")
        if (equippedClass != null) data.equippedClassName = equippedClass.name;
        else data.equippedClassName = "";
    }

    public void LoadData(SaveData data)
    {
        if (!isPlayer) return;
        if (!string.IsNullOrEmpty(data.equippedClassName))
        {
            // БЕРЕМ ИЗ БАЗЫ ДАННЫХ ВМЕСТО RESOURCES!
            ClassData loadedClass = SaveManager.Instance.database.GetClassByName(data.equippedClassName);
            
            if (loadedClass != null) EquipClass(loadedClass);
            else Debug.LogWarning($"Не удалось найти класс {data.equippedClassName} в GameDatabase!");
        }
    }

    

    // --- МЕТОД ПРИЕМА ИМПАКТА ---

    // данные методы влияют на характеристики скорости персонажа
    public void TakeImpact(float attackerImpact, bool isBlocked, Vector3 pushDirection)
    {
        float finalImpact = attackerImpact;

        if (isBlocked)
        {
            float blockEff = 0.5f; // замени на свою переменную эффективности, если нужно
            finalImpact *= (1f - blockEff); 
        }

        finalImpact -= ImpactResistance.Value;
        if (finalImpact <= 0) return;

        // Вектор нам уже передали в готовом виде! Просто нормализуем на всякий случай
        Vector3 finalDir = pushDirection.normalized;

        // 1. Замедление (остается в корутине статов, так как это просто циферка-множитель)
        if (impactCoroutine != null) StopCoroutine(impactCoroutine);
        impactCoroutine = StartCoroutine(ImpactRoutine(Mathf.Min(finalImpact, 1f)));

        // 2. ОТБРАСЫВАНИЕ (Knockback)
        // Больше никаких корутин и StopCoroutine! Просто записываем силу напрямую.
        if (finalImpact > 1f)
        {
            float knockbackForce = finalImpact - 1f;
            
            // Задаем стартовый вектор силы.
            // Скрипты движения (Character/AI) увидят этот вектор, сдвинут тело и плавно погасят его в своем Update.
            CurrentKnockbackVelocity = finalDir * knockbackForce * 15f; 
        }
    }
    private System.Collections.IEnumerator ImpactRoutine(float severity)
    {
        severity = Mathf.Clamp(severity, 0.1f, 1f);

        // Фаза 1: Жесткое замедление (1.0 = скорость 0%)
        ImpactSpeedMultiplier = 1f - severity;
        ImpactSpeedMultiplier = Mathf.Clamp(ImpactSpeedMultiplier, 0.2f, 1f); // Минимум 20% скорости
        
        yield return new WaitForSeconds(0.5f * severity); // Оглушение длится до 0.5 сек

        // Фаза 2: Плавное восстановление
        float recoveryTime = 0.5f * severity; 
        float timer = 0f;
        float startMultiplier = ImpactSpeedMultiplier;

        while (timer < recoveryTime)
        {
            timer += Time.deltaTime;
            ImpactSpeedMultiplier = Mathf.Lerp(startMultiplier, 1f, timer / recoveryTime);
            yield return null; 
        }

        ImpactSpeedMultiplier = 1f;
        impactCoroutine = null;
    }

}