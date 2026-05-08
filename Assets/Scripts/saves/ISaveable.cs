
public interface ISaveable
{
    // Метод для записи данных из игры в файл
    void SaveData(SaveData data);
    
    // Метод для загрузки данных из файла в игру
    void LoadData(SaveData data);
}