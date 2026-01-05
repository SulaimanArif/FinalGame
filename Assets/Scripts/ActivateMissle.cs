using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActivateMissle : MonoBehaviour
{
    public static bool missileActive = false;
    public static bool resetMissile = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) 
        {
            missileActive = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            resetMissile = true;
        }
    }
}