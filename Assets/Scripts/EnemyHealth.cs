using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI elements

public class EnemyHealth : MonoBehaviour
{
    public float health = 50f;
    public AudioSource dyingSound;
    public RectTransform healthBar; // Reference to the UI health bar (RectTransform)
    private float maxHealth = 50f; // Enemy's maximum health
    private float maxBarWidth; // Stores the initial width of the health bar

    public StatsManager statsManager;

    void Start()
    {
        gameObject.GetComponent<Animator>().Play("Aiming");

        if (healthBar != null)
        {
            maxBarWidth = healthBar.sizeDelta.x; // Store the initial width of the health bar
            healthBar.pivot = new Vector2(0f, 0.5f); // Ensure pivot is set to the left
        }
    }

    public void TakeDamage(float amount)
    {
        health -= amount;
        UpdateHealthBar();

        if (health <= 0f)
        {
            healthBar.gameObject.SetActive(false);
            Die();
        }
    }

    void UpdateHealthBar()
    {
        if (healthBar != null)
        {
            float healthPercentage = health / maxHealth; // Calculate health percentage (0 to 1)
            healthBar.sizeDelta = new Vector2(maxBarWidth * healthPercentage, healthBar.sizeDelta.y); // Adjust width
        }
    }

    void Die()
    {
        Transform[] childTransforms = gameObject.GetComponentsInChildren<Transform>();

        foreach (Transform child in childTransforms)
        {
            if (child.CompareTag("Gun"))
            {
                Destroy(child.gameObject);
            }
        }
        gameObject.GetComponent<BoxCollider>().enabled = false;
        gameObject.GetComponent<Animator>().Play("Dying");
        dyingSound.Play();
        StartCoroutine(DestroyEnemy());
    }

    IEnumerator DestroyEnemy()
    {
        yield return new WaitForSeconds(2);
        Destroy(gameObject);
        StatsManager.enemiesKilled++;
        statsManager.enemiesKilledAmount.text = StatsManager.enemiesKilled.ToString();
    }
}
