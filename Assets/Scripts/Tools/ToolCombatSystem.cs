using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles tool-based combat and resource gathering
/// Add to Player GameObject
/// </summary>
public class ToolCombatSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public HeldItemDisplay heldItemDisplay;
    public InventorySystem inventorySystem;
    public PlayerInputActions inputActions;
    
    [Header("Tool Settings")]
    public ToolData currentTool;
    public ToolInstance currentToolInstance; // Track current tool instance
    public ItemData currentToolItem; // Track current item
    public LayerMask hitMask; // What can be hit
    
    [Header("Visual Feedback")]
    public bool showHitMarker = true;
    public float hitMarkerDuration = 0.2f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    
    private float lastAttackTime;
    private bool isToolEquipped = false;
    
    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (heldItemDisplay == null)
        {
            heldItemDisplay = GetComponentInChildren<HeldItemDisplay>();
        }
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Disable();
    }
    
    void Update()
    {
        CheckEquippedTool();
    }
    
    void CheckEquippedTool()
    {
        if (heldItemDisplay == null) return;
        
        // Get currently held item
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        // Check if it's a tool
        if (heldItem != null && heldItem.itemType == ItemType.Tool)
        {
            // Find the tool instance in inventory
            ToolInstance toolInstance = FindToolInstanceForItem(heldItem);
            
            if (toolInstance != currentToolInstance)
            {
                currentToolInstance = toolInstance;
                currentTool = toolInstance?.toolData;
                currentToolItem = heldItem;
                isToolEquipped = currentTool != null;
                
                if (isToolEquipped)
                {
                    Debug.Log($"Equipped tool: {currentTool.toolName} ({currentToolInstance.currentDurability}/{currentTool.maxDurability})");
                }
            }
        }
        else
        {
            currentTool = null;
            currentToolInstance = null;
            currentToolItem = null;
            isToolEquipped = false;
        }
    }
    
    ToolInstance FindToolInstanceForItem(ItemData item)
    {
        if (inventorySystem == null) return null;
        
        // Search inventory for this specific item and get its tool instance
        for (int x = 0; x < inventorySystem.width; x++)
        {
            for (int y = 0; y < inventorySystem.height; y++)
            {
                InventorySlotData slotData = inventorySystem.GetSlot(x, y);
                if (slotData != null && slotData.item == item && slotData.toolInstance != null)
                {
                    return slotData.toolInstance;
                }
            }
        }
        
        return null;
    }
    
    ToolData FindToolDataForItem(ItemData item)
    {
        // Direct reference first
        if (item.toolData != null)
        {
            return item.toolData;
        }
        
        // Load all ToolData assets
        ToolData[] allTools = Resources.FindObjectsOfTypeAll<ToolData>();
        
        foreach (ToolData tool in allTools)
        {
            // Match by name
            if (tool.toolName.Equals(item.itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return tool;
            }
        }
        
        return null;
    }
    
    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // Add null checks at the start
        if (!isToolEquipped || currentTool == null || currentToolInstance == null)
        {
            return;
        }
        
        // Check if tool is broken
        if (currentToolInstance.IsBroken())
        {
            Debug.Log($"{currentTool.toolName} is broken!");
            DestroyBrokenTool();
            return; // IMPORTANT: Return after destroying
        }
        
        // Check attack cooldown
        float cooldown = 1f / currentTool.attackSpeed;
        if (Time.time < lastAttackTime + cooldown) return;
        
        PerformToolAttack();
        lastAttackTime = Time.time;
    }

    void PerformToolAttack()
    {
        // Add safety check at start
        if (currentTool == null || currentToolInstance == null)
        {
            Debug.LogWarning("PerformToolAttack called but tool is null!");
            return;
        }
        
        // Play swing sound
        if (audioSource != null && currentTool.swingSound != null)
        {
            audioSource.PlayOneShot(currentTool.swingSound);
        }
        
        // Raycast from camera center
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, currentTool.attackRange, hitMask))
        {
            // Calculate damage based on what we hit
            float damage = currentTool.CalculateDamage(hit.collider.gameObject);
            
            // Try to damage the object
            Health targetHealth = hit.collider.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, transform.position, 0f);
                
                // Play hit sound
                if (audioSource != null && currentTool.hitSound != null)
                {
                    audioSource.PlayOneShot(currentTool.hitSound);
                }
                
                // Show hit marker
                if (showHitMarker)
                {
                    ShowHitMarker(hit.point);
                }
                
                Debug.Log($"Hit {hit.collider.name} for {damage} damage!");
            }
            
            // Reduce tool durability - with null check
            if (currentToolInstance != null)
            {
                currentToolInstance.UseTool();
                
                // Check if tool broke - with null check
                if (currentToolInstance != null && currentToolInstance.IsBroken())
                {
                    Debug.Log($"{currentTool.toolName} broke!");
                    DestroyBrokenTool();
                    return; // IMPORTANT: Return immediately after destroying
                }
            }
        }
        
        // Visual feedback
        Debug.DrawRay(ray.origin, ray.direction * currentTool.attackRange, Color.red, 0.5f);
    }

    void DestroyBrokenTool()
    {
        if (currentToolItem == null || inventorySystem == null)
        {
            Debug.LogWarning("DestroyBrokenTool called but references are null");
            // Still clear local references
            currentTool = null;
            currentToolInstance = null;
            currentToolItem = null;
            isToolEquipped = false;
            return;
        }
        
        Debug.Log($"Destroying broken {currentToolItem.itemName}");
        
        // Clear from hand FIRST (before removing from inventory)
        if (heldItemDisplay != null)
        {
            heldItemDisplay.HideItem();
        }
        
        // Clear references BEFORE removing from inventory
        ItemData itemToRemove = currentToolItem;
        currentTool = null;
        currentToolInstance = null;
        currentToolItem = null;
        isToolEquipped = false;
        
        // Remove from inventory LAST
        inventorySystem.RemoveItem(itemToRemove, 1);
        
        Debug.Log("Tool destroyed successfully");
    }
    
    void ShowHitMarker(Vector3 worldPosition)
    {
        // Simple hit marker - you can enhance this with particles or UI
        Debug.Log($"HIT at {worldPosition}");
        
        // TODO: Instantiate hit effect particle
        // GameObject hitEffect = Instantiate(hitEffectPrefab, worldPosition, Quaternion.identity);
        // Destroy(hitEffect, hitMarkerDuration);
    }
    
    public ToolData GetCurrentTool()
    {
        return currentTool;
    }
    
    public ToolInstance GetCurrentToolInstance()
    {
        return currentToolInstance;
    }
    
    public bool IsToolEquipped()
    {
        return isToolEquipped;
    }
    
    void OnDrawGizmosSelected()
    {
        if (currentTool == null || playerCamera == null) return;
        
        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * currentTool.attackRange);
    }
}