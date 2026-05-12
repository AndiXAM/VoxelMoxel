using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "RPG/Items/Weapon")]
public class Weapon : Item
{
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
}