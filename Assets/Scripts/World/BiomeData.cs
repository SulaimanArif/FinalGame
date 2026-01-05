using UnityEngine;

[CreateAssetMenu(fileName = "NewBiome", menuName = "Survival Game/Biome")]
public class BiomeData : ScriptableObject
{
    [Header("Biome Identification")]
    public string biomeName;
    
    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public float heightMultiplier = 10f;
    public float heightOffset = 0f;
    
    [Header("Vegetation")]
    public GameObject[] vegetationPrefabs;
    [Range(0f, 1f)]
    public float vegetationDensity = 0.1f;
    
    [Header("Biome Threshold")]
    [Range(0f, 1f)]
    public float moistureMin = 0f;
    [Range(0f, 1f)]
    public float moistureMax = 1f;
}