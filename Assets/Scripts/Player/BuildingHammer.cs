using UnityEngine;

/// <summary>
/// Building Hammer tool that activates build mode when equipped
/// Add this component to the Player GameObject
/// </summary>
public class BuildingHammer : MonoBehaviour
{
    [Header("References")]
    public BuildingSystem buildingSystem;
    public HeldItemDisplay heldItemDisplay;
    
    [Header("Hammer Item")]
    public ItemData hammerItem; // Assign your hammer ItemData
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private bool isHammerEquipped = false;
    private ItemData currentlyEquippedItem = null;
    
    void Awake()
    {
        if (buildingSystem == null)
        {
            buildingSystem = GetComponent<BuildingSystem>();
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
    
    void Start()
    {
        // Make sure build mode starts deactivated
        if (buildingSystem != null)
        {
            if (buildingSystem.isBuildModeActive)
            {
                buildingSystem.ToggleBuildMode();
            }
        }
    }
    
    void Update()
    {
        CheckEquippedItem();
    }
    
    void CheckEquippedItem()
    {
        if (heldItemDisplay == null || hammerItem == null) return;
        
        // Get currently held item
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        // Check if equipped item changed
        if (heldItem != currentlyEquippedItem)
        {
            currentlyEquippedItem = heldItem;
            
            // Check if it's the hammer
            if (heldItem == hammerItem)
            {
                EquipHammer();
            }
            else
            {
                UnequipHammer();
            }
        }
    }
    
    void EquipHammer()
    {
        if (isHammerEquipped) return;
        
        isHammerEquipped = true;
        
        // Activate build mode
        if (buildingSystem != null && !buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
            
            if (showDebugInfo)
            {
                Debug.Log("ðŸ”¨ Hammer equipped - Build mode ACTIVATED");
            }
        }
    }
    
    void UnequipHammer()
    {
        if (!isHammerEquipped) return;
        
        isHammerEquipped = false;
        
        // Deactivate build mode
        if (buildingSystem != null && buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
            
            if (showDebugInfo)
            {
                Debug.Log("ðŸ”¨ Hammer unequipped - Build mode DEACTIVATED");
            }
        }
    }
    
    // Public method to check if hammer is equipped (useful for other systems)
    public bool IsHammerEquipped()
    {
        return isHammerEquipped;
    }
    
    // Force unequip (e.g., when player dies or enters combat)
    public void ForceUnequip()
    {
        if (heldItemDisplay != null)
        {
            heldItemDisplay.HideItem();
        }
        
        currentlyEquippedItem = null;
        UnequipHammer();
    }
}