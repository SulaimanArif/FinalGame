using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    
    [Header("Recipes")]
    public CraftingRecipe[] allRecipes;
    
    [Header("Events")]
    public UnityEvent<CraftingRecipe> OnRecipeCrafted;
    public UnityEvent OnRecipesUpdated;
    
    void Start()
    {
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        // Listen for inventory changes to update recipe availability
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged.AddListener(OnInventoryChanged);
        }
    }
    
    void OnInventoryChanged()
    {
        OnRecipesUpdated?.Invoke();
    }
    
    public bool CraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || inventorySystem == null) return false;
        
        bool success = recipe.Craft(inventorySystem);
        
        if (success)
        {
            OnRecipeCrafted?.Invoke(recipe);
        }
        
        return success;
    }
    
    public bool CanCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || inventorySystem == null) return false;
        return recipe.CanCraft(inventorySystem);
    }
    
    public List<CraftingRecipe> GetAvailableRecipes()
    {
        List<CraftingRecipe> available = new List<CraftingRecipe>();
        
        foreach (CraftingRecipe recipe in allRecipes)
        {
            if (CanCraftRecipe(recipe))
            {
                available.Add(recipe);
            }
        }
        
        return available;
    }
    
    public List<CraftingRecipe> GetAllRecipes()
    {
        return new List<CraftingRecipe>(allRecipes);
    }
}