using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    public string itemID = "item_00"; // Unique identifier
    [TextArea(3, 5)]
    public string description = "Item description";
    public Sprite icon;
    
    [Header("Stack Settings")]
    public bool isStackable = true;
    public int maxStackSize = 99;
    
    [Header("Item Type")]
    public ItemType itemType = ItemType.Resource;
    
    [Header("World Representation")]
    public GameObject worldPrefab; // The 3D model to spawn in world
}

public enum ItemType
{
    Resource,
    Food,
    Tool,
    Weapon,
    Consumable
}