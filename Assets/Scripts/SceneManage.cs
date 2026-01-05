using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManage : MonoBehaviour
{
    public static bool gameStarted = false;
    int index = 0;

    [Header("Game Items")]
    public GameObject energyBar;
    public GameObject missionsButton;
    public GameObject gun;
    public GameObject startGun;
    public GameObject muzzleFlash;
    public GameObject enemyCount;

    [Header("Audio")]
    public AudioSource clickSound;
    public AudioSource reloadSound;

    [Header("Other")]
    public GameObject car;

    public GameObject menu;

    void Start()
    {
        gameStarted = false;

        energyBar.SetActive(false);
        enemyCount.SetActive(false);
        //missionsButton.SetActive(false);
        gun.SetActive(false);
        startGun.SetActive(false);
        muzzleFlash.SetActive(false);

        //StartGame();
        menu.SetActive(true);
    }

    public void StartGame() 
    {
        clickSound.Play();

        menu.SetActive(false);
        energyBar.SetActive(true);
        enemyCount.SetActive(true);
        //missionsButton.SetActive(true);


        StartCoroutine(HideBackground());
    }

    IEnumerator HideBackground() 
    {
        yield return new WaitForSeconds(2);
        startGun.SetActive(true);

        StartCoroutine(ShowGun());
    }

    IEnumerator ShowGun()
    {
        
        yield return new WaitForSeconds(1.5f);

        reloadSound.Play();
        startGun.SetActive(false);
        gun.SetActive(true);
        gameStarted = true;
    }

    public void RestartGame() 
    {
        SceneManager.LoadScene("Story");
    }
}
