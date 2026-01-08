using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;
    public int amount = 1;
    
    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    public bool autoPickup = true;
    public float rotationSpeed = 50f;
    public float bobSpeed = 1f;
    public float bobHeight = 0.3f;
    
    [Header("Visual")]
    public GameObject visualModel;
    
    private Vector3 startPosition;
    private bool canBePickedUp = true;
    private Rigidbody rb;
    private bool hasLanded = false;
    
    void Start()
    {
        startPosition = transform.position;
        
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        rb.mass = 0.5f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        
        if (visualModel == null)
        {
            visualModel = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
        }
    }
    
    void Update()
    {
        if (!hasLanded && rb.velocity.magnitude < 0.1f)
        {
            hasLanded = true;
            startPosition = transform.position;
        }
        
        if (visualModel != null && hasLanded)
        {
            visualModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            float newY = startPosition.y + bobHeight + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp || !autoPickup) return;
        
        if (other.CompareTag("Player"))
        {
            InventorySystem inventory = other.GetComponent<InventorySystem>();
            
            if (inventory != null)
            {
                TryPickup(inventory);
            }
        }
    }
    
    public bool TryPickup(InventorySystem inventory)
    {
        if (itemData == null)
        {
            return false;
        }
        
        bool success = inventory.AddItem(itemData, amount);
        
        if (success)
        {
            Destroy(gameObject);
            return true;
        }
        else
        {
            return false;
        }
    }
    
    public void SetItemData(ItemData data, int itemAmount = 1)
    {
        itemData = data;
        amount = itemAmount;
        
        if (data.worldPrefab != null && visualModel == null)
        {
            visualModel = Instantiate(data.worldPrefab, transform);
        }
    }
}