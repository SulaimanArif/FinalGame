using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class PlayerHealth : MonoBehaviour
{
    public float health = 200f;
    public AudioSource dyingSound;
    public GameObject actualGun;
    public GameObject dyingGun;
    public GameObject actualCamera;
    public GameObject dyingCamera;
    public GameObject fader;

    public RectTransform healthBar; // Reference to the UI health bar (RectTransform)
    private float maxHealth = 200f; // Player's maximum health
    private float maxBarWidth; // Stores the initial width of the health bar

    public GameObject restartMenu;

    void Start()
    {
        if (healthBar != null)
        {
            maxBarWidth = healthBar.sizeDelta.x; // Store the initial width of the health bar
            healthBar.pivot = new Vector2(0f, 0.5f); // Ensure pivot is set to the left
        }
    }

    public void TakingDamage(float amount)
    {
        health -= amount;
        UpdateHealthBar();

        if (health <= 0f)
        {
            Debug.Log(health);
            Die();

            restartMenu.SetActive(true);

        }
    }

    public void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercentage = health / maxHealth; // Calculate health percentage (0 to 1)
            healthBar.sizeDelta = new Vector2(maxBarWidth * healthPercentage, healthBar.sizeDelta.y); // Adjust width
        }
    }

    void Die()
    {
        dyingSound.Play();
        gameObject.GetComponent<BoxCollider>().enabled = false;
        actualGun.SetActive(false);
        actualCamera.SetActive(false);
        dyingCamera.SetActive(true);
        dyingGun.SetActive(true);
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        yield return new WaitForSeconds(2);
        fader.SetActive(true);
    }
}
