using System.Collections;
using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public float rotationSpeed = 5f;
    public float fireRate = 1f;
    public Transform firePoint;
    public float detectionRange = 500f;
    public float damage = 10f;
    public LayerMask playerLayer;

    public Transform player;
    private float nextTimeToFire = 0f;
    public bool playerInRange = false;

    public GameObject enemyMuzzleFlash;
    public AudioSource shootingSound;

    EnemyHealth enemyHealth;
    PlayerHealth playerHealth;

    public GameObject actualPlayer;
    private int soundPlayed = 0;

    void Start()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        playerHealth = actualPlayer.GetComponent<PlayerHealth>();
    }

    void Update()
    {
        if (playerInRange)
        {
            gameObject.GetComponent<Animator>().SetBool("isAiming", true);
            gameObject.GetComponent<Animator>().SetBool("isWalking", false);

            RotateTowardsPlayer();
            ShootAtPlayer();
            enemyMuzzleFlash.SetActive(true);
            soundPlayed++;
        }
        else
        {
            gameObject.GetComponent<Animator>().SetBool("isAiming", false);
            gameObject.GetComponent<Animator>().SetBool("isWalking", true);

            //RotateAwayFromPlayer();

            enemyMuzzleFlash.SetActive(false);
            shootingSound.Stop();
            soundPlayed = 0;
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

    void RotateTowardsPlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
    }

    void RotateAwayFromPlayer()
    {
        Vector3 direction = (transform.position - player.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));

        // Ensure smooth rotation by maintaining a continuous transition
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
                    playerHealth.TakingDamage(damage);
                    Debug.Log(playerHealth.health);
                }
            }
        }
    }
}