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
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    [Header("UI Settings")]
    public float slotSize = 80f;
    public float slotSpacing = 10f;
    
    private InventorySlot[,] slotUIArray;
    private bool isInventoryOpen = false;
    
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
}