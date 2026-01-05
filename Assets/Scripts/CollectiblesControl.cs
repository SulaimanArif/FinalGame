using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectiblesControl : MonoBehaviour
{
    public AudioSource collectSound;
    PlayerHealth playerHealth;
    public GameObject shieldIcon;
    public static bool shieldActive;
    public StatsManager statsManager;
    public AudioSource carPartCollectSound;
    public static bool isCarPartCollected = false;

    public GameObject errorMessage;
    void Start()
    {
        playerHealth = GetComponent<PlayerHealth>();
        shieldActive = false;
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "Car Part") 
        {
            errorMessage.SetActive(false);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Gold") 
        {
            collectSound.Play();
            Destroy(collision.gameObject);
            StatsManager.goldCollected++;
            statsManager.goldAmount.text = StatsManager.goldCollected.ToString();
        }

        if (collision.gameObject.tag == "Diamond")
        {
            collectSound.Play();
            Destroy(collision.gameObject);
            StatsManager.diamondsCollected++;
            statsManager.diamondsAmount.text = StatsManager.diamondsCollected.ToString();
        }

        if (collision.gameObject.tag == "Health")
        {
            collectSound.Play();
            Destroy(collision.gameObject);
            playerHealth.health = 200;
            playerHealth.UpdateHealthBar();
        }


        if (collision.gameObject.tag == "Car Part")
        {
            if (!isCarPartCollected)
            {
                carPartCollectSound.Play();
                Destroy(collision.gameObject);
                isCarPartCollected = true;
                StatsManager.carPartsCollected++;
                statsManager.carPartsAmount.text = StatsManager.carPartsCollected.ToString();
            }
            else 
            {
                errorMessage.SetActive(true);
            }

        }

        if (collision.gameObject.tag == "Shield")
        {
            collectSound.Play();
            Destroy(collision.gameObject);
            if (!shieldActive) 
            {
                shieldActive = true;
                shieldIcon.SetActive(true);
                StartCoroutine(ActivateShield());
            }
        }
    }

    IEnumerator ActivateShield() 
    {
        yield return new WaitForSeconds(10);
        shieldIcon.SetActive(false);
        shieldActive = false;
    }
}