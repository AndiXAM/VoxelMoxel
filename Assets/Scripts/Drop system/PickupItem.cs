using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PickupItem : MonoBehaviour
{
    [HideInInspector] public Item itemData; 
    
    [Header("Настройки магнита")]
    public float pickupRadius = 2f;
    
    [Tooltip("Стартовая скорость, когда предмет только начинает лететь")]
    public float startSpeed = 2f; 
    
    [Tooltip("На сколько ускоряется предмет каждую секунду полета (Экспонента)")]
    public float accelerationRate = 15f; 
    
    public float destroyDistance = 0.5f;

    [HideInInspector] public bool canBePickedUp = false; 

    private Collider playerCollider; 
    private Inventory playerInventory;
    private bool isFollowing = false;
    private Rigidbody rb;

    // Текущая скорость полета (будет расти)
    private float currentSpeed;

    private void Start()
    {
        SphereCollider col = GetComponent<SphereCollider>();
        col.radius = pickupRadius;
        col.isTrigger = true;

        rb = GetComponent<Rigidbody>();
        
        // Сбрасываем скорость при старте
        currentSpeed = startSpeed;
    }

    public void EnablePickup()
    {
        canBePickedUp = true;
        CheckForPlayerInRadius();
    }

    private void Update()
    {
        if (!isFollowing || playerCollider == null || playerInventory == null) return;

        // 1. УСКОРЕНИЕ (Самая важная часть!)
        // Каждую секунду полета предмет становится всё быстрее и быстрее
        currentSpeed += accelerationRate * Time.deltaTime;

        // 2. Ищем ближайшую точку на теле игрока
        Vector3 targetPos = playerCollider.ClosestPoint(transform.position);

        // 3. Плавно летим к игроку с ТЕКУЩЕЙ (растущей) скоростью
        transform.position = Vector3.MoveTowards(transform.position, targetPos, currentSpeed * Time.deltaTime);

        // 4. Подбираем
        if (Vector3.Distance(transform.position, targetPos) <= destroyDistance)
        {
            CollectItem();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isFollowing || !canBePickedUp) return;
        TryStartFollowing(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (isFollowing || !canBePickedUp) return;
        TryStartFollowing(other);
    }

    private void TryStartFollowing(Collider potentialPlayer)
    {
        if (potentialPlayer.CompareTag("Player"))
        {
            Inventory inv = potentialPlayer.transform.root.GetComponentInChildren<Inventory>();
            
            if (inv != null && inv.GetItemCount() < inv.TotalSlots)
            {
                playerInventory = inv;
                playerCollider = potentialPlayer; 
                isFollowing = true;

                // Сбрасываем скорость на стартовую в момент начала полета
                currentSpeed = startSpeed;

                if (rb != null)
                {
                    rb.isKinematic = true;
                    rb.useGravity = false;
                }
            }
        }
    }

    private void CheckForPlayerInRadius()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, pickupRadius);
        foreach (var col in hitColliders)
        {
            if (col.CompareTag("Player")) // Защита: проверяем только игрока!
            {
                TryStartFollowing(col);
                if (isFollowing) break; 
            }
        }
    }

    private void CollectItem()
    {
        if (itemData != null && playerInventory != null)
        {
            playerInventory.AddItem(itemData, 1); // <--- ДОБАВЛЯЕМ КОЛИЧЕСТВО
            Destroy(gameObject); 
        }
    }
}