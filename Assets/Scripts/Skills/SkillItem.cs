using UnityEngine;

// Определяет, какую часть тела занимает анимация
public enum SkillBodyMask
{
    FullBody,   // Все тело (обычно персонаж останавливается или скользит вперед)
    UpperBody   // Только руки и торс (ноги свободны, персонаж может бежать)
}

public abstract class SkillItem : Item
{
    [Header("Настройки Анимации")]
    [Tooltip("Название триггера в Animator или имя анимации")]
    public AnimationClip skillAnimation; // Перетаскиваем .anim файл напрямую!
    public SkillBodyMask bodyMask = SkillBodyMask.FullBody;
    public bool canMoveWhileCasting = false;

    [Header("Тайминги (в секундах)")]
    [Tooltip("Время подготовки (замах / каст до срабатывания эффекта)")]
    public float castTime = 0.2f; 

    [Tooltip("Длительность активной фазы способности")]
    public float activeDuration = 0.5f; 

    [Tooltip("Время перезарядки до следующего использования")]
    public float cooldown = 3.0f;

    [Header("Требования к Оружию")]
    [Tooltip("Требуется ли оружие в слоте снаряжения для применения способности?")]
    public bool requiresEquippedWeapon = false;

    [Header("Аудио и Эффекты (VFX)")]
    public AudioClip castSound;
    public AudioClip impactSound;
    public AudioClip activationSound;
    public GameObject castVFXPrefab;   // Эффект при касте (например, свечение под ногами)
    public GameObject impactVFXPrefab; // Эффект при ударе (например, волна земли / взрыв)

    // --- ГЛАВНЫЙ МЕТОД СПОСОБНОСТИ ---
    // Каждая уникальная способность будет переопределять этот метод и писать свой эффект
    public abstract void ExecuteSkill(PlayerSkillHandler user, Weapon weaponFromSlot);
}