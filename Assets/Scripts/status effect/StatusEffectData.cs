using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Status Effect", menuName = "RPG/Status Effect")]
public class StatusEffectData : ScriptableObject
{
    public string effectName;
    [TextArea(2, 4)] public string description;

    [Header("Visuals")]
    public Sprite icon;
    public Sprite background; 
    public Color colorTint = Color.white; 

    [Header("Settings")]
    public float duration = 10f;       
    public bool isStackable = false;   
    public int maxStacks = 1;          
    public bool refreshDurationOnNewStack = true;

    // --- НОВЫЙ БЛОК: ИГНОРИРОВАНИЕ ЗАЩИТЫ ---
    [Header("Penetration (Пробивание)")]
    [Tooltip("Наложится ли эффект, если цель увернулась (Dodge Invincible)?")]
    public bool canIgnoreDodge = false;
    [Tooltip("Наложится ли эффект, если цель идеально спарировала удар?")]
    public bool canIgnoreParry = false;
    [Tooltip("Наложится ли эффект, если цель заблокировала удар щитом?")]
    public bool canIgnoreBlock = false;

    // --- НОВЫЙ БЛОК: МОДИФИКАТОРЫ СТАТОВ ---
    [Header("Stat Modifiers")]
    [Tooltip("Бонусы или штрафы к характеристикам, пока висит этот эффект. (Используй отрицательные значения для дебаффов)")]
    public List<ClassStatBonus> statModifiers = new List<ClassStatBonus>(); // Переиспользуем структуру из ClassData!

    [Header("Effect Logic (Для ДоТов)")]
    public bool isDamageOverTime = false; 
    public int damagePerTick = 5;      
    public float tickInterval = 1f;    
}