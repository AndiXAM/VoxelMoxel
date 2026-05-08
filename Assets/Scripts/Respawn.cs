using UnityEngine;
using UnityEngine.UI;

public class Respawn : MonoBehaviour
{
    [Header("UI Ссылки")]
    public GameObject respawnCanvas; // Весь Canvas (или Panel) с экраном смерти
    public Button respawnButton;     // Кнопка "Возродиться"

    [Header("Настройки Возрождения")]
    public Transform currentSpawnPoint; // Чекпоинт (Костер), где мы появимся
    public GameObject playerCharacter;  // Ссылка на объект игрока (Капсулу)

    private void Start()
    {
        // 1. Привязываем кнопку к нашему методу (чтобы не делать это руками в Инспекторе)
        if (respawnButton != null)
        {
            respawnButton.onClick.AddListener(RespawnPlayer);
        }

        // 2. Прячем экран смерти при старте игры
        if (respawnCanvas != null)
        {
            respawnCanvas.SetActive(false);
        }
    }

    // Вызывается из HealthSystem игрока, когда ХП падает до 0
    public void ShowDeathScreen()
    {
        if (respawnCanvas != null)
        {
            respawnCanvas.SetActive(true); // Показываем "You Died"
        }

        // Опционально: Отключаем управление игроком (Character.cs), 
        // чтобы он не бегал трупом, пока мы смотрим на меню
        Character movementScript = playerCharacter.GetComponent<Character>();
        if (movementScript != null) movementScript.enabled = false;
        
        // Отключаем боевку, чтобы мертвый не махал мечом
        PlayerCombat combatScript = playerCharacter.GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = false;
    }

    // Вызывается по клику на кнопку
    private void RespawnPlayer()
    {
        // 1. Прячем экран смерти
        if (respawnCanvas != null) respawnCanvas.SetActive(false);

        // 2. ТЕЛЕПОРТАЦИЯ (Твоя правильная логика с выключением контроллера)
        CharacterController controller = playerCharacter.GetComponent<CharacterController>();
        if (controller != null)
        {
            controller.enabled = false; // Выключаем физику
            playerCharacter.transform.position = currentSpawnPoint.position;
            controller.enabled = true;  // Включаем физику обратно
        }

        // 3. ВОССТАНАВЛИВАЕМ ХП
        HealthSystem playerHealth = playerCharacter.GetComponent<HealthSystem>();
        if (playerHealth != null)
        {
            // Мы вызываем метод лечения (у тебя он называется GetHealth, я предполагаю)
            // И передаем туда гигантское число, чтобы вылечить до максимума
            playerHealth.GetHealth(999999); 
        }

        // 4. ОЧИЩАЕМ ДЕБАФФЫ (Яды, горение)
        StatusEffectManager effectManager = playerCharacter.GetComponent<StatusEffectManager>();
        if (effectManager != null)
        {
            // Удаляем все активные эффекты (чтобы не умереть от яда сразу после респавна)
            effectManager.activeEffects.Clear(); 
        }

        // 5. СБРАСЫВАЕМ АНИМАЦИИ
        Animator anim = playerCharacter.GetComponentInChildren<Animator>();
        if (anim != null)
        {
            anim.Play("Idle"); // Жестко заставляем встать в стойку
            anim.SetBool("Death", false); // Если у тебя был параметр Death, снимаем его
        }

        // 6. ВОЗВРАЩАЕМ УПРАВЛЕНИЕ
        Character movementScript = playerCharacter.GetComponent<Character>();
        if (movementScript != null) movementScript.enabled = true;
        
        PlayerCombat combatScript = playerCharacter.GetComponent<PlayerCombat>();
        if (combatScript != null) combatScript.enabled = true;

        Debug.Log("Игрок успешно возрожден на чекпоинте!");
        
        // --- ЧИСТИМ АРЕНУ ОТ СТАРЫХ ВРАГОВ (Твой старый костыль EHS.Die) ---
        // Так как мы больше не телепортируем одного и того же врага, 
        // если ты хочешь респавнить врагов при твоей смерти (как в Dark Souls),
        // тебе понадобится отдельный EnemySpawnerManager. Пока мы просто воскрешаем игрока.
    }
}