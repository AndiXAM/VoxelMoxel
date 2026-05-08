using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq; // Нужно для FindObjectsByType().OfType<>()

public class SaveManager : MonoBehaviour
{
    // Синглтон - глобальная точка доступа к сохранению
    public static SaveManager Instance { get; private set; }

    [Header("Реестр данных (База)")]
    [Tooltip("Перетащи сюда файл GameDatabase из папки Project")]
    public GameDatabase database; 

    // Внутренние данные сессии
    [HideInInspector] public string currentSaveFileName = ""; 
    [HideInInspector] public SaveData currentSaveData;        

    private string saveFolder;

    private void Awake()
    {
        // Настраиваем Синглтон (живет вечно)
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); 
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Определяем путь к папке сохранений на компьютере игрока
        saveFolder = Application.persistentDataPath + "/Saves/";
        
        // Создаем папку, если это первый запуск игры на ПК
        if (!Directory.Exists(saveFolder))
        {
            Directory.CreateDirectory(saveFolder);
        }
    }

    // --- МЕТОДЫ ДЛЯ ГЛАВНОГО МЕНЮ ---

    // Читает папку и возвращает список всех существующих сейвов (для отрисовки меню)
    public List<SaveData> GetAllSaves()
    {
        List<SaveData> allSaves = new List<SaveData>();
        
        if (!Directory.Exists(saveFolder)) return allSaves;

        string[] files = Directory.GetFiles(saveFolder, "*.json");

        foreach (string file in files)
        {
            string json = File.ReadAllText(file);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data != null) allSaves.Add(data);
        }
        return allSaves;
    }

    // Создает новый мир (вызывается кнопкой Create в меню)
    public void CreateNewSave(string customSaveName)
    {
        currentSaveData = new SaveData();
        currentSaveData.saveName = customSaveName;
        currentSaveData.lastPlayDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // Генерируем уникальное имя файла
        currentSaveFileName = "save_" + System.DateTime.Now.Ticks + ".json";

        // Сохраняем пустой прогресс на диск
        SaveGame(); 
    }

    // Выбор уже существующего слота в меню (для кнопки Load)
    public void SetCurrentSave(string fileName)
    {
        currentSaveFileName = fileName;
    }

    // --- ГЛАВНАЯ ЛОГИКА СОХРАНЕНИЯ (Сбор данных со сцены) ---
    public void SaveGame()
    {
        if (string.IsNullOrEmpty(currentSaveFileName)) return;

        // Обновляем время
        currentSaveData.lastPlayDate = System.DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        // 1. Ищем все скрипты на сцене, которые имеют интерфейс ISaveable
        // (Ищем даже на выключенных объектах, без сортировки для скорости)
        IEnumerable<ISaveable> saveables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveable>();
        
        // 2. Просим каждый скрипт записать свои данные в наш файл currentSaveData
        foreach (ISaveable saveableObj in saveables)
        {
            saveableObj.SaveData(currentSaveData);
        }

        // 3. Сериализуем (превращаем в текст) и записываем на диск
        string json = JsonUtility.ToJson(currentSaveData, true);
        File.WriteAllText(saveFolder + currentSaveFileName, json);
        
        Debug.Log($"<color=green>Игра успешно сохранена! Файл: {currentSaveFileName}</color>");
    }

    // --- ГЛАВНАЯ ЛОГИКА ЗАГРУЗКИ (Раздача данных на сцену) ---
    public void LoadGame()
    {
        if (string.IsNullOrEmpty(currentSaveFileName)) return;

        string filePath = saveFolder + currentSaveFileName;
        
        if (File.Exists(filePath))
        {
            // 1. Читаем текст из файла и превращаем обратно в класс
            string json = File.ReadAllText(filePath);
            currentSaveData = JsonUtility.FromJson<SaveData>(json);
            
            // Защита: проверяем базу данных
            if (database == null)
            {
                Debug.LogError("[SaveManager] Ошибка: GameDatabase не назначена в инспекторе! Загрузка прервана.");
                return;
            }

            // 2. Ищем все скрипты на сцене, готовые принять данные
            IEnumerable<ISaveable> saveables = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None).OfType<ISaveable>();
            
            // 3. Раздаем данные скриптам (они сами решают, что с ними делать)
            foreach (ISaveable saveableObj in saveables)
            {
                saveableObj.LoadData(currentSaveData);
            }
            
            Debug.Log($"<color=yellow>Игра успешно загружена! Мир: {currentSaveData.saveName}</color>");
        }
        else
        {
            Debug.LogWarning($"[SaveManager] Файл сохранения {currentSaveFileName} не найден на диске!");
        }
    }
}