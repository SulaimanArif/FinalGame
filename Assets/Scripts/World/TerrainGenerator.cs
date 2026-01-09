using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float scale = 50f;
    public int seed = 0;
    
    [Header("Noise Settings")]
    public int octaves = 4;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;
    
    [Header("Biomes")]
    public BiomeData[] biomes;
    
    [Header("Vegetation")]
    public Transform vegetationParent;
    public LayerMask groundMask;
    [Range(0.5f, 5f)]
    public float flattenRadius = 1.5f; 
    [Range(0f, 1f)]
    public float flattenStrength = 0.8f;
    
    private GameObject terrainObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private List<Vector2> vegetationPositions = new List<Vector2>();

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        if (terrainObject != null)
            DestroyImmediate(terrainObject);
            
        if (vegetationParent != null)
        {
            foreach (Transform child in vegetationParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        vegetationPositions.Clear();
        
        float[,] heightMap = NoiseGenerator.GenerateNoiseMap(mapWidth, mapHeight, scale, seed, octaves, persistence, lacunarity, offset);
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(mapWidth, mapHeight, scale * 2, seed + 1, 3, 0.5f, 2f, offset);
        
        DetermineVegetationPositions(moistureMap);
        
        FlattenAroundVegetation(heightMap, moistureMap);
        
        CreateTerrain(heightMap, moistureMap);
        
        StartCoroutine(SpawnVegetationDelayed(moistureMap));
    }

    void DetermineVegetationPositions(float[,] moistureMap)
    {
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);
        
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                
                if (biome != null && biome.vegetationPrefabs.Length > 0)
                {
                    if (Random.value < biome.vegetationDensity)
                    {
                        float randomX = x + Random.Range(-0.5f, 0.5f);
                        float randomY = y + Random.Range(-0.5f, 0.5f);
                        vegetationPositions.Add(new Vector2(randomX, randomY));
                    }
                }
            }
        }
    }

    void FlattenAroundVegetation(float[,] heightMap, float[,] moistureMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        foreach (Vector2 vegPos in vegetationPositions)
        {
            int centerX = Mathf.RoundToInt(vegPos.x);
            int centerY = Mathf.RoundToInt(vegPos.y);
            
            if (centerX < 0 || centerX >= width || centerY < 0 || centerY >= height) continue;
            
            int moistureX = Mathf.Clamp(centerX, 0, moistureMap.GetLength(0) - 1);
            int moistureY = Mathf.Clamp(centerY, 0, moistureMap.GetLength(1) - 1);
            BiomeData biome = GetBiomeAtPosition(moistureMap[moistureX, moistureY]);
            
            float targetHeight = heightMap[centerX, centerY];
            
            int radiusInCells = Mathf.CeilToInt(flattenRadius);
            
            for (int y = centerY - radiusInCells; y <= centerY + radiusInCells; y++)
            {
                for (int x = centerX - radiusInCells; x <= centerX + radiusInCells; x++)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    
                    if (distance <= flattenRadius)
                    {
                        float influence = 1f - (distance / flattenRadius);
                        influence = Mathf.Pow(influence, 2); 
                        influence *= flattenStrength;
                        
                        heightMap[x, y] = Mathf.Lerp(heightMap[x, y], targetHeight, influence);
                    }
                }
            }
        }
    }

     void CreateTerrain(float[,] heightMap, float[,] moistureMap)
    {
        terrainObject = new GameObject("Terrain");
        terrainObject.transform.parent = transform;
        terrainObject.layer = LayerMask.NameToLayer("Ground");
        
        meshFilter = terrainObject.AddComponent<MeshFilter>();
        meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshCollider = terrainObject.AddComponent<MeshCollider>();
        
        Mesh mesh = GenerateMesh(heightMap, moistureMap);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        
        if (biomes.Length > 0 && biomes[0].terrainMaterial != null)
        {
            meshRenderer.material = biomes[0].terrainMaterial;
        }
    }

    Mesh GenerateMesh(float[,] heightMap, float[,] moistureMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uvs = new Vector2[width * height];
        int vertIndex = 0;
        int triIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                // float biomeHeight = biome != null ? heightMap[x, y] * biome.heightMultiplier + biome.heightOffset : heightMap[x, y] * 10f;

                float baseHeight = heightMap[x, y];

                float falloff = GetRadialFalloff(x, y, width, height);
                baseHeight *= falloff;

                float biomeHeight = biome != null ? baseHeight * biome.heightMultiplier + biome.heightOffset : baseHeight * 10f;
                
                vertices[vertIndex] = new Vector3(x, biomeHeight, y);
                uvs[vertIndex] = new Vector2(x / (float)width, y / (float)height);
                
                if (x < width - 1 && y < height - 1)
                {
                    triangles[triIndex] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + width;
                    triangles[triIndex + 2] = vertIndex + width + 1;
                    
                    triangles[triIndex + 3] = vertIndex;
                    triangles[triIndex + 4] = vertIndex + width + 1;
                    triangles[triIndex + 5] = vertIndex + 1;
                    
                    triIndex += 6;
                }
                
                vertIndex++;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }
    void SpawnGrass(float[,] moistureMap)
    {
        if (vegetationParent == null) return;
        
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);
        
        for (int y = 0; y < height; y += 1) 
        {
            for (int x = 0; x < width; x += 1)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                
                if (biome != null && biome.grassPrefab != null)
                {
                    if (Random.value < biome.grassDensity)
                    {
                        SpawnGrassPatch(new Vector2(x, y), biome);
                    }
                }
            }
        }
    }

    void SpawnGrassPatch(Vector2 position, BiomeData biome)
    {
        for (int i = 0; i < biome.grassPerPatch; i++)
        {
            Vector2 offset = Random.insideUnitCircle * biome.grassSpreadRadius;
            Vector3 rayStart = new Vector3(position.x + offset.x, 100f, position.y + offset.y);
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, groundMask))
            {
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                GameObject grass = Instantiate(biome.grassPrefab, hit.point, rotation, vegetationParent);
                float scale = Random.Range(0.8f, 1.2f);
                grass.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    System.Collections.IEnumerator SpawnVegetationDelayed(float[,] moistureMap)
    {
        yield return new WaitForFixedUpdate();
        
        SpawnVegetation(moistureMap);
        SpawnGrass(moistureMap);
    }

    void SpawnVegetation(float[,] moistureMap)
    {
        if (vegetationParent == null) return;
        
        int index = 0;
        foreach (Vector2 vegPos in vegetationPositions)
        {
            int x = Mathf.RoundToInt(vegPos.x);
            int y = Mathf.RoundToInt(vegPos.y);
            
            if (x < 0 || x >= moistureMap.GetLength(0) || y < 0 || y >= moistureMap.GetLength(1)) continue;
            
            BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
            
            if (biome != null && biome.vegetationPrefabs.Length > 0)
            {
                GameObject prefab = biome.vegetationPrefabs[Random.Range(0, biome.vegetationPrefabs.Length)];
                
                RaycastHit hit;
                Vector3 rayStart = new Vector3(vegPos.x, 100f, vegPos.y);
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, groundMask))
                {
                    Vector3 position = hit.point;
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    GameObject vegetation = Instantiate(prefab, position, rotation, vegetationParent);
                    float scale = Random.Range(0.8f, 1.2f);
                    vegetation.transform.localScale = new Vector3(scale, scale, scale);
                }
            }
            
            index++;
        }
    }

    BiomeData GetBiomeAtPosition(float moisture)
    {
        foreach (BiomeData biome in biomes)
        {
            if (moisture >= biome.moistureMin && moisture <= biome.moistureMax)
            {
                return biome;
            }
        }
        return biomes.Length > 0 ? biomes[0] : null;
    }

    float GetRadialFalloff(int x, int y, int width, int height)
    {
        float centerX = width / 2f;
        float centerY = height / 2f;

        float maxDistance = Mathf.Min(width, height) / 2f;
        float dist = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));

        float fallStart = maxDistance - 30f;
        float fallEnd   = maxDistance;

        float t = Mathf.InverseLerp(fallStart, fallEnd, dist);
        return Mathf.Clamp01(1f - t);
    }

}