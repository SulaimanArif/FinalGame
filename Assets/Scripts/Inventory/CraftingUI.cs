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
        
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(false);
        }
        
        RefreshRecipes();
    }
    
    public void RefreshRecipes()
    {
        foreach (GameObject button in spawnedRecipeButtons)
        {
            Destroy(button);
        }
        spawnedRecipeButtons.Clear();
        
        if (craftingSystem == null) return;
        
        List<CraftingRecipe> allRecipes = craftingSystem.GetAllRecipes();
        
        foreach (CraftingRecipe recipe in allRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipesContainer);
            
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                CraftingRecipe recipeRef = recipe; 
                button.onClick.AddListener(() => SelectRecipe(recipeRef));
            }
            
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
            
            bool canCraft = craftingSystem.CanCraftRecipe(recipe);
            
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
        
        foreach (GameObject display in spawnedMaterialDisplays)
        {
            Destroy(display);
        }
        spawnedMaterialDisplays.Clear();
        
        int index = 0;
        foreach (CraftingMaterial material in recipe.requiredMaterials)
        {
            GameObject displayObj = Instantiate(materialDisplayPrefab, materialsContainer);
            
            for (int i = 0; i < displayObj.transform.childCount; i++)
            {
                Transform child = displayObj.transform.GetChild(i);
            }
            
            Transform iconTransform = displayObj.transform.Find("Image");
            
            Image matIcon = null;
            if (iconTransform != null)
            {
                matIcon = iconTransform.GetComponent<Image>();
            }
            
            Transform nameTransform = displayObj.transform.Find("ItemName");
            Transform amountTransform = displayObj.transform.Find("Amount");
            
            TextMeshProUGUI itemNameText = nameTransform?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI matAmountText = amountTransform?.GetComponent<TextMeshProUGUI>();
            
            if (matIcon != null && material.item != null && material.item.icon != null)
            {
                matIcon.sprite = material.item.icon;
                matIcon.enabled = true;
                matIcon.color = Color.white;
            }
            
            if (itemNameText != null && material.item != null)
            {
                itemNameText.text = material.item.itemName;

            }
            
            if (matAmountText != null && material.item != null)
            {
                int available = craftingSystem.inventorySystem.GetItemCount(material.item);
                matAmountText.text = $"{available}/{material.amount}";
                matAmountText.color = available >= material.amount ? Color.white : Color.red;
            }
            
            spawnedMaterialDisplays.Add(displayObj);
            index++;
        }
        
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
            RefreshRecipes();
            UpdateCraftButton();
            
            if (selectedRecipe != null)
            {
                SelectRecipe(selectedRecipe); 
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