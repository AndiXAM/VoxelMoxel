using UnityEngine;

public class EnemyHealthSystem : MonoBehaviour
{
    [Header("Здоровье")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    [Header("Идентификатор Врага (Для Квестов)")]
    public EnemyData myEnemyType;

    [Header("Visual Effects")]
    public DamageFlasher damageFlasher;

    [Header("Смерть")]
    [Tooltip("Что удалить при смерти? Обычно это корень врага (gameObject)")]
    [SerializeField] private GameObject deathTarget;

    private bool isDead = false;

    private void Start()
    {
        // 1. Пытаемся взять ХП из статов (Если это Умный Враг)
        StatsContainer stats = GetComponent<StatsContainer>();
        if (stats != null)
        {
            maxHealth = Mathf.RoundToInt(stats.MaxHealth.Value);
        }

        // 2. Инициализируем текущее здоровье
        currentHealth = maxHealth;

        // Если не назначили цель для удаления - удаляем сами себя
        if (deathTarget == null)
        {
            deathTarget = gameObject;
        }
    }

    public void GetDamage(int damage)
    {
        // Защита от получения урона после смерти (чтобы лут не сыпался дважды)
        if (isDead) return;

        // Вычисляем процент урона для врага
        float damagePercent = (float)damage / maxHealth;

        // ВЫЗЫВАЕМ ВСПЫШКУ
        if (damageFlasher != null && damage > 0)
        {
            damageFlasher.Flash(damagePercent);
        }

        // Отнимаем здоровье
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        // Проверяем смерть
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    // Если у тебя появится механика лечения врагов (например, вампиризм или лекари)
    public void HealDamage(int healAmount)
    {
        if (isDead) return;

        // Если статы могли измениться, обновляем максимум
        StatsContainer stats = GetComponent<StatsContainer>();
        if (stats != null) maxHealth = Mathf.RoundToInt(stats.MaxHealth.Value);

        currentHealth += healAmount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }

    private void Die()
    {
        isDead = true;

        // --- 1. КВЕСТЫ: СООБЩАЕМ ОБ УБИЙСТВЕ ---
        if (QuestManager.Instance != null && myEnemyType != null)
        {
            QuestManager.Instance.OnEventOccurred(myEnemyType); 
        }

        // --- 2. ЛУТ: ВЫСЫПАЕМ ПРЕДМЕТЫ ---
        EnemyLoot lootSystem = GetComponent<EnemyLoot>();
        if (lootSystem != null)
        {
            lootSystem.DropLoot();
        } 

        // --- 3. УНИЧТОЖЕНИЕ ---
        // (Здесь в будущем можно добавить запуск анимации смерти вместо мгновенного Destroy,
        //  и вызывать Destroy уже в самом конце анимации через Animation Event).
        
        Destroy(deathTarget);
    }
}