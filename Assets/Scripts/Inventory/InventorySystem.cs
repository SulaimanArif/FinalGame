using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int width = 6;
    public int height = 5;
    
    [Header("Events")]
    public UnityEvent<ItemData, int> OnItemAdded;
    public UnityEvent<ItemData, int> OnItemRemoved;
    public UnityEvent OnInventoryChanged;
    
    private InventorySlotData[,] slots;
    
    void Awake()
    {
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        slots = new InventorySlotData[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                slots[x, y] = new InventorySlotData();
            }
        }
    }
    
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        
        int remainingAmount = amount;
        
        // If stackable, try to add to existing stacks first
        if (item.isStackable)
        {
            for (int x = 0; x < width && remainingAmount > 0; x++)
            {
                for (int y = 0; y < height && remainingAmount > 0; y++)
                {
                    if (slots[x, y].item == item && slots[x, y].amount < item.maxStackSize)
                    {
                        int spaceInStack = item.maxStackSize - slots[x, y].amount;
                        int amountToAdd = Mathf.Min(spaceInStack, remainingAmount);
                        
                        slots[x, y].amount += amountToAdd;
                        remainingAmount -= amountToAdd;
                    }
                }
            }
        }
        
        // Add to empty slots
        while (remainingAmount > 0)
        {
            Vector2Int emptySlot = FindEmptySlot();
            
            if (emptySlot.x == -1)
            {
                // Inventory full
                Debug.Log("Inventory is full!");
                OnInventoryChanged?.Invoke();
                return false;
            }
            
            int amountToAdd = item.isStackable ? Mathf.Min(remainingAmount, item.maxStackSize) : 1;
            
            slots[emptySlot.x, emptySlot.y].item = item;
            slots[emptySlot.x, emptySlot.y].amount = amountToAdd;
            remainingAmount -= amountToAdd;
        }
        
        OnItemAdded?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        
        int remainingToRemove = amount;
        
        for (int x = 0; x < width && remainingToRemove > 0; x++)
        {
            for (int y = 0; y < height && remainingToRemove > 0; y++)
            {
                if (slots[x, y].item == item)
                {
                    int amountToRemove = Mathf.Min(slots[x, y].amount, remainingToRemove);
                    slots[x, y].amount -= amountToRemove;
                    remainingToRemove -= amountToRemove;
                    
                    if (slots[x, y].amount <= 0)
                    {
                        slots[x, y].item = null;
                        slots[x, y].amount = 0;
                    }
                }
            }
        }
        
        if (remainingToRemove > 0)
        {
            Debug.Log("Not enough items to remove!");
            return false;
        }
        
        OnItemRemoved?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (slots[x, y].item == item)
                {
                    count += slots[x, y].amount;
                }
            }
        }
        
        return count;
    }
    
    public InventorySlotData GetSlot(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return slots[x, y];
    }
    
    Vector2Int FindEmptySlot()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (slots[x, y].item == null)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        
        return new Vector2Int(-1, -1); // No empty slot found
    }
    
    public bool HasSpace()
    {
        return FindEmptySlot().x != -1;
    }
}

[System.Serializable]
public class InventorySlotData
{
    public ItemData item;
    public int amount;
}