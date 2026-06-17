using UnityEngine;
using System.Collections;

public class GameStartup : MonoBehaviour
{
    private IEnumerator Start()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        if (SaveManager.Instance != null && !string.IsNullOrEmpty(SaveManager.Instance.currentSaveFileName))
        {
            SaveManager.Instance.LoadGame();
            
            // --- ПРОВЕРКА НА НОВУЮ ИГРУ ---
            // Если после загрузки класс игрока ПУСТОЙ, значит это новый мир!
            if (SaveManager.Instance.currentSaveData.equippedClassName == "")
            {
                // Запускаем катсцену
                if (IntroCutsceneManager.Instance != null)
                {
                    IntroCutsceneManager.Instance.StartCutscene();
                }
            }
        }
    }
}