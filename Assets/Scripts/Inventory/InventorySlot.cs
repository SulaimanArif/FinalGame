using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public GameObject selectedBorder;
    
    [Header("Slot Data")]
    public int slotX;
    public int slotY;
    
    private ItemData currentItem;
    private int currentAmount;
    private Canvas canvas;
    private InventoryUI inventoryUI;
    
    // Drag data
    private GameObject draggedIcon;
    private RectTransform draggedRectTransform;
    private CanvasGroup draggedCanvasGroup;
    
    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        
        // InventoryUI is not in parent hierarchy, so find it in scene
        inventoryUI = FindObjectOfType<InventoryUI>();
        
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI not found in scene!");
        }
    }
    
    public void Setup(int x, int y)
    {
        slotX = x;
        slotY = y;
        ClearSlot();
    }
    
    public void UpdateSlot(ItemData item, int amount)
    {
        currentItem = item;
        currentAmount = amount;
        
        if (item != null && amount > 0)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            
            if (item.isStackable && amount > 1)
            {
                amountText.text = amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        currentAmount = 0;
        iconImage.enabled = false;
        amountText.enabled = false;
        
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }
    }
    
    public void SetSelected(bool selected)
    {
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(selected);
        }
    }
    
    // Pointer Click - Select item or unequip
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"=== SLOT CLICKED === Position: ({slotX}, {slotY}), Item: {currentItem?.itemName ?? "EMPTY"}");
        
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI is NULL!");
            return;
        }
        
        if (currentItem != null)
        {
            Debug.Log("Selecting item...");
            inventoryUI.SelectItem(this);
        }
        else
        {
            Debug.Log("Unequipping item...");
            inventoryUI.UnequipItem();
        }
    }
    
    // Drag Begin
    public void OnBeginDrag(PointerEventData eventData)
    {

        Debug.Log($"=== BEGIN DRAG === Item: {currentItem?.itemName ?? "EMPTY"}");
        if (currentItem == null) return;
        
        // Create dragged icon
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(canvas.transform, false);
        draggedIcon.transform.SetAsLastSibling();
        
        draggedRectTransform = draggedIcon.AddComponent<RectTransform>();
        draggedRectTransform.sizeDelta = new Vector2(60, 60);
        
        Image image = draggedIcon.AddComponent<Image>();
        image.sprite = currentItem.icon;
        image.raycastTarget = false;
        
        draggedCanvasGroup = draggedIcon.AddComponent<CanvasGroup>();
        draggedCanvasGroup.alpha = 0.6f;
        draggedCanvasGroup.blocksRaycasts = false;
        
        // Make original slot semi-transparent
        iconImage.color = new Color(1, 1, 1, 0.5f);
    }
    
    // Drag
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedRectTransform.position = eventData.position;
        }
    }
    
    // Drag End
    // Drag End
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
        }
        
        // Restore original slot alpha
        iconImage.color = Color.white;
        
        // Check if dropped on another slot
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        InventorySlot targetSlot = null;
        
        // Check if we hit a slot or a child of a slot
        if (targetObject != null)
        {
            targetSlot = targetObject.GetComponent<InventorySlot>();
            if (targetSlot == null)
            {
                // Maybe we hit a child (like the icon or amount text)
                targetSlot = targetObject.GetComponentInParent<InventorySlot>();
            }
        }
        
        if (targetSlot != null && targetSlot != this && inventoryUI != null)
        {
            inventoryUI.SwapItems(this, targetSlot);
        }
    }

    public void OnSlotClicked()
    {
        if (inventoryUI == null) return;
        
        if (currentItem != null)
        {
            // Select this item
            inventoryUI.SelectItem(this);
        }
        else
        {
            // Clicked empty slot - unequip
            inventoryUI.UnequipItem();
        }
    }
    
    public ItemData GetItem() => currentItem;
    public int GetAmount() => currentAmount;
}