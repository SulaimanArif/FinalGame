using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName = "New Recipe";
    public ItemData resultItem;
    public int resultAmount = 1;
    
    [Header("Required Materials")]
    public CraftingMaterial[] requiredMaterials;
    
    [Header("UI")]
    public Sprite recipeIcon;
    [TextArea(2, 4)]
    public string description = "Craft description";
    
    public bool CanCraft(InventorySystem inventory)
    {
        if (inventory == null || resultItem == null) return false;
        
        foreach (CraftingMaterial material in requiredMaterials)
        {
            int availableAmount = inventory.GetItemCount(material.item);
            
            if (availableAmount < material.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public bool Craft(InventorySystem inventory)
    {
        if (!CanCraft(inventory)) return false;
        
        if (!inventory.HasSpace() && !resultItem.isStackable)
        {
            return false;
        }
        
        foreach (CraftingMaterial material in requiredMaterials)
        {
            bool removed = inventory.RemoveItem(material.item, material.amount);
            if (!removed)
            {
                return false;
            }
        }
        
        bool added = inventory.AddItem(resultItem, resultAmount);
        
        if (!added)
        {
            foreach (CraftingMaterial material in requiredMaterials)
            {
                inventory.AddItem(material.item, material.amount);
            }
            return false;
        }
        
        return true;
    }
    
    public Sprite GetIcon()
    {
        if (recipeIcon != null) return recipeIcon;
        if (resultItem != null && resultItem.icon != null) return resultItem.icon;
        return null;
    }
}

[System.Serializable]
public class CraftingMaterial
{
    public ItemData item;
    public int amount = 1;
}