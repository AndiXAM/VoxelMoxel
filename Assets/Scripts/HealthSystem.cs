using UnityEngine;
using UnityEngine.UI;

public class HealthSystem : MonoBehaviour, ISaveable
{
    [SerializeField] StatsContainer playerStats;

    [Header("Visual Effects")]
    public DamageFlasher damageFlasher;
    
    [Header("Settings")]
    private float currentHealth; 
    private float maxHealth;     

    [Header("References")]
    [SerializeField] private Image fillImage;
    [SerializeField] private Death death; 
    [SerializeField] private CharacterAnimatorController animationController; 

    // --- ПОДПИСКА НА СОБЫТИЯ ---
    private void OnEnable()
    {
        // Защита от ошибок при старте
        if (playerStats != null && playerStats.MaxHealth != null)
        {
            // Говорим: Когда MaxHealth изменится, вызови метод HandleMaxHealthChanged
            playerStats.MaxHealth.OnValueChanged += HandleMaxHealthChanged;
        }
    }

    private void OnDisable()
    {
        // Обязательно отписываемся при выключении скрипта, чтобы не было утечек памяти
        if (playerStats != null && playerStats.MaxHealth != null)
        {
            playerStats.MaxHealth.OnValueChanged -= HandleMaxHealthChanged;
        }
    }

    void Start()
    {
        maxHealth = playerStats.MaxHealth.Value;
        currentHealth = maxHealth;
        UpdateHealthBar();
    }

    // --- ЭТОТ МЕТОД ВЫЗОВЕТСЯ АВТОМАТИЧЕСКИ, КОГДА НАДЕНУТ КЛАСС/БРОНЮ НА +ХП ---
    private void HandleMaxHealthChanged()
    {
        // Обновляем наш лимит
        maxHealth = playerStats.MaxHealth.Value;

        // Важное правило: если мы сняли броню и максимальное ХП упало,
        // наше текущее ХП не должно быть больше нового максимума
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        // Перерисовываем UI
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (fillImage != null)
        {
            fillImage.fillAmount = currentHealth / maxHealth;
        }
    }

    public void GetDamage(int damage) // Или float, если урон дробный
    {
        // Вычитаем урон
        currentHealth -= damage;

        // Обновляем интерфейс
        UpdateHealthBar();

        // Вычисляем процент снесенного ХП (например: 20 урона / 100 макс = 0.2f)
        float damagePercent = (float)damage / maxHealth;

        // ВЫЗЫВАЕМ ВСПЫШКУ
        if (damageFlasher != null && damage > 0)
        {
            damageFlasher.Flash(damagePercent);
        }

        // Проверяем смерть
        if (currentHealth <= 0)
        {
            currentHealth = 0; // Чтобы не уходить в минус
            
            // Логика смерти
            if (death != null) death.PlayerDeath();
            if (animationController != null) animationController.StartDeath();
            
            // (Опционально) Воскрешение:
            // currentHealth = maxHealth;
        }
    }

    public void GetHealth(int healAmount)
    {
        // Всегда проверяем актуальный Максимум (вдруг мы надели шлем на +HP?)
        maxHealth = playerStats.MaxHealth.Value;

        currentHealth += healAmount;

        // Не даем вылечиться больше максимума
        if (currentHealth > maxHealth)
        {
            currentHealth = maxHealth;
        }

        UpdateHealthBar();
    }

    // --- ИНТЕРФЕЙС СОХРАНЕНИЯ ---
    public void SaveData(SaveData data)
    {
        // Приводим float к int, так как в SaveData у нас int currentHealth
        data.currentHealth = Mathf.RoundToInt(currentHealth);
    }

    public void LoadData(SaveData data)
    {
        // 1. Применяем здоровье из файла
        currentHealth = data.currentHealth;
        
        // 2. Обновляем лимит (на случай, если статы уже подгрузились)
        if (playerStats != null) maxHealth = playerStats.MaxHealth.Value;
        
        // 3. Защита от смерти при загрузке
        if (currentHealth <= 0) currentHealth = maxHealth; 
        
        // 4. Перерисовываем UI
        UpdateHealthBar();
    }
}