using UnityEngine;

[CreateAssetMenu(fileName = "New Enemy Data", menuName = "RPG/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public string enemyName = "ГоблинБоблин";
    // Сюда можно добавить иконку, базовое ХП, лут и т.д., 
    // но сейчас нам важен только сам факт существования этого файла как уникального ID!
}