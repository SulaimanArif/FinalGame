using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

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
        
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(true);
        }
        
        // Update recipe details
        if (recipeIcon != null && recipe.GetIcon() != null)
        {
            recipeIcon.sprite = recipe.GetIcon();
            recipeIcon.enabled = true;
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
        
        // Display required materials
        foreach (CraftingMaterial material in recipe.requiredMaterials)
        {
            GameObject displayObj = Instantiate(materialDisplayPrefab, materialsContainer);
            
            Image matIcon = displayObj.transform.Find("Icon")?.GetComponent<Image>();
            if (matIcon != null && material.item.icon != null)
            {
                matIcon.sprite = material.item.icon;
                matIcon.enabled = true;
            }
            
            TextMeshProUGUI matText = displayObj.transform.Find("Amount")?.GetComponent<TextMeshProUGUI>();
            if (matText != null)
            {
                int available = craftingSystem.inventorySystem.GetItemCount(material.item);
                matText.text = $"{available}/{material.amount}";
                
                // Color red if not enough
                matText.color = available >= material.amount ? Color.white : Color.red;
            }
            
            spawnedMaterialDisplays.Add(displayObj);
        }
        
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