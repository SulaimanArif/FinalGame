using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;
    
    [Header("Health UI")]
    [SerializeField] private Image healthBarFill;
    [SerializeField] private TextMeshProUGUI healthText;
    
    [Header("Hunger UI")]
    [SerializeField] private Image hungerBarFill;
    [SerializeField] private TextMeshProUGUI hungerText;
    
    [Header("Death Screen")]
    [SerializeField] private GameObject deathScreen;
    [SerializeField] private TextMeshProUGUI deathText;
    
    [Header("Settings")]
    [SerializeField] private bool showNumbers = true;
    [SerializeField] private Color healthColor = Color.red;
    [SerializeField] private Color hungerColor = new Color(1f, 0.5f, 0f); // Orange
    
    void Start()
    {
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }
        
        if (playerStats != null)
        {
            // Subscribe to stat changes
            playerStats.OnHealthChanged.AddListener(UpdateHealthUI);
            playerStats.OnHungerChanged.AddListener(UpdateHungerUI);
            playerStats.OnPlayerDeath.AddListener(ShowDeathScreen);
        }
        
        // Apply colors
        if (healthBarFill != null) healthBarFill.color = healthColor;
        if (hungerBarFill != null) hungerBarFill.color = hungerColor;
        
        // Hide death screen initially
        if (deathScreen != null) deathScreen.SetActive(false);
        
        // Initial update
        UpdateHealthUI(playerStats.Health, playerStats.MaxHealth);
        UpdateHungerUI(playerStats.Hunger, playerStats.MaxHunger);
    }
    
    void OnDestroy()
    {
        if (playerStats != null)
        {
            playerStats.OnHealthChanged.RemoveListener(UpdateHealthUI);
            playerStats.OnHungerChanged.RemoveListener(UpdateHungerUI);
            playerStats.OnPlayerDeath.RemoveListener(ShowDeathScreen);
        }
    }
    
    private void UpdateHealthUI(float current, float max)
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = current / max;
        }
        
        if (healthText != null && showNumbers)
        {
            healthText.text = $"{Mathf.Ceil(current)}/{max}";
        }
    }
    
    private void UpdateHungerUI(float current, float max)
    {
        if (hungerBarFill != null)
        {
            hungerBarFill.fillAmount = current / max;
        }
        
        if (hungerText != null && showNumbers)
        {
            hungerText.text = $"{Mathf.Ceil(current)}/{max}";
        }
    }
    
    private void ShowDeathScreen()
    {
        if (deathScreen != null)
        {
            deathScreen.SetActive(true);
        }
    }
    
    public void OnRespawnButton()
    {
        if (playerStats != null)
        {
            playerStats.Respawn();
        }
        
        if (deathScreen != null)
        {
            deathScreen.SetActive(false);
        }
    }
}