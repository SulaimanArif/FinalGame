using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        public BiomeData biome;
        [Range(0f, 1f)]
        public float spawnChance = 0.5f;
        public int minGroupSize = 1;
        public int maxGroupSize = 2;
        public float groupSpreadRadius = 3f;
    }
    
    [Header("Spawn Settings")]
    public EnemySpawnData[] enemySpawns;
    public float spawnCheckInterval = 10f; 
    public float despawnDistance = 120f;
    public float minSpawnDistance = 50f;
    public float maxSpawnDistance = 100f; 
    
    [Header("Night Spawning")]
    public DayNightCycle dayNightCycle;
    public bool onlySpawnAtNight = true;
    [Range(0f, 1f)]
    public float nightStartTime = 0.75f; 
    [Range(0f, 1f)]
    public float nightEndTime = 0.25f; 
    
    [Header("Population Limits")]
    public int maxEnemiesPerBiomeUnit = 3;
    public int globalMaxEnemies = 30;
    
    [Header("References")]
    public Transform player;
    public TerrainGenerator terrainGenerator;
    public LayerMask groundMask;
    public GameProgressionSystem progressionSystem; 
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private float nextSpawnCheckTime = 0f;
    private Dictionary<BiomeData, int> biomeEnemyCounts = new Dictionary<BiomeData, int>();
    private Dictionary<BiomeData, float> biomeAreas = new Dictionary<BiomeData, float>();
    private bool wasNight = false;
    
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
        
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<DayNightCycle>();
        }
        
        if (progressionSystem == null)
        {
            progressionSystem = FindObjectOfType<GameProgressionSystem>();
        }
        
        CalculateBiomeAreas();
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnCheckTime)
        {
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
            
            CleanupDistantEnemies();
            
            if (ShouldSpawn())
            {
                SpawnEnemiesAroundPlayer();
            }
        }
        
        bool isNight = IsNightTime();
        wasNight = isNight;
    }
    
    bool ShouldSpawn()
    {
        if (onlySpawnAtNight && !IsNightTime())
        {
            return false;
        }
        
        return true;
    }
    
    bool IsNightTime()
    {
        if (dayNightCycle == null) return true; 
        
        float timeOfDay = dayNightCycle.GetTimeOfDay();
        
        if (nightStartTime > nightEndTime)
        {
            return timeOfDay >= nightStartTime || timeOfDay <= nightEndTime;
        }
        else
        {
            return timeOfDay >= nightStartTime && timeOfDay <= nightEndTime;
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
            biomeEnemyCounts[kvp.Key] = 0;
        }
    }
    
    void SpawnEnemiesAroundPlayer()
    {
        if (player == null) return;
        
        spawnedEnemies.RemoveAll(e => e == null);
        
        if (spawnedEnemies.Count >= globalMaxEnemies)
        {
            return;
        }
        
        int spawnAttempts = 10;
        
        for (int i = 0; i < spawnAttempts; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x * distance, 0, randomCircle.y * distance);
            
            float distToPlayer = Vector3.Distance(spawnPos, player.position);
            if (distToPlayer < minSpawnDistance)
            {
                continue;
            }
            
            BiomeData biome = GetBiomeAtWorldPosition(spawnPos);
            if (biome == null) continue;
            
            int maxForBiome = GetMaxEnemiesForBiome(biome);
            int currentInBiome = GetEnemyCountInBiome(biome);
            
            if (currentInBiome >= maxForBiome)
            {
                continue;
            }
            
            TrySpawnEnemyGroup(spawnPos, biome);
        }
    }
    
    void TrySpawnEnemyGroup(Vector3 position, BiomeData biome)
    {
        List<EnemySpawnData> validSpawns = new List<EnemySpawnData>();
        
        foreach (EnemySpawnData spawnData in enemySpawns)
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
        
        EnemySpawnData selectedSpawn = validSpawns[Random.Range(0, validSpawns.Count)];
        int groupSize = Random.Range(selectedSpawn.minGroupSize, selectedSpawn.maxGroupSize + 1);
        
        for (int i = 0; i < groupSize; i++)
        {
            if (spawnedEnemies.Count >= globalMaxEnemies) break;
            
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * selectedSpawn.groupSpreadRadius;
                offset = new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            Vector3 spawnPos = position + offset;
            
            if (Vector3.Distance(spawnPos, player.position) < minSpawnDistance)
            {
                continue;
            }
            
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
            {
                GameObject enemy = Instantiate(selectedSpawn.enemyPrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                
                if (progressionSystem != null)
                {
                    progressionSystem.ApplyScalingToEnemy(enemy);
                }
                
                if (!enemy.CompareTag("Enemy"))
                {
                    enemy.tag = "Enemy";
                }
                
                spawnedEnemies.Add(enemy);
                
                if (biomeEnemyCounts.ContainsKey(biome))
                {
                    biomeEnemyCounts[biome]++;
                }
            }
        }
    }
    
    void CleanupDistantEnemies()
    {
        if (player == null) return;
        
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(player.position, spawnedEnemies[i].transform.position);
            
            if (distance > despawnDistance)
            {
                Vector3 enemyPos = spawnedEnemies[i].transform.position;
                BiomeData biome = GetBiomeAtWorldPosition(enemyPos);
                if (biome != null && biomeEnemyCounts.ContainsKey(biome))
                {
                    biomeEnemyCounts[biome]--;
                }
                
                Destroy(spawnedEnemies[i]);
                spawnedEnemies.RemoveAt(i);
            }
        }
    }
    
    int GetMaxEnemiesForBiome(BiomeData biome)
    {
        if (!biomeAreas.ContainsKey(biome)) return 3;
        
        float area = biomeAreas[biome];
        int max = Mathf.CeilToInt(area / 1000f * maxEnemiesPerBiomeUnit);
        return Mathf.Max(max, 2);
    }
    
    int GetEnemyCountInBiome(BiomeData biome)
    {
        if (!biomeEnemyCounts.ContainsKey(biome)) return 0;
        return biomeEnemyCounts[biome];
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
    
    public void DespawnAllEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        
        spawnedEnemies.Clear();
        
        foreach (var key in biomeEnemyCounts.Keys)
        {
            biomeEnemyCounts[key] = 0;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
    }
}