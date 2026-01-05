using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public AudioSource clickSound;
    public GameObject missionsFrame;
    public GameObject openButton;
    public GameObject closeButton;

    bool showingMissions = false;
    public void OpenMissions() 
    {
        clickSound.Play();
        missionsFrame.SetActive(true);
        openButton.SetActive(false);
        //closeButton.SetActive(true);
    }

    public void CloseMissions() 
    {
        clickSound.Play();
        missionsFrame.SetActive(false);
        openButton.SetActive(true);
        //closeButton.SetActive(false);
    }

    public void MissionsButton() 
    {
        clickSound.Play();

        if (!showingMissions) 
        {
            showingMissions = true;
            missionsFrame.SetActive(true);
        }
        else if (showingMissions)
        {
            showingMissions = false;
            missionsFrame.SetActive(false);
        }
    }
}