using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManageCarMachine : MonoBehaviour
{
    bool cPressed = false;
    const int carCreationTime = 20;

    public GameObject pressEText;
    public GameObject successMessage;
    public GameObject createCarMessage;
    public GameObject carCreationText;
    public GameObject particleEffects;
    public AudioSource carPartInsertSound;
    public AudioSource machineSound;

    public GameObject dummyCar;
    public GameObject actualCar;

    public GameObject[] carParts;
    int index = 0;



    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "Player") 
        {

            if (CollectiblesControl.isCarPartCollected) 
            {
                pressEText.SetActive(true);
            }
            else
            {

                if (!cPressed)
                {
                    if (StatsManager.goldCollected == StatsManager.totalGold &&
                        StatsManager.diamondsCollected == StatsManager.totalDiamonds &&
                        StatsManager.enemiesKilled == StatsManager.totalEnemies &&
                        StatsManager.carPartsCollected == StatsManager.totalCarParts)
                    {
                        createCarMessage.SetActive(true);
                    }
                }



            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        pressEText.SetActive(false);
        successMessage.SetActive(false);
        createCarMessage.SetActive(false);
    }

    void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            if (CollectiblesControl.isCarPartCollected)
            {
                if (Input.GetKey(KeyCode.E))
                {
                    carPartInsertSound.Play();
                    CollectiblesControl.isCarPartCollected = false;
                    pressEText.SetActive(false);
                    successMessage.SetActive(true);
                    StartCoroutine(messageWait());
                }
            }
            else 
            {
                if (StatsManager.goldCollected == StatsManager.totalGold &&
                    StatsManager.diamondsCollected == StatsManager.totalDiamonds &&
                    StatsManager.enemiesKilled == StatsManager.totalEnemies &&
                    StatsManager.carPartsCollected == StatsManager.totalCarParts) 
                {
                    

                    if (!cPressed) 
                    {
                        if (Input.GetKey(KeyCode.C))
                        {
                            cPressed = true;
                            createCarMessage.SetActive(false);
                            carCreationText.SetActive(true);
                            particleEffects.SetActive(true);
                            machineSound.Play();
                            StartCoroutine(CreateCar());
                        }
                    }


                }
            }
        }
    }

    IEnumerator messageWait() 
    {
        yield return new WaitForSeconds(1.5f);
        successMessage.SetActive(false);
    }

    IEnumerator CreateCar()
    {
        while (index < carParts.Length)
        {
            carParts[index].SetActive(true);
            index++;
            yield return new WaitForSeconds(1);
        }

        carCreationText.SetActive(false);
        particleEffects.SetActive(false);
        machineSound.Stop();
        dummyCar.SetActive(false);
        actualCar.SetActive(true);
    }
}