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
        public float spawnChance = 0.5f; // Chance to spawn in valid location
        public int minGroupSize = 1;
        public int maxGroupSize = 3;
        public float groupSpreadRadius = 5f;
    }
    
    [Header("Spawn Settings")]
    public AnimalSpawnData[] animalSpawns;
    public float spawnCheckInterval = 5f; // Check every 5 seconds
    public float despawnDistance = 100f; // Remove animals this far away
    public float spawnDistance = 80f; // Spawn animals at this distance
    
    [Header("Population Limits")]
    public int maxAnimalsPerBiomeUnit = 5; // Max animals per 1000 square units of biome
    public int globalMaxAnimals = 50; // Absolute max animals in world
    
    [Header("References")]
    public Transform player;
    public TerrainGenerator terrainGenerator;
    public LayerMask groundMask;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
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
        
        // Calculate biome areas
        CalculateBiomeAreas();
        
        // Initial spawn
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
        
        // Sample the terrain to estimate biome areas
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(
            width, height, 
            terrainGenerator.scale * 2, 
            terrainGenerator.seed + 1, 
            3, 0.5f, 2f, 
            terrainGenerator.offset
        );
        
        Dictionary<BiomeData, int> biomeCells = new Dictionary<BiomeData, int>();
        
        for (int y = 0; y < height; y += 5) // Sample every 5 cells for performance
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
        
        // Convert cell counts to approximate areas
        float cellArea = 25f; // Each sampled cell represents ~5x5 = 25 square units
        foreach (var kvp in biomeCells)
        {
            biomeAreas[kvp.Key] = kvp.Value * cellArea;
            biomeAnimalCounts[kvp.Key] = 0;
            
            if (showDebugInfo)
            {
                Debug.Log($"Biome {kvp.Key.biomeName}: Area = {biomeAreas[kvp.Key]} unitsÂ²");
            }
        }
    }
    
    void SpawnAnimalsAroundPlayer()
    {
        if (player == null) return;
        
        // Remove null references
        spawnedAnimals.RemoveAll(a => a == null);
        
        // Check if we're at global limit
        if (spawnedAnimals.Count >= globalMaxAnimals)
        {
            if (showDebugInfo)
                Debug.Log("Global animal limit reached");
            return;
        }
        
        // Try to spawn animals in a ring around the player
        int spawnAttempts = 10;
        
        for (int i = 0; i < spawnAttempts; i++)
        {
            // Random position around player
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(spawnDistance * 0.7f, spawnDistance);
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x * distance, 0, randomCircle.y * distance);
            
            // Get biome at this position
            BiomeData biome = GetBiomeAtWorldPosition(spawnPos);
            if (biome == null) continue;
            
            // Check biome-specific limit
            int maxForBiome = GetMaxAnimalsForBiome(biome);
            int currentInBiome = GetAnimalCountInBiome(biome);
            
            if (currentInBiome >= maxForBiome)
            {
                if (showDebugInfo && i == 0)
                    Debug.Log($"Biome {biome.biomeName} at animal limit ({currentInBiome}/{maxForBiome})");
                continue;
            }
            
            // Try to spawn animal for this biome
            TrySpawnAnimalGroup(spawnPos, biome);
        }
    }
    
    void TrySpawnAnimalGroup(Vector3 position, BiomeData biome)
    {
        // Find valid animal spawn data for this biome
        List<AnimalSpawnData> validSpawns = new List<AnimalSpawnData>();
        
        if (showDebugInfo)
        {
            Debug.Log($"=== Trying to spawn in biome: {biome?.biomeName ?? "NULL"} ===");
        }
        
        foreach (AnimalSpawnData spawnData in animalSpawns)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Checking spawn: {spawnData.animalPrefab?.name ?? "NULL"} for biome {spawnData.biome?.biomeName ?? "NULL"}");
            }
            
            if (spawnData.biome == biome && Random.value <= spawnData.spawnChance)
            {
                validSpawns.Add(spawnData);
                if (showDebugInfo)
                {
                    Debug.Log($"âœ“ Valid spawn found: {spawnData.animalPrefab.name}");
                }
            }
        }
        
        if (validSpawns.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"âœ— No valid spawns for biome: {biome?.biomeName}");
            }
            return;
        }
        
        // Pick random animal type
        AnimalSpawnData selectedSpawn = validSpawns[Random.Range(0, validSpawns.Count)];
        
        // Spawn a group
        int groupSize = Random.Range(selectedSpawn.minGroupSize, selectedSpawn.maxGroupSize + 1);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawning group of {groupSize} {selectedSpawn.animalPrefab.name}");
        }
        
        for (int i = 0; i < groupSize; i++)
        {
            // Check global limit
            if (spawnedAnimals.Count >= globalMaxAnimals) break;
            
            // Random offset for group spread
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * selectedSpawn.groupSpreadRadius;
                offset = new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            Vector3 spawnPos = position + offset;
            
            // Raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
            {
                GameObject animal = Instantiate(selectedSpawn.animalPrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                spawnedAnimals.Add(animal);
                
                // Track biome count
                if (biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]++;
                }
                
                if (showDebugInfo && i == 0)
                {
                    Debug.Log($"âœ“ Spawned {selectedSpawn.animalPrefab.name} at {hit.point} in {biome.biomeName}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"âœ— Failed to find ground at {spawnPos}");
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
                // Decrease biome count
                Vector3 animalPos = spawnedAnimals[i].transform.position;
                BiomeData biome = GetBiomeAtWorldPosition(animalPos);
                if (biome != null && biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]--;
                }
                
                Destroy(spawnedAnimals[i]);
                spawnedAnimals.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Despawned animal (too far)");
            }
        }
    }
    
    int GetMaxAnimalsForBiome(BiomeData biome)
    {
        if (!biomeAreas.ContainsKey(biome)) return 5;
        
        float area = biomeAreas[biome];
        int max = Mathf.CeilToInt(area / 1000f * maxAnimalsPerBiomeUnit);
        return Mathf.Max(max, 3); // Minimum 3 per biome
    }
    
    int GetAnimalCountInBiome(BiomeData biome)
    {
        if (!biomeAnimalCounts.ContainsKey(biome)) return 0;
        return biomeAnimalCounts[biome];
    }
    
    BiomeData GetBiomeAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return null;
        
        // Convert world position to terrain coordinates
        float moistureValue = GetMoistureAtWorldPosition(worldPos);
        BiomeData biome = GetBiomeAtMoisture(moistureValue);
        
        if (showDebugInfo && Random.value < 0.01f) // Log occasionally to avoid spam
        {
            Debug.Log($"Position {worldPos} -> Moisture: {moistureValue:F2} -> Biome: {biome?.biomeName ?? "NULL"}");
        }
        
        return biome;
    }
    
    float GetMoistureAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return 0.5f;
        
        // Sample moisture noise at this position - MUST match TerrainGenerator's moisture calculation
        float scale = terrainGenerator.scale * 2;
        
        // Account for seed offset
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
        
        if (showDebugInfo)
        {
            Debug.LogWarning($"No biome found for moisture {moisture:F2}");
        }
        
        return terrainGenerator.biomes.Length > 0 ? terrainGenerator.biomes[0] : null;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // Draw spawn distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, spawnDistance);
        
        // Draw despawn distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
    }
}