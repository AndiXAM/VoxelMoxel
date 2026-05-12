using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class LootDrop
{
    [Tooltip("Какой файл предмета (ScriptableObject) должен выпасть?")]
    public Item itemData; // ТЕПЕРЬ ТУТ ITEM, А НЕ PREFAB!
    
    [Range(0f, 100f)] public float dropChance = 100f;
    public int minAmount = 1;
    public int maxAmount = 1;
}

public class EnemyLoot : MonoBehaviour
{
    [Header("Таблица Лута")]
    public List<LootDrop> lootTable = new List<LootDrop>();

    [Header("Настройки разлета")]
    public float dropHeight = 1f;    
    public float scatterForce = 4f;  

    public void DropLoot()
    {
        foreach (var loot in lootTable)
        {
            if (loot.itemData == null || loot.itemData.prefab == null) 
            {
                Debug.LogWarning($"У врага {gameObject.name} в луте пустой предмет или у предмета нет 3D-префаба!");
                continue;
            }

            float roll = Random.Range(0f, 100f);

            if (roll <= loot.dropChance)
            {
                int amountToDrop = Random.Range(loot.minAmount, loot.maxAmount + 1);

                for (int i = 0; i < amountToDrop; i++)
                {
                    SpawnPhysicalItem(loot.itemData);
                }
            }
        }
    }

    private void SpawnPhysicalItem(Item item)
    {
        Vector3 spawnPos = transform.position + Vector3.up * dropHeight;
        GameObject droppedObj = Instantiate(item.prefab, spawnPos, Quaternion.identity);
        droppedObj.name = "Drop_" + item.itemName;
        droppedObj.transform.localScale = Vector3.one;

        // 1. ФИЗИКА
        Rigidbody rb = droppedObj.GetComponent<Rigidbody>();
        if (rb == null) rb = droppedObj.AddComponent<Rigidbody>();
        
        rb.mass = 1f;
        // Чуть увеличим трение, чтобы они не катились как на льду
        rb.linearDamping = 2f; 
        rb.angularDamping = 2f;

        // --- ВОТ ГЛАВНОЕ ИСПРАВЛЕНИЕ БАГА! ---
        // Заставляем Unity просчитывать траекторию полета непрерывно, чтобы не пробить пол
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; 

        // 2. ФИЗИЧЕСКИЙ КОЛЛАЙДЕР
        bool hasSolidCollider = false;
        foreach (var col in droppedObj.GetComponentsInChildren<Collider>())
        {
            if (!col.isTrigger) 
            {
                hasSolidCollider = true; 
                // Увеличил размер с 0.1 до 0.25. Это самый безопасный минимум для Unity.
                if (col is BoxCollider box) box.size = new Vector3(0.25f, 0.25f, 0.25f);
                if (col is SphereCollider sphere) sphere.radius = 0.15f;
                break;
            }
        }

        if (!hasSolidCollider)
        {
            BoxCollider box = droppedObj.AddComponent<BoxCollider>();
            // Увеличил размер
            box.size = new Vector3(0.25f, 0.25f, 0.25f); 
        }

        // 3. СКРИПТ ПОДБОРА (Магнит)
        PickupItem pickupScript = droppedObj.AddComponent<PickupItem>();
        pickupScript.itemData = item; 
        
        // НОВОЕ: Отключаем магнит на 0.5 секунд!
        pickupScript.canBePickedUp = false; 
        pickupScript.Invoke("EnablePickup", 0.5f); // Включится через полсекунды

        // 4. ВЗРЫВ ЛУТА (Разлет)
        Vector2 randomDir2D = Random.insideUnitCircle.normalized;
        Vector3 scatterDirection = new Vector3(randomDir2D.x, 1f, randomDir2D.y).normalized;

        rb.AddForce(scatterDirection * scatterForce, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * scatterForce, ForceMode.Impulse);
        
        // ОПЦИОНАЛЬНО: Если ты вернул галочки слоев (Layer Collision Matrix) в норму,
        // можешь закомментировать строку ниже. Если нет - оставь.
        droppedObj.layer = LayerMask.NameToLayer("Loot");
    }
    
}