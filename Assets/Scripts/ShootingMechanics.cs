using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShootingMechanics : MonoBehaviour
{
    public float fireRate = 0.1f; // Time between shots
    public float damage = 10f; // Damage per shot
    float range = 950f; // Range of the raycast
    public Transform firePoint; // The position where the raycast originates
    public GameObject muzzleFlash; // Reference to the muzzle flash particle system
    public AudioSource shootingSound; // Reference to the audio source for shooting sound
    //public AudioSource shootingSound2;
    //public AudioSource shootingSound3;
    public LayerMask hitLayerMask; // Layers that the raycast can hit

    private float nextTimeToFire = 0f;
    public static bool isShooting = false;

    public AudioSource hitSound;
    bool canTakeDamage = false;

    EnemyHealth enemyHealth;

    public GameObject player;
    PlayerHealth playerHealth;

    void Start()
    {
        playerHealth = player.GetComponent<PlayerHealth>();
    }

    void Update()
    {

        if (SceneManage.gameStarted) 
        {
            if (PlayerAiming.isAiming)
            {
                if (Input.GetButton("Fire1"))
                {
                    if (playerHealth.health > 0)
                    {
                        isShooting = true;
                    }

                    if (Time.time >= nextTimeToFire)
                    {
                        nextTimeToFire = Time.time + fireRate;
                        Shoot();
                    }
                }
                else
                {
                    isShooting = false;
                }
            }
            else
            {
                isShooting = false;
            }

            HandleMuzzleFlash();
            HandleShootingSound();
        }
    }

    void Shoot()
    {
        RaycastHit hit;
        if (Physics.Raycast(firePoint.position, firePoint.forward, out hit, range, hitLayerMask))
        {
            //Debug.Log(hit.transform.name);

            if (hit.transform.tag == "Enemy") 
            {
                //hit.transform.GetComponent<Animator>().Play("Hit");
                enemyHealth = hit.transform.GetComponent<EnemyHealth>();
                //hitSound.Play();
                if (enemyHealth != null)
                {
                    canTakeDamage = true;

                    if (canTakeDamage) 
                    {
                        StartCoroutine(EnemyShot());
                    }

                }
            }
        }
    }

    IEnumerator EnemyShot() 
    {
        yield return new WaitForSeconds(0.1f);
        
        enemyHealth.TakeDamage(damage);
        canTakeDamage = false;
    }

    void HandleMuzzleFlash()
    {
        if (isShooting)
        {
            muzzleFlash.SetActive(true);
        }
        else
        {
            muzzleFlash.SetActive(false);
        }
    }

    void HandleShootingSound()
    {
        if (isShooting)
        {
            if (!shootingSound.isPlaying)
            {
                shootingSound.Play();
            }
        }
        else
        {
            if (shootingSound.isPlaying)
            {
                shootingSound.Stop();
            }
        }
    }
}