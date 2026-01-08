using UnityEngine;

public class HeldItemDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform itemHoldPosition; // Position in front of camera
    public Camera playerCamera;
    
    [Header("Settings")]
    public Vector3 holdPositionOffset = new Vector3(0.5f, -0.3f, 0.8f); // Right, Down, Forward
    public Vector3 holdRotationOffset = new Vector3(0f, -90f, 0f);
    public float itemScale = 0.5f;
    
    [Header("Animation")]
    public bool enableBobbing = true;
    public float bobSpeed = 2f;
    public float bobAmount = 0.02f;
    
    private GameObject currentHeldItem;
    private ItemData currentItemData;
    private Vector3 originalPosition;
    
    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Create hold position if it doesn't exist
        if (itemHoldPosition == null)
        {
            GameObject holdPosObj = new GameObject("ItemHoldPosition");
            holdPosObj.transform.parent = playerCamera.transform;
            holdPosObj.transform.localPosition = holdPositionOffset;
            holdPosObj.transform.localRotation = Quaternion.Euler(holdRotationOffset);
            itemHoldPosition = holdPosObj.transform;
        }
        
        originalPosition = itemHoldPosition.localPosition;
    }
    
    void Update()
    {
        if (currentHeldItem != null && enableBobbing)
        {
            // Bobbing animation
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            itemHoldPosition.localPosition = originalPosition + new Vector3(0, bobOffset, 0);
        }
    }
    
    public void ShowItem(ItemData itemData)
    {
        if (itemData == null)
        {
            HideItem();
            return;
        }
        
        // If same item, don't recreate
        if (currentItemData == itemData && currentHeldItem != null)
        {
            return;
        }
        
        // Clear previous item
        HideItem();
        
        currentItemData = itemData;
        
        // Spawn the item's world prefab
        if (itemData.worldPrefab != null)
        {
            currentHeldItem = Instantiate(itemData.worldPrefab, itemHoldPosition);
            currentHeldItem.transform.localPosition = Vector3.zero;
            currentHeldItem.transform.localRotation = Quaternion.identity;
            currentHeldItem.transform.localScale = Vector3.one * itemScale;
            
            // Remove WorldItem script FIRST (it has RequireComponent dependencies)
            WorldItem worldItem = currentHeldItem.GetComponent<WorldItem>();
            if (worldItem != null) Destroy(worldItem);
            
            // Now we can safely remove physics components
            Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider[] colliders = currentHeldItem.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null) Destroy(col);
            }
            
            // Set layer to avoid raycast issues
            SetLayerRecursively(currentHeldItem, LayerMask.NameToLayer("Ignore Raycast"));
            
            Debug.Log($"Now holding: {itemData.itemName}");
        }
        else
        {
            Debug.LogWarning($"Item {itemData.itemName} has no world prefab to display!");
        }
    }
    
    public void HideItem()
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }
        
        currentItemData = null;
    }
    
    public ItemData GetCurrentItem()
    {
        return currentItemData;
    }
    
    public bool IsHoldingItem()
    {
        return currentHeldItem != null;
    }
    
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}