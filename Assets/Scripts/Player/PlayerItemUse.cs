using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemUse : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    public PlayerStats playerStats;
    public HeldItemDisplay heldItemDisplay;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    private ItemData selectedItem; // Track which item is selected in inventory
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
        
        if (heldItemDisplay == null)
        {
            heldItemDisplay = GetComponentInChildren<HeldItemDisplay>();
            if (heldItemDisplay == null)
            {
                heldItemDisplay = FindObjectOfType<HeldItemDisplay>();
            }
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.UseItem.performed += OnUseItemPerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.UseItem.performed -= OnUseItemPerformed;
        inputActions.Player.Disable();
    }
    
    void OnUseItemPerformed(InputAction.CallbackContext context)
    {
        UseSelectedItem();
    }
    
    public void SetSelectedItem(ItemData item)
    {
        selectedItem = item;
        Debug.Log($"Selected item: {item?.itemName ?? "None"}");
        
        // Update held item display
        if (heldItemDisplay != null)
        {
            heldItemDisplay.ShowItem(item);
        }
    }
    
    public void UseSelectedItem()
    {
        if (selectedItem == null)
        {
            Debug.Log("No item selected!");
            return;
        }
        
        UseItem(selectedItem);
    }
    
    public void UseItem(ItemData item)
    {
        if (item == null || inventorySystem == null) return;
        
        // Check if player has the item
        int itemCount = inventorySystem.GetItemCount(item);
        if (itemCount <= 0)
        {
            Debug.Log($"You don't have any {item.itemName}!");
            return;
        }
        
        // Handle different item types
        if (item.isEdible)
        {
            EatFood(item);
        }
        else
        {
            Debug.Log($"{item.itemName} cannot be used!");
        }
    }
    
    // Replace the EatFood method with this updated version:

    void EatFood(ItemData foodItem)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found!");
            return;
        }
        
        // Check if hunger is already full
        if (playerStats.Hunger >= playerStats.MaxHunger && foodItem.healthRestoreAmount <= 0)
        {
            Debug.Log("You're not hungry!");
            return;
        }
        
        // Restore hunger
        if (foodItem.hungerRestoreAmount > 0)
        {
            playerStats.AddHunger(foodItem.hungerRestoreAmount);
            Debug.Log($"Ate {foodItem.itemName}! Restored {foodItem.hungerRestoreAmount} hunger.");
        }
        
        // Restore health (if applicable)
        if (foodItem.healthRestoreAmount > 0)
        {
            playerStats.Heal(foodItem.healthRestoreAmount);
            Debug.Log($"Healed {foodItem.healthRestoreAmount} HP!");
        }
        
        // Remove one from inventory
        bool removed = inventorySystem.RemoveItem(foodItem, 1);
        
        // FIX: Check if item is now depleted
        if (removed)
        {
            int remainingCount = inventorySystem.GetItemCount(foodItem);
            
            if (remainingCount <= 0)
            {
                // Item depleted - clear from hand
                if (heldItemDisplay != null)
                {
                    heldItemDisplay.HideItem();
                }
                selectedItem = null;
                Debug.Log($"{foodItem.itemName} depleted - removed from hand");
            }
        }
    }
}