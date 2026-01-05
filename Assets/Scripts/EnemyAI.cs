using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class EnemyAI : MonoBehaviour
{
    public float rotationSpeed = 5f; // Speed at which the enemy rotates to face the player
    public float fireRate = 1f; // Time between shots
    public Transform firePoint; // The point from where the raycast originates
    float detectionRange = 500f; // The range within which the enemy can detect and shoot the player
    public float damage = 10f; // Damage dealt to the player
    public LayerMask playerLayer; // Layer mask to specify what layers the raycast can hit

    public Transform player;
    private float nextTimeToFire = 0f;
    public bool playerInRange = false;

    public GameObject enemyMuzzleFlash;
    public AudioSource shootingSound;

    EnemyHealth enemyHealth;
    PlayerHealth playerHealth;

    public GameObject actualPlayer;

    int soundPlayed = 0;

    public GameObject playerArea;

    int setNavMesh = 0;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        playerHealth = actualPlayer.GetComponent<PlayerHealth>();
        //this.gameObject.GetComponent<NavMeshAgent>().SetDestination(playerArea.transform.position);
    }

    void Update()
    {
        if (SceneManage.gameStarted) 
        {
            if (playerInRange)
            {
                /*if (setNavMesh == 0) 
                {
                    this.gameObject.GetComponent<NavMeshAgent>().enabled = false;
                    setNavMesh++;
                }*/


                //gameObject.GetComponent<Animator>().SetBool("isAiming", true);
                //gameObject.GetComponent<Animator>().SetBool("isWalking", false);

                RotateTowardsPlayer();
                ShootAtPlayer();
                enemyMuzzleFlash.SetActive(true);
                soundPlayed++;
            }
            else if (!playerInRange)
            {
                /*if (setNavMesh != 0) 
                {
                    this.gameObject.GetComponent<NavMeshAgent>().enabled = true;
                    gameObject.GetComponent<NavMeshAgent>().SetDestination(playerArea.transform.position);
                    setNavMesh = 0;
                }*/



                //gameObject.GetComponent<Animator>().SetBool("isAiming", false);
                //gameObject.GetComponent<Animator>().SetBool("isWalking", true);

                enemyMuzzleFlash.SetActive(false);
                shootingSound.Stop();
                soundPlayed = 0;
                //gameObject.GetComponent<Animator>().Play("Walking");
            }

            if (enemyHealth.health <= 0)
            {
                enemyMuzzleFlash.SetActive(false);
                shootingSound.Stop();
            }

            if (playerHealth.health <= 0)
            {
                playerInRange = false;
                ShootingMechanics.isShooting = false;
            }
        }



        
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
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
                    if (!CollectiblesControl.shieldActive) 
                    {
                        playerHealth.TakingDamage(damage);
                    }
                    
                    Debug.Log(playerHealth.health);
                }
            }
        }
    }
}