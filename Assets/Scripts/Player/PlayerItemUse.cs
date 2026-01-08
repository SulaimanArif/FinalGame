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
    
    private ItemData selectedItem; 
    
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
        
        if (heldItemDisplay != null)
        {
            heldItemDisplay.ShowItem(item);
        }
    }
    
    public void UseSelectedItem()
    {
        if (selectedItem == null)
        {
            return;
        }
        
        UseItem(selectedItem);
    }
    
    public void UseItem(ItemData item)
    {
        if (item == null || inventorySystem == null) return;
        
        int itemCount = inventorySystem.GetItemCount(item);
        if (itemCount <= 0)
        {
            return;
        }
        
        if (item.isEdible)
        {
            EatFood(item);
        }
    }

    void EatFood(ItemData foodItem)
    {
        if (playerStats == null)
        {
            return;
        }
        
        if (playerStats.Hunger >= playerStats.MaxHunger && foodItem.healthRestoreAmount <= 0)
        {
            return;
        }
        
        if (foodItem.hungerRestoreAmount > 0)
        {
            playerStats.AddHunger(foodItem.hungerRestoreAmount);
        }
        
        if (foodItem.healthRestoreAmount > 0)
        {
            playerStats.Heal(foodItem.healthRestoreAmount);
        }
        
        bool removed = inventorySystem.RemoveItem(foodItem, 1);
        
        if (removed)
        {
            int remainingCount = inventorySystem.GetItemCount(foodItem);
            
            if (remainingCount <= 0)
            {
                if (heldItemDisplay != null)
                {
                    heldItemDisplay.HideItem();
                }
                selectedItem = null;
            }
        }
    }
}