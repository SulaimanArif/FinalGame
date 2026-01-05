using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class EnemyControl : MonoBehaviour
{
    public float rotationSpeed = 5f; // Speed at which the enemy rotates to face the player
    public float fireRate = 1f; // Time between shots
    public Transform firePoint; // The point from where the raycast originates
    float detectionRange = 500f; // The range within which the enemy can detect and shoot the player
    public float damage = 10f; // Damage dealt to the player
    public LayerMask playerLayer; // Layer mask to specify what layers the raycast can hit
    float moveSpeed = 3.5f;
    public Transform player;
    private float nextTimeToFire = 0f;
    public bool playerInRange = false;

    public GameObject enemyMuzzleFlash;
    public AudioSource shootingSound;

    EnemyHealth enemyHealth;
    PlayerHealth playerHealth;

    public GameObject actualPlayer;

    int soundPlayed = 0;

    NavMeshAgent navMeshAgent;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        playerHealth = actualPlayer.GetComponent<PlayerHealth>();
        navMeshAgent = GetComponent<NavMeshAgent>();

        // Ensure the NavMeshAgent has the correct move speed
        navMeshAgent.speed = moveSpeed;
    }

    void Update()
    {
        if (playerInRange)
        {
            RotateTowardsPlayer();
            ShootAtPlayer();
            enemyMuzzleFlash.SetActive(true);
            soundPlayed++;
            navMeshAgent.isStopped = true;  // Stop moving when in range
        }
        else if (!playerInRange)
        {
            enemyMuzzleFlash.SetActive(false);
            shootingSound.Stop();
            soundPlayed = 0;
            MoveTowardsPlayer();
        }

        if (enemyHealth.health <= 0)
        {
            enemyMuzzleFlash.SetActive(false);
            shootingSound.Stop();
            navMeshAgent.isStopped = true;
        }

        if (playerHealth.health <= 0)
        {
            playerInRange = false;
            navMeshAgent.isStopped = true;
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    void MoveTowardsPlayer()
    {
        Debug.Log("Function Called");
        navMeshAgent.isStopped = false;  // Ensure movement is enabled
        navMeshAgent.SetDestination(actualPlayer.transform.position);
    }

    void ShootAtPlayer()
    {
        if (soundPlayed == 1)
        {
            shootingSound.Play();
        }

        if (Time.time >= nextTimeToFire)
        {
            nextTimeToFire = Time.time + fireRate;

            RaycastHit hit;
            if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, detectionRange, playerLayer))
            {
                PlayerHealth playerHealth = hit.transform.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakingDamage(damage);
                    Debug.Log(playerHealth.health);
                }
            }
        }
    }
}
