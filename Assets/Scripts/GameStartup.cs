using UnityEngine;
using System.Collections;

public class GameStartup : MonoBehaviour
{
    private IEnumerator Start()
    {
        // 1. Ждем ровно 1 кадр, чтобы ВСЕ скрипты (инвентарь, статы) успели сделать свой Awake() и Start().
        // Иначе мы попытаемся загрузить данные в неинициализированные компоненты.
        yield return null; 

        // 2. Просим Менеджер Сохранений раздать данные!
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.LoadGame();
        }
        else
        {
            Debug.LogWarning("SaveManager не найден! Игра начата с нуля (режим разработчика).");
        }
    }
}