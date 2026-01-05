using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaunchMissle : MonoBehaviour
{
    public GameObject explosion;
    float launchSpeed = 400f;
    public AudioSource blastSound;
    public GameObject missile;
    Vector3 missleStartPosition;
    int blastMissle = 0;
    bool helicopterActive = true;
    public AudioSource helicopterSound;
    public bool missileMoving = false;
    int missileSet = 0;

    void Start()
    {
        explosion.SetActive(false);
        missleStartPosition = missile.transform.position;

        if (helicopterActive) 
        {
            helicopterSound.Play();
        }
    }

    void Update()
    {
        if (ActivateMissle.missileActive)
        {
            StartCoroutine(LaunchWait());
        }

        if (missileMoving) 
        {
            missile.transform.Translate(Vector3.down * launchSpeed * Time.deltaTime);
        }

        Debug.Log(ActivateMissle.missileActive);
    }

    IEnumerator StartMissile() 
    {
        yield return new WaitForSeconds(4f);
        missileMoving = false;
        blastMissle++;
        if (blastMissle == 1) 
        {
            BlastMissle();
            blastMissle++;
        }
        
    }

    void BlastMissle() 
    {
        Debug.Log("Function Ran");
        explosion.transform.position = missile.transform.position;
        explosion.SetActive(true);
        blastSound.Play();
        missile.SetActive(false);
        ActivateMissle.missileActive = false;
        missile.transform.position = missleStartPosition;
        blastMissle = 0;
        missileSet = 0;
    }

    IEnumerator LaunchWait() 
    {
        yield return new WaitForSeconds(2.5f);
        
        if (missileSet == 0) 
        {
            missile.SetActive(true);
            missile.transform.position = missleStartPosition;
            missileSet++;
        }
        
        missileMoving = true;
        StartCoroutine(StartMissile());
    }
}