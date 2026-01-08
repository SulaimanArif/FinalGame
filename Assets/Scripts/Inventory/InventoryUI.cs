using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public GameObject slotPrefab;
    
    [Header("Tabs")]
    public GameObject inventoryTab;
    public GameObject craftingTab;
    public Button inventoryTabButton;
    public Button craftingTabButton;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    [Header("UI Settings")]
    public float slotSize = 80f;
    public float slotSpacing = 10f;

    [Header("Item Details")]
    public GameObject itemDetailPanel;
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailStatsText;

    private InventorySlot selectedSlot;
    
    private InventorySlot[,] slotUIArray;
    private bool isInventoryOpen = false;
    private bool isInventoryTabActive = true;
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Inventory.performed += OnInventoryToggle;
    }
    
    void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnInventoryToggle;
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        CreateInventorySlots();
        inventoryPanel.SetActive(false);
        
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged.AddListener(RefreshInventoryUI);
        }
        
        // Setup tab buttons
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.AddListener(() => SwitchTab(true));
            Debug.Log("Inventory tab button listener added");
        }
        else
        {
            Debug.LogError("Inventory Tab Button is not assigned!");
        }
        
        if (craftingTabButton != null)
        {
            craftingTabButton.onClick.AddListener(() => SwitchTab(false));
            Debug.Log("Crafting tab button listener added");
        }
        else
        {
            Debug.LogError("Crafting Tab Button is not assigned!");
        }
        
        // Default to inventory tab
        SwitchTab(true);
    }
    
    void CreateInventorySlots()
    {
        slotUIArray = new InventorySlot[inventorySystem.width, inventorySystem.height];
        
        for (int y = 0; y < inventorySystem.height; y++)
        {
            for (int x = 0; x < inventorySystem.width; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsParent);
                RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
                
                // Position slot
                float posX = x * (slotSize + slotSpacing);
                float posY = -y * (slotSize + slotSpacing);
                rectTransform.anchoredPosition = new Vector2(posX, posY);
                
                // Setup slot
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                slot.Setup(x, y);
                
                // Wire up button if it exists
                Button button = slotObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(slot.OnSlotClicked);
                }
                
                slotUIArray[x, y] = slot;
            }
        }
    }
    
    void RefreshInventoryUI()
    {
        for (int x = 0; x < inventorySystem.width; x++)
        {
            for (int y = 0; y < inventorySystem.height; y++)
            {
                InventorySlotData slotData = inventorySystem.GetSlot(x, y);
                slotUIArray[x, y].UpdateSlot(slotData.item, slotData.amount);
            }
        }
    }
    
    void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }
    
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);
        
        // Lock/unlock cursor using CursorManager
        if (isInventoryOpen)
        {
            // Inventory opened - unlock cursor
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.UnlockCursor();
            }
            else
            {
                // Fallback if CursorManager doesn't exist
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
        else
        {
            // Inventory closed - lock cursor
            if (CursorManager.Instance != null)
            {
                CursorManager.Instance.LockCursor();
            }
            else
            {
                // Fallback if CursorManager doesn't exist
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
    
    void SwitchTab(bool showInventory)
    {
        Debug.Log($"Switching tab to: {(showInventory ? "Inventory" : "Crafting")}");
        
        isInventoryTabActive = showInventory;
        
        if (inventoryTab != null)
        {
            inventoryTab.SetActive(showInventory);
            Debug.Log($"Inventory tab set to: {showInventory}");
        }
        else
        {
            Debug.LogError("Inventory Tab GameObject is not assigned!");
        }
        
        if (craftingTab != null)
        {
            craftingTab.SetActive(!showInventory);
            Debug.Log($"Crafting tab set to: {!showInventory}");
        }
        else
        {
            Debug.LogError("Crafting Tab GameObject is not assigned!");
        }
        
        // Update button colors
        UpdateTabButtonColors();
    }
    
    void UpdateTabButtonColors()
    {
        if (inventoryTabButton != null)
        {
            var colors = inventoryTabButton.colors;
            colors.normalColor = isInventoryTabActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            inventoryTabButton.colors = colors;
        }
        
        if (craftingTabButton != null)
        {
            var colors = craftingTabButton.colors;
            colors.normalColor = !isInventoryTabActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            craftingTabButton.colors = colors;
        }
    }


    public void SelectItem(InventorySlot slot)
    {

        Debug.Log($"=== SELECT ITEM CALLED ===");
        Debug.Log($"Slot position: ({slot.slotX}, {slot.slotY})");
        Debug.Log($"Slot item: {slot.GetItem()?.itemName ?? "NULL"}");
        Debug.Log($"Slot amount: {slot.GetAmount()}");
        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        
        // Select new slot
        selectedSlot = slot;
        selectedSlot.SetSelected(true);
        
        // Get the actual item from the slot
        ItemData itemToEquip = slot.GetItem();
        
        // Update held item display
        PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
        if (itemUse != null)
        {
            itemUse.SetSelectedItem(itemToEquip);
        }
        
        // Show item details
        ShowItemDetails(itemToEquip);
        
        Debug.Log($"Selected item: {(itemToEquip != null ? itemToEquip.itemName : "NULL")}");
    }

    public void UnequipItem()
    {
        // Deselect current slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        
        // Clear held item
        PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
        if (itemUse != null)
        {
            itemUse.SetSelectedItem(null);
        }
        
        // Hide item details
        HideItemDetails();
        
        Debug.Log("Unequipped item");
    }

    public void SwapItems(InventorySlot slotA, InventorySlot slotB)
    {
        if (inventorySystem == null) return;
        
        // Get slot data
        InventorySlotData dataA = inventorySystem.GetSlot(slotA.slotX, slotA.slotY);
        InventorySlotData dataB = inventorySystem.GetSlot(slotB.slotX, slotB.slotY);
        
        if (dataA == null || dataB == null) return;
        
        // Swap the data
        ItemData tempItem = dataA.item;
        int tempAmount = dataA.amount;
        
        dataA.item = dataB.item;
        dataA.amount = dataB.amount;
        
        dataB.item = tempItem;
        dataB.amount = tempAmount;
        
        // IMPORTANT: Trigger inventory changed event
        inventorySystem.OnInventoryChanged?.Invoke();
        
        // Refresh UI
        RefreshInventoryUI();
        
        // If we swapped with the selected slot, update held item
        if (selectedSlot == slotA || selectedSlot == slotB)
        {
            PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
            if (itemUse != null && selectedSlot != null)
            {
                itemUse.SetSelectedItem(selectedSlot.GetItem());
            }
        }
        
        Debug.Log($"Swapped items between slot ({slotA.slotX},{slotA.slotY}) and ({slotB.slotX},{slotB.slotY})");
    }

    void ShowItemDetails(ItemData item)
    {
        if (itemDetailPanel == null || item == null)
        {
            HideItemDetails();
            return;
        }
        
        itemDetailPanel.SetActive(true);
        
        // Icon
        if (detailIcon != null)
        {
            detailIcon.sprite = item.icon;
            detailIcon.enabled = item.icon != null;
        }
        
        // Name
        if (detailNameText != null)
        {
            detailNameText.text = item.itemName;
        }
        
        // Description
        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = item.description;
        }
        
        // Type
        if (detailTypeText != null)
        {
            detailTypeText.text = $"Type: {item.itemType}";
        }
        
        // Stats (food stats, etc)
        if (detailStatsText != null)
        {
            string stats = "";
            
            if (item.isEdible)
            {
                stats += $"<color=#90EE90>Hunger: +{item.hungerRestoreAmount}</color>\n";
                if (item.healthRestoreAmount > 0)
                {
                    stats += $"<color=#FF6B6B>Health: +{item.healthRestoreAmount}</color>\n";
                }
            }
            
            if (item.isStackable)
            {
                stats += $"Max Stack: {item.maxStackSize}";
            }
            
            detailStatsText.text = stats;
        }
    }

    void HideItemDetails()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }
    }
    
    public bool IsInventoryOpen()
    {
        return isInventoryOpen;
    }
}