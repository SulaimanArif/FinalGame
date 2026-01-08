using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName = "Wall";
    public GameObject prefab;
    public Sprite icon;
    
    [Header("Placement")]
    public Vector3 snapSize = new Vector3(2f, 2f, 0.2f); 
    public Vector3 pivotOffset = Vector3.zero; 
    public bool canRotate = true;
    public float rotationIncrement = 90f;
    
    [Header("Snapping")]
    public bool snapToBuildings = true; 
    public float snapDistance = 0.5f; 
    
    [Header("Material Cost")]
    public BuildingCost[] costs;
    
    [Header("Preview")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
}

[System.Serializable]
public class BuildingCost
{
    public ItemData material;
    public int amount;
}