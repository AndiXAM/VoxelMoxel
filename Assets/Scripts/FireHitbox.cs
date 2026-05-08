using UnityEngine;

public class FireHitbox : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private float heatingRate = 10f;
    [SerializeField] private float maxDistance = 3f;

    [Header("Ссылки")]
    [SerializeField] private Temperature temperatureSystem; 

    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            // Рассчитываем интенсивность нагрева в зависимости от расстояния
            float distance = Vector3.Distance(transform.position, other.transform.position);
            float heatIntensity = Mathf.Clamp01(1 - distance/maxDistance);
            
            temperatureSystem.AddHeat(heatingRate * heatIntensity * Time.deltaTime, 30);
        }
    }
}