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
    
    private GameObject draggedIcon;
    private RectTransform draggedRectTransform;
    private CanvasGroup draggedCanvasGroup;
    
    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        
        inventoryUI = FindObjectOfType<InventoryUI>();
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
    
    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            return;
        }
        
        if (currentItem != null)
        {
            inventoryUI.SelectItem(this);
        }
        else
        {
            inventoryUI.UnequipItem();
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null) return;
        
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
        
        iconImage.color = new Color(1, 1, 1, 0.5f);
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedRectTransform.position = eventData.position;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
        }
        
        iconImage.color = Color.white;
        
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        InventorySlot targetSlot = null;
        
        if (targetObject != null)
        {
            targetSlot = targetObject.GetComponent<InventorySlot>();
            if (targetSlot == null)
            {
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
            inventoryUI.SelectItem(this);
        }
        else
        {
            inventoryUI.UnequipItem();
        }
    }
    
    public ItemData GetItem() => currentItem;
    public int GetAmount() => currentAmount;
}