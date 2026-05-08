using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Нужно для смены сцен

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuCanvas;
    public Button resumeButton;
    public Button mainMenuButton; // <--- НОВАЯ КНОПКА
    public Button exitButton;
    
    private Canvas canvas;

    private void Start()
    {
        pauseMenuCanvas.SetActive(false);
        canvas = pauseMenuCanvas.GetComponent<Canvas>();

        resumeButton.onClick.AddListener(ResumeGame);
        exitButton.onClick.AddListener(ExitGame);
        
        // Настраиваем новую кнопку
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            canvas.enabled = true;
            TogglePause();
        }
    }

    private void TogglePause()
    {
        bool shouldPause = !pauseMenuCanvas.activeSelf;
        pauseMenuCanvas.SetActive(shouldPause);
        Time.timeScale = shouldPause ? 0f : 1f;
    }

    private void ResumeGame()
    {
        canvas.enabled = false;
        TogglePause();
    }

    // --- ЛОГИКА ВЫХОДА С АВТОСОХРАНЕНИЕМ ---

    public void ReturnToMainMenu()
    {
        // 1. Сохраняем игру
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();

        // 2. Снимаем паузу (иначе в главном меню всё зависнет!)
        Time.timeScale = 1f;

        // 3. Загружаем сцену меню (Предполагаю, она называется "MainMenu")
        SceneManager.LoadScene("GameMenu"); 
    }

    public void ExitGame()
    {
        // 1. Сохраняем игру
        if (SaveManager.Instance != null) SaveManager.Instance.SaveGame();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}