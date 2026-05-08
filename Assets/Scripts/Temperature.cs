using UnityEngine;
using UnityEngine.UI;

public class Temperature : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float coolingRate = 2f;
    [SerializeField] private float maxTemp = 100f;
    [SerializeField] private float minTemp = -100f;

    [SerializeField] private float normalTemp = 0f;

    [Header("Визуализация")]
    [SerializeField] private Image temperatureIndicator;
    [SerializeField] private float rotationMultiplier = 1.5f;

    private float currentTemp;
    private RectTransform indicatorTransform;

    void Start()
    {
        currentTemp = normalTemp;
        indicatorTransform = temperatureIndicator.GetComponent<RectTransform>();
    }

    void Update()
    {
        
        if(currentTemp > normalTemp)
        {
            currentTemp -= coolingRate * Time.deltaTime;
            currentTemp = Mathf.Clamp(currentTemp, minTemp, maxTemp);
        }

        
        indicatorTransform.rotation = Quaternion.Euler(0, 0, -currentTemp * rotationMultiplier);
    }

    public void AddHeat(float amount,int maxAddedTemperature)
    {
        if (Mathf.Abs(currentTemp) < maxAddedTemperature)
        {
        currentTemp += amount;
        currentTemp = Mathf.Clamp(currentTemp, minTemp, maxTemp);
        }

    }
}