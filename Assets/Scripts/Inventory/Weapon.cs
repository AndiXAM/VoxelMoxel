using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "RPG/Items/Weapon")]
public class Weapon : Item
{
    [Header("Animation Speed")]
    [Tooltip("Множитель скорости анимации для этого оружия (1 = норма, 1.5 = быстрее, 0.7 = медленнее)")]
    public float animSpeedMultiplier = 1f;
    
    [Header("Weapon Settings")]
    public float damage = 10f;
    
    [Tooltip("Сколько времени длится анимация замаха (до включения хитбокса)")]
    public float attackWindup = 0.2f; // <--- НОВАЯ ПЕРЕМЕННАЯ (Замах)
    
    [Tooltip("Сколько времени хитбокс АКТИВЕН и опасен")]
    public float attackDuration = 0.3f; 

    [Tooltip("Сила ошеломления (1 = базовое замедление, 2 = сильное замедление тяжелым оружием)")]
    public float impactPower = 1f; 
    
    [Tooltip("Кулдаун до следующего удара в комбо")]
    public float attackCooldown = 0.5f;

    [Tooltip("время во время которого анимается еще проигрывается, но её можно срезать")]
    public float recoveryDelay = 0f; 
    
    [Header("Combo Settings")]
    public int comboLength = 3; 
    public float resetTime = 1f;

    [Header("Dual Wield Settings")]
    public bool isDualWeapon = false; 
    [Tooltip("Префаб для левой руки (если это двойное оружие)")]
    public GameObject offHandPrefab; 

    [HideInInspector] public GameObject leftHandChildObject; 
    [HideInInspector] public Collider weaponHitbox; 
    [System.NonSerialized] public Collider currentActiveHitbox;

    [Header("Components")]
    public AudioClip attackSound;
    public AnimatorOverrideController weaponAnimatorOverride; 

    [Header("Skill Weapon Settings")]
    [Tooltip("Если включено, это оружие-способность (Slash), копирующее статы оружия из слота снаряжения")]
    public bool isSkillWeapon = false;
}