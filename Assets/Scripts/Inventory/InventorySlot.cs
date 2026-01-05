using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
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
    
    public void OnSlotClicked()
    {
        if (currentItem != null)
        {
            Debug.Log($"Clicked: {currentItem.itemName} x{currentAmount}");
            // Future: Implement item interaction, moving, dropping, etc.
        }
    }
    
    public ItemData GetItem() => currentItem;
    public int GetAmount() => currentAmount;
}