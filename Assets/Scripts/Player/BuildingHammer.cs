using UnityEngine;

public class BuildingHammer : MonoBehaviour
{
    [Header("References")]
    public BuildingSystem buildingSystem;
    public HeldItemDisplay heldItemDisplay;
    
    [Header("Hammer Item")]
    public ItemData hammerItem; 
    
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
        
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        if (heldItem != currentlyEquippedItem)
        {
            currentlyEquippedItem = heldItem;
            
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
        
        if (buildingSystem != null && !buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
        }
    }
    
    void UnequipHammer()
    {
        if (!isHammerEquipped) return;
        
        isHammerEquipped = false;
        
        if (buildingSystem != null && buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
        }
    }
    
    public bool IsHammerEquipped()
    {
        return isHammerEquipped;
    }
    
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