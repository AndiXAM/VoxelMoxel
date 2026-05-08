using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.IO; // <--- Нужно для удаления файлов (File.Delete)

public class MainMenuScript : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;      
    public GameObject savesPanel;     
    public GameObject newGamePanel;   

    [Header("Main Menu Buttons")]
    public Button playButton;
    public Button exitButton;

    [Header("Saves Menu (Список)")]
    public Transform savesListParent; 
    public GameObject saveSlotPrefab; 
    public Button backToMainButton;
    public Button openNewGamePanelButton;

    // --- НОВЫЙ БЛОК: КНОПКИ ДЕЙСТВИЙ (PLAY / DELETE) ---
    [Header("Saves Menu (Действия)")]
    public GameObject actionButtonsContainer; // Панель, где лежат кнопки Play и Delete (чтобы прятать их вместе)
    public Button loadSelectedSaveButton;     // Кнопка PLAY
    public Button deleteSelectedSaveButton;   // Кнопка DELETE

    [Header("New Game Panel")]
    public TMP_InputField saveNameInput; 
    public Button createSaveButton;
    public Button cancelNewGameButton;

    // Внутренние переменные
    private SaveSlotUI currentlySelectedSlot = null; // Какой слот сейчас выделен?

    private void Start()
    {
        playButton.onClick.AddListener(OpenSavesPanel);
        exitButton.onClick.AddListener(ExitGame);
        backToMainButton.onClick.AddListener(OpenMainPanel);
        openNewGamePanelButton.onClick.AddListener(OpenNewGamePanel);
        createSaveButton.onClick.AddListener(CreateNewGame);
        cancelNewGameButton.onClick.AddListener(OpenSavesPanel);

        // --- ПРИВЯЗКА НОВЫХ КНОПОК ---
        loadSelectedSaveButton.onClick.AddListener(LoadSelectedSave);
        deleteSelectedSaveButton.onClick.AddListener(DeleteSelectedSave);

        OpenMainPanel();
    }

    // ================= ПЕРЕКЛЮЧЕНИЕ ПАНЕЛЕЙ =================

    private void OpenMainPanel()
    {
        mainPanel.SetActive(true);
        savesPanel.SetActive(false);
        newGamePanel.SetActive(false);
    }

    private void OpenSavesPanel()
    {
        mainPanel.SetActive(false);
        savesPanel.SetActive(true);
        newGamePanel.SetActive(false);

        // При открытии меню сбрасываем выбор и прячем кнопки Play/Delete
        currentlySelectedSlot = null;
        actionButtonsContainer.SetActive(false);

        RefreshSavesList(); 
    }

    private void OpenNewGamePanel()
    {
        savesPanel.SetActive(false);
        newGamePanel.SetActive(true);
        saveNameInput.text = "Новый Мир"; 
    }

    // ================= ЛОГИКА СПИСКА СОХРАНЕНИЙ =================

    private void RefreshSavesList()
    {
        // 1. Очищаем старые кнопки
        foreach (Transform child in savesListParent) Destroy(child.gameObject);

        string saveFolder = Application.persistentDataPath + "/Saves/";
        if (!Directory.Exists(saveFolder)) return;

        string[] files = Directory.GetFiles(saveFolder, "*.json");

        // 2. Создаем новые кнопки
        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            if (data != null)
            {
                GameObject slotObj = Instantiate(saveSlotPrefab, savesListParent);
                SaveSlotUI slotUI = slotObj.GetComponent<SaveSlotUI>();
                
                // ВАЖНО: Передаем 'this' (ссылку на этот скрипт меню)
                slotUI.Setup(data, Path.GetFileName(file), this);
            }
        }
    }

    // МЕТОД: Вызывается, когда мы кликаем по любой кнопке слота
    public void SelectSaveSlot(SaveSlotUI clickedSlot)
    {
        // 1. Снимаем рамку со старого слота (если был)
        if (currentlySelectedSlot != null)
        {
            currentlySelectedSlot.SetSelected(false);
        }

        // 2. Запоминаем новый слот и рисуем ему рамку
        currentlySelectedSlot = clickedSlot;
        currentlySelectedSlot.SetSelected(true);

        // 3. Показываем кнопки Play и Delete!
        actionButtonsContainer.SetActive(true);
    }

    // ================= ЛОГИКА PLAY / DELETE =================

    private void LoadSelectedSave()
    {
        if (currentlySelectedSlot == null) return;

        // Говорим менеджеру выбрать файл и грузим сцену!
        SaveManager.Instance.SetCurrentSave(currentlySelectedSlot.fileName);
        SceneManager.LoadScene("SampleScene");
    }

    private void DeleteSelectedSave()
    {
        if (currentlySelectedSlot == null) return;

        // 1. Узнаем путь к файлу
        string filePath = Application.persistentDataPath + "/Saves/" + currentlySelectedSlot.fileName;

        // 2. Физически удаляем файл с жесткого диска
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Debug.Log($"Сохранение удалено: {currentlySelectedSlot.fileName}");
        }

        // 3. Прячем кнопки действий (так как слот удален)
        actionButtonsContainer.SetActive(false);
        currentlySelectedSlot = null;

        // 4. Перерисовываем список (удаленный слот исчезнет)
        RefreshSavesList();
    }
    // --- ИГРОВАЯ ЛОГИКА ---

    private void CreateNewGame()
    {
        string newName = saveNameInput.text;
        
        // Защита от пустого имени
        if (string.IsNullOrWhiteSpace(newName)) newName = "no name";

        // Обращаемся к Менеджеру, чтобы он создал файл
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.CreateNewSave(newName);
            
            // Загружаем сцену игры!
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            Debug.LogError("SaveManager не найден на сцене!");
        }
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}