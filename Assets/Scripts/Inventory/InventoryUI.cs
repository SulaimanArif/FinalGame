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
        
        // Lock/unlock cursor
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
}