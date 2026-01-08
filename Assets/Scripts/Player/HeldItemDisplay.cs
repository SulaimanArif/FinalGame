using UnityEngine;

public class HeldItemDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform itemHoldPosition; 
    public Camera playerCamera;
    
    [Header("Settings")]
    public Vector3 holdPositionOffset = new Vector3(0.5f, -0.3f, 0.8f); 
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
        
        if (currentItemData == itemData && currentHeldItem != null)
        {
            return;
        }
        
        HideItem();
        
        currentItemData = itemData;
        
        if (itemData.worldPrefab != null)
        {
            currentHeldItem = Instantiate(itemData.worldPrefab, itemHoldPosition);
            currentHeldItem.transform.localPosition = Vector3.zero;
            currentHeldItem.transform.localRotation = Quaternion.identity;
            currentHeldItem.transform.localScale = Vector3.one * itemScale;
            
            WorldItem worldItem = currentHeldItem.GetComponent<WorldItem>();
            if (worldItem != null) Destroy(worldItem);
            
            Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider[] colliders = currentHeldItem.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null) Destroy(col);
            }
            
            SetLayerRecursively(currentHeldItem, LayerMask.NameToLayer("Ignore Raycast"));
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