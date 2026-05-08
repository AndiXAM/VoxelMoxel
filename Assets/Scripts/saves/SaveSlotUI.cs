using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class SaveSlotUI : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI dateText;
    public Image selectionBorder; // <--- НОВОЕ: Рамка выделения (сделай её в префабе)

    // Эти данные кнопка хранит внутри себя
    [HideInInspector] public string fileName; 
    [HideInInspector] public string sceneToLoad;

    // Ссылка на главное меню, чтобы сказать ему о клике
    private MainMenuScript mainMenu;

    // 1. МЕТОД ИНИЦИАЛИЗАЦИИ
    public void Setup(SaveData data, string fName, MainMenuScript menuRef)
    {
        fileName = fName;
        sceneToLoad = data.lastSceneName;
        nameText.text = data.saveName;
        dateText.text = data.lastPlayDate;
        mainMenu = menuRef;

        // По умолчанию слот не выбран
        SetSelected(false);

        // Вешаем слушатель на клик
        GetComponent<Button>().onClick.AddListener(OnSlotClicked);
    }

    // 2. МЕТОД КЛИКА ПО СЛОТУ
    private void OnSlotClicked()
    {
        // Не грузим сцену! Просто говорим меню: "Я выбран!"
        if (mainMenu != null)
        {
            mainMenu.SelectSaveSlot(this);
        }
    }

    // 3. МЕТОД ВЫДЕЛЕНИЯ (Включает/выключает рамку)
    public void SetSelected(bool isSelected)
    {
        if (selectionBorder != null)
        {
            selectionBorder.enabled = isSelected;
        }
    }
}