using UnityEngine;

// Этот скрипт служит "Витриной" состояний игрока.
// Враги (ИИ) будут читать эти флаги, чтобы принимать решения.
public class PlayerStateFlags : MonoBehaviour
{
    [Header("Боевые флаги")]
    [Tooltip("Игрок прямо сейчас совершает атаку (замах/удар)")]
    public bool isSwingingWeapon = false;

    // В будущем сюда можно добавить:
    // public bool isDodging = false;
    // public bool isHealing = false;
    // public bool isStunned = false;
}