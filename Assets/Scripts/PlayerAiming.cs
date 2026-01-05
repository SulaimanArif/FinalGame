using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAiming : MonoBehaviour
{
    public GameObject gun;
    public AudioSource backgroundMusic;
    public static bool isAiming = false;

    void Start()
    {
        backgroundMusic.Play();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(1)) 
        {
            gun.GetComponent<Animator>().Play("Aim");
            StartCoroutine(StartShooting());
        }

        if (Input.GetMouseButtonUp(1)) 
        {
            gun.GetComponent<Animator>().Play("Unaim");
            isAiming = false;
        }
    }

    IEnumerator StartShooting() 
    {
        yield return new WaitForSeconds(0.5f);
        isAiming = true;
    }
}