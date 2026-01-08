using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CraftingUI : MonoBehaviour
{
    [Header("References")]
    public CraftingSystem craftingSystem;
    public GameObject craftingPanel;
    public Transform recipesContainer;
    public GameObject recipeButtonPrefab;
    
    [Header("Recipe Details Panel")]
    public GameObject recipeDetailsPanel;
    public Image recipeIcon;
    public TextMeshProUGUI recipeNameText;
    public TextMeshProUGUI recipeDescriptionText;
    public Transform materialsContainer;
    public GameObject materialDisplayPrefab;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;
    
    private List<GameObject> spawnedRecipeButtons = new List<GameObject>();
    private List<GameObject> spawnedMaterialDisplays = new List<GameObject>();
    private CraftingRecipe selectedRecipe;
    
    void Start()
    {
        if (craftingSystem == null)
        {
            craftingSystem = FindObjectOfType<CraftingSystem>();
        }
        
        if (craftingSystem != null)
        {
            craftingSystem.OnRecipesUpdated.AddListener(RefreshRecipes);
        }
        
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }
        
        // Hide details panel initially
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(false);
        }
        
        RefreshRecipes();
    }
    
    public void RefreshRecipes()
    {
        // Clear existing buttons
        foreach (GameObject button in spawnedRecipeButtons)
        {
            Destroy(button);
        }
        spawnedRecipeButtons.Clear();
        
        if (craftingSystem == null) return;
        
        // Create button for each recipe
        List<CraftingRecipe> allRecipes = craftingSystem.GetAllRecipes();
        
        foreach (CraftingRecipe recipe in allRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipesContainer);
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                CraftingRecipe recipeRef = recipe; // Capture for lambda
                button.onClick.AddListener(() => SelectRecipe(recipeRef));
            }
            
            // Setup visuals
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && recipe.GetIcon() != null)
            {
                iconImage.sprite = recipe.GetIcon();
                iconImage.enabled = true;
            }
            
            TextMeshProUGUI nameText = buttonObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = recipe.recipeName;
            }
            
            // Check if can craft
            bool canCraft = craftingSystem.CanCraftRecipe(recipe);
            
            // Dim if can't craft
            CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = canCraft ? 1f : 0.5f;
            
            spawnedRecipeButtons.Add(buttonObj);
        }
    }
    
    void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        
        Debug.Log($"=== SELECT RECIPE: {recipe.recipeName} ===");
        
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(true);
        }
        
        // Update recipe details
        if (recipeIcon != null && recipe.GetIcon() != null)
        {
            recipeIcon.sprite = recipe.GetIcon();
            recipeIcon.enabled = true;
            recipeIcon.color = Color.white;
        }
        
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;
        }
        
        if (recipeDescriptionText != null)
        {
            recipeDescriptionText.text = recipe.description;
        }
        
        // Clear previous materials display
        foreach (GameObject display in spawnedMaterialDisplays)
        {
            Destroy(display);
        }
        spawnedMaterialDisplays.Clear();
        
        Debug.Log($"Recipe has {recipe.requiredMaterials.Length} materials");
        
        // Display required materials
        int index = 0;
        foreach (CraftingMaterial material in recipe.requiredMaterials)
        {
            Debug.Log($"--- Material {index}: {material.item?.itemName ?? "NULL"} ---");
            
            GameObject displayObj = Instantiate(materialDisplayPrefab, materialsContainer);
            Debug.Log($"Instantiated display object: {displayObj.name}");
            
            // Print all children
            Debug.Log($"Display has {displayObj.transform.childCount} children:");
            for (int i = 0; i < displayObj.transform.childCount; i++)
            {
                Transform child = displayObj.transform.GetChild(i);
                Debug.Log($"  Child {i}: {child.name} - Components: {string.Join(", ", child.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
            
            // Find icon
            Transform iconTransform = displayObj.transform.Find("Image");
            Debug.Log($"Icon transform found: {iconTransform != null}");
            
            Image matIcon = null;
            if (iconTransform != null)
            {
                matIcon = iconTransform.GetComponent<Image>();
                Debug.Log($"Icon Image component found: {matIcon != null}");
            }
            
            // Find texts
            Transform nameTransform = displayObj.transform.Find("ItemName");
            Transform amountTransform = displayObj.transform.Find("Amount");
            Debug.Log($"ItemName found: {nameTransform != null}, Amount found: {amountTransform != null}");
            
            TextMeshProUGUI itemNameText = nameTransform?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI matAmountText = amountTransform?.GetComponent<TextMeshProUGUI>();
            
            // Set icon
            if (matIcon != null)
            {
                if (material.item != null)
                {
                    if (material.item.icon != null)
                    {
                        matIcon.sprite = material.item.icon;
                        matIcon.enabled = true;
                        matIcon.color = Color.white;
                        Debug.Log($"âœ“ Icon SET for {material.item.itemName}: {material.item.icon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"âœ— Material {material.item.itemName} has NO ICON!");
                    }
                }
                else
                {
                    Debug.LogWarning("âœ— Material.item is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("âœ— matIcon component is NULL!");
            }
            
            // Set item name
            if (itemNameText != null && material.item != null)
            {
                itemNameText.text = material.item.itemName;
                Debug.Log($"Set item name: {material.item.itemName}");
            }
            
            // Set amount
            if (matAmountText != null && material.item != null)
            {
                int available = craftingSystem.inventorySystem.GetItemCount(material.item);
                matAmountText.text = $"{available}/{material.amount}";
                matAmountText.color = available >= material.amount ? Color.white : Color.red;
                Debug.Log($"Set amount: {available}/{material.amount}");
            }
            
            spawnedMaterialDisplays.Add(displayObj);
            index++;
        }
        
        Debug.Log("=== RECIPE SELECTION COMPLETE ===");
        
        // Update craft button
        UpdateCraftButton();
    }
    
    void UpdateCraftButton()
    {
        if (craftButton == null || selectedRecipe == null) return;
        
        bool canCraft = craftingSystem.CanCraftRecipe(selectedRecipe);
        craftButton.interactable = canCraft;
        
        if (craftButtonText != null)
        {
            craftButtonText.text = canCraft ? "CRAFT" : "INSUFFICIENT MATERIALS";
        }
    }
    
    void OnCraftButtonClicked()
    {
        if (selectedRecipe == null) return;
        
        bool success = craftingSystem.CraftRecipe(selectedRecipe);
        
        if (success)
        {
            // Refresh UI
            RefreshRecipes();
            UpdateCraftButton();
            
            // Update materials display
            if (selectedRecipe != null)
            {
                SelectRecipe(selectedRecipe); // Refresh details panel
            }
        }
    }
    
    void OnDestroy()
    {
        if (craftingSystem != null)
        {
            craftingSystem.OnRecipesUpdated.RemoveListener(RefreshRecipes);
        }
    }
}