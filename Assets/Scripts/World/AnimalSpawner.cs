using UnityEngine;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AnimalSpawnData
    {
        public GameObject animalPrefab;
        public BiomeData biome;
        [Range(0f, 1f)]
        public float spawnChance = 0.5f; 
        public int minGroupSize = 1;
        public int maxGroupSize = 3;
        public float groupSpreadRadius = 5f;
    }
    
    [Header("Spawn Settings")]
    public AnimalSpawnData[] animalSpawns;
    public float spawnCheckInterval = 5f; 
    public float despawnDistance = 100f; 
    public float spawnDistance = 80f; 
    
    [Header("Population Limits")]
    public int maxAnimalsPerBiomeUnit = 5;
    public int globalMaxAnimals = 50; 
    
    [Header("References")]
    public Transform player;
    public TerrainGenerator terrainGenerator;
    public LayerMask groundMask;
    
    private List<GameObject> spawnedAnimals = new List<GameObject>();
    private float nextSpawnCheckTime = 0f;
    private Dictionary<BiomeData, int> biomeAnimalCounts = new Dictionary<BiomeData, int>();
    private Dictionary<BiomeData, float> biomeAreas = new Dictionary<BiomeData, float>();
    
    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<TerrainGenerator>();
        }
        
        CalculateBiomeAreas();
        
        SpawnAnimalsAroundPlayer();
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnCheckTime)
        {
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
            
            CleanupDistantAnimals();
            SpawnAnimalsAroundPlayer();
        }
    }
    
    void CalculateBiomeAreas()
    {
        if (terrainGenerator == null) return;
        
        int width = terrainGenerator.mapWidth;
        int height = terrainGenerator.mapHeight;
        
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(
            width, height, 
            terrainGenerator.scale * 2, 
            terrainGenerator.seed + 1, 
            3, 0.5f, 2f, 
            terrainGenerator.offset
        );
        
        Dictionary<BiomeData, int> biomeCells = new Dictionary<BiomeData, int>();
        
        for (int y = 0; y < height; y += 5) 
        {
            for (int x = 0; x < width; x += 5)
            {
                BiomeData biome = GetBiomeAtMoisture(moistureMap[x, y]);
                if (biome != null)
                {
                    if (!biomeCells.ContainsKey(biome))
                        biomeCells[biome] = 0;
                    
                    biomeCells[biome]++;
                }
            }
        }
        
        float cellArea = 25f; 
        foreach (var kvp in biomeCells)
        {
            biomeAreas[kvp.Key] = kvp.Value * cellArea;
            biomeAnimalCounts[kvp.Key] = 0;
        }
    }
    
    void SpawnAnimalsAroundPlayer()
    {
        if (player == null) return;
        
        spawnedAnimals.RemoveAll(a => a == null);
        
        if (spawnedAnimals.Count >= globalMaxAnimals)
        {
            return;
        }
        
        int spawnAttempts = 10;
        
        for (int i = 0; i < spawnAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(spawnDistance * 0.7f, spawnDistance);
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x * distance, 0, randomCircle.y * distance);
            
            BiomeData biome = GetBiomeAtWorldPosition(spawnPos);
            if (biome == null) continue;
            
            int maxForBiome = GetMaxAnimalsForBiome(biome);
            int currentInBiome = GetAnimalCountInBiome(biome);
            
            if (currentInBiome >= maxForBiome)
            {
                continue;
            }
            
            TrySpawnAnimalGroup(spawnPos, biome);
        }
    }
    
    void TrySpawnAnimalGroup(Vector3 position, BiomeData biome)
    {
        List<AnimalSpawnData> validSpawns = new List<AnimalSpawnData>();
        
        foreach (AnimalSpawnData spawnData in animalSpawns)
        {
            
            if (spawnData.biome == biome && Random.value <= spawnData.spawnChance)
            {
                validSpawns.Add(spawnData);
            }
        }
        
        if (validSpawns.Count == 0)
        {
            return;
        }
        
        AnimalSpawnData selectedSpawn = validSpawns[Random.Range(0, validSpawns.Count)];
        
        int groupSize = Random.Range(selectedSpawn.minGroupSize, selectedSpawn.maxGroupSize + 1);
        
        for (int i = 0; i < groupSize; i++)
        {
            if (spawnedAnimals.Count >= globalMaxAnimals) break;
            
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * selectedSpawn.groupSpreadRadius;
                offset = new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            Vector3 spawnPos = position + offset;
            
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
            {
                GameObject animal = Instantiate(selectedSpawn.animalPrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                spawnedAnimals.Add(animal);
                
                if (biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]++;
                }
            }
        }
    }
    
    void CleanupDistantAnimals()
    {
        if (player == null) return;
        
        for (int i = spawnedAnimals.Count - 1; i >= 0; i--)
        {
            if (spawnedAnimals[i] == null)
            {
                spawnedAnimals.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(player.position, spawnedAnimals[i].transform.position);
            
            if (distance > despawnDistance)
            {
                Vector3 animalPos = spawnedAnimals[i].transform.position;
                BiomeData biome = GetBiomeAtWorldPosition(animalPos);
                if (biome != null && biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]--;
                }
                
                Destroy(spawnedAnimals[i]);
                spawnedAnimals.RemoveAt(i);
            }
        }
    }
    
    int GetMaxAnimalsForBiome(BiomeData biome)
    {
        if (!biomeAreas.ContainsKey(biome)) return 5;
        
        float area = biomeAreas[biome];
        int max = Mathf.CeilToInt(area / 1000f * maxAnimalsPerBiomeUnit);
        return Mathf.Max(max, 3);
    }
    
    int GetAnimalCountInBiome(BiomeData biome)
    {
        if (!biomeAnimalCounts.ContainsKey(biome)) return 0;
        return biomeAnimalCounts[biome];
    }
    
    BiomeData GetBiomeAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return null;
        
        float moistureValue = GetMoistureAtWorldPosition(worldPos);
        BiomeData biome = GetBiomeAtMoisture(moistureValue);
        
        return biome;
    }
    
    float GetMoistureAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return 0.5f;
        
        float scale = terrainGenerator.scale * 2;
        
        System.Random prng = new System.Random(terrainGenerator.seed + 1);
        float offsetX = prng.Next(-100000, 100000) + terrainGenerator.offset.x;
        float offsetY = prng.Next(-100000, 100000) + terrainGenerator.offset.y;
        
        float halfWidth = terrainGenerator.mapWidth / 2f;
        float halfHeight = terrainGenerator.mapHeight / 2f;
        
        float sampleX = (worldPos.x - halfWidth) / scale + offsetX;
        float sampleY = (worldPos.z - halfHeight) / scale + offsetY;
        
        return Mathf.PerlinNoise(sampleX, sampleY);
    }
    
    BiomeData GetBiomeAtMoisture(float moisture)
    {
        if (terrainGenerator == null || terrainGenerator.biomes == null) return null;
        
        foreach (BiomeData biome in terrainGenerator.biomes)
        {
            if (moisture >= biome.moistureMin && moisture <= biome.moistureMax)
            {
                return biome;
            }
        }
        
        return terrainGenerator.biomes.Length > 0 ? terrainGenerator.biomes[0] : null;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, spawnDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
    }
}