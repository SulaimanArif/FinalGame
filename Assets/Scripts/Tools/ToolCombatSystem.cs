using UnityEngine;
using UnityEngine.InputSystem;

public class ToolCombatSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public HeldItemDisplay heldItemDisplay;
    public InventorySystem inventorySystem;
    public PlayerInputActions inputActions;
    
    [Header("Tool Settings")]
    public ToolData currentTool;
    public ToolInstance currentToolInstance; 
    public ItemData currentToolItem;
    public LayerMask hitMask; 
    
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
        
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        if (heldItem != null && heldItem.itemType == ItemType.Tool)
        {
            ToolInstance toolInstance = FindToolInstanceForItem(heldItem);
            
            if (toolInstance != currentToolInstance)
            {
                currentToolInstance = toolInstance;
                currentTool = toolInstance?.toolData;
                currentToolItem = heldItem;
                isToolEquipped = currentTool != null;
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
        if (item.toolData != null)
        {
            return item.toolData;
        }
        
        ToolData[] allTools = Resources.FindObjectsOfTypeAll<ToolData>();
        
        foreach (ToolData tool in allTools)
        {
            if (tool.toolName.Equals(item.itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return tool;
            }
        }
        
        return null;
    }
    
    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (!isToolEquipped || currentTool == null || currentToolInstance == null)
        {
            return;
        }
        
        if (currentToolInstance.IsBroken())
        {
            DestroyBrokenTool();
            return; 
        }
        
        float cooldown = 1f / currentTool.attackSpeed;
        if (Time.time < lastAttackTime + cooldown) return;
        
        PerformToolAttack();
        lastAttackTime = Time.time;
    }

    void PerformToolAttack()
    {
        if (currentTool == null || currentToolInstance == null)
        {
            return;
        }
        
        if (audioSource != null && currentTool.swingSound != null)
        {
            audioSource.PlayOneShot(currentTool.swingSound);
        }
        
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, currentTool.attackRange, hitMask))
        {
            float damage = currentTool.CalculateDamage(hit.collider.gameObject);
            
            Health targetHealth = hit.collider.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, transform.position, 0f);
                
                if (audioSource != null && currentTool.hitSound != null)
                {
                    audioSource.PlayOneShot(currentTool.hitSound);
                }
                
                if (showHitMarker)
                {
                    ShowHitMarker(hit.point);
                }
            }
            
            if (currentToolInstance != null)
            {
                currentToolInstance.UseTool();
                
                if (currentToolInstance != null && currentToolInstance.IsBroken())
                {
                    DestroyBrokenTool();
                    return; 
                }
            }
        }
    }

    void DestroyBrokenTool()
    {
        if (currentToolItem == null || inventorySystem == null)
        {
            currentTool = null;
            currentToolInstance = null;
            currentToolItem = null;
            isToolEquipped = false;
            return;
        }
        
        if (heldItemDisplay != null)
        {
            heldItemDisplay.HideItem();
        }
        
        ItemData itemToRemove = currentToolItem;
        currentTool = null;
        currentToolInstance = null;
        currentToolItem = null;
        isToolEquipped = false;

        inventorySystem.RemoveItem(itemToRemove, 1);
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
       
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * currentTool.attackRange);
    }
}