using UnityEngine;
using System.Collections;

public class PlayerConsumables : MonoBehaviour
{
    [Header("References")]
    public Transform rightHand;
    public HealthSystem healthSystem;
    public Inventory inventory;
    public AudioSource audioSource;
    
    private Consumable currentConsumable;
    private GameObject currentConsumableObject;
    private bool isConsuming = false;
    
    // --- НОВАЯ ПЕРЕМЕННАЯ ---
    // Запоминаем, из какого слота мы пьем, чтобы удалить именно его!
    private int slotIndexBeingConsumed = -1; 

    private void Update()
    {
        if (currentConsumable != null && Input.GetMouseButtonDown(0) && !isConsuming)
        {
            // Перед стартом корутины запоминаем номер слота, который сейчас выбран в инвентаре
            if (inventory != null)
            {
                slotIndexBeingConsumed = inventory.selectedSlotIndex;
            }
            StartCoroutine(ConsumeRoutine());
        }
    }

    public void EquipConsumable(Consumable newItem)
    {
        ClearHand();
        if (newItem == null) return;

        currentConsumable = newItem;

        // Создаем предмет В МИРЕ, а потом цепляем к руке (как мы чинили гигантизм)
        if (newItem.prefab != null)
        {
            currentConsumableObject = Instantiate(newItem.prefab);
            currentConsumableObject.transform.SetParent(rightHand, true);
            currentConsumableObject.transform.localPosition = new Vector3(0,-0.0028f,0.001f);
            currentConsumableObject.transform.localRotation = Quaternion.Euler(90, 0, 0f);
        }
    }

    public void ClearHand()
    {
        if (currentConsumableObject != null)
        {
            Destroy(currentConsumableObject);
        }
        currentConsumable = null;
        isConsuming = false;
    }

    private IEnumerator ConsumeRoutine()
    {
        isConsuming = true;
        yield return new WaitForSeconds(currentConsumable.consumeDuration);

        if (healthSystem != null)
        {
            healthSystem.GetHealth((int)currentConsumable.healAmount);
        }

        if (currentConsumable.destroyOnUse)
        {
            ClearHand();
            // ИСПРАВЛЕНИЕ: ищем инвентарь, если ссылка вдруг потерялась
            if (inventory == null) inventory = transform.root.GetComponentInChildren<Inventory>();

            if (inventory != null && slotIndexBeingConsumed != -1)
            {
                inventory.RemoveItemAtSlot(slotIndexBeingConsumed, 1);
                slotIndexBeingConsumed = -1;
            }
        }
        isConsuming = false;
    }
}
