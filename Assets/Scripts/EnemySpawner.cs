using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public GameObject enemy;
    public Transform[] enemySpawnPoints;
    bool[] isSpawned;
    float spawnTime = 3f;
    public bool isSpawning = false;
    public Vector3 spawnPostion;
    public int arraySize = 5;

    void Start()
    {
        isSpawned = new bool[arraySize];

        for (int i = 0; i < isSpawned.Length; i++)
        {
            isSpawned[i] = false;
        }
    }

    void Update()
    {
        if (!isSpawning) 
        {
            isSpawning = true;
            StartCoroutine(SpawnEnemy());
        }
    }

    IEnumerator SpawnEnemy() 
    {
        yield return new WaitForSeconds(spawnTime);

        for (int i = 0; i < isSpawned.Length; i++) 
        {
            if (isSpawned[i] == false) 
            {
                spawnPostion = enemySpawnPoints[i].position;
                isSpawned[i] = true;
                Instantiate(enemy, spawnPostion, Quaternion.identity);
                break;
            }
        }

        isSpawning = false;
    }
}