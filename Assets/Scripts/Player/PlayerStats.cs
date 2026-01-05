using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthRegenRate = 5f; // HP per second
    [SerializeField] private float healthRegenDelay = 3f; // Seconds after damage
    
    [Header("Hunger Settings")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float hungerDepletionRate = 1f; // Per second
    [SerializeField] private float hungerDamageRate = 2f; // HP per second when hunger is 0
    [SerializeField] private float hungerRegenThreshold = 50f; // Hunger needed for health regen
    
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; // current, max
    public UnityEvent<float, float> OnHungerChanged; // current, max
    public UnityEvent OnPlayerDeath;
    
    [Header("References")]
    [SerializeField] private PlayerLook playerLook; // Reference to camera's PlayerLook
    
    private float timeSinceLastDamage = 0f;
    private bool isDead = false;
    
    // Properties
    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    
    public float Hunger => currentHunger;
    public float MaxHunger => maxHunger;
    public float HungerPercentage => currentHunger / maxHunger;
    
    public bool IsDead => isDead;
    
    void Start()
    {
        // Initialize stats
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        
        // Auto-find PlayerLook if not assigned
        if (playerLook == null)
        {
            playerLook = GetComponentInChildren<PlayerLook>();
        }
        
        // Trigger initial UI update
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
    
    void Update()
    {
        if (isDead) return;
        
        // Deplete hunger over time
        DepleteHunger(hungerDepletionRate * Time.deltaTime);
        
        // Handle hunger effects
        if (currentHunger <= 0)
        {
            // Take damage from starvation
            TakeDamage(hungerDamageRate * Time.deltaTime, true);
        }
        else if (currentHunger >= hungerRegenThreshold)
        {
            // Regenerate health if hunger is sufficient
            timeSinceLastDamage += Time.deltaTime;
            
            if (timeSinceLastDamage >= healthRegenDelay)
            {
                Heal(healthRegenRate * Time.deltaTime);
            }
        }
    }
    
    public void TakeDamage(float damage, bool bypassRegenDelay = false)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Reset regen timer unless it's from hunger
        if (!bypassRegenDelay)
        {
            timeSinceLastDamage = 0f;
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (isDead) return;
        
        currentHealth += amount;
        currentHealth = Mathf.Min(currentHealth, maxHealth);
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
    
    public void AddHunger(float amount)
    {
        if (isDead) return;
        
        currentHunger += amount;
        currentHunger = Mathf.Min(currentHunger, maxHunger);
        
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
    
    public void DepleteHunger(float amount)
    {
        if (isDead) return;
        
        currentHunger -= amount;
        currentHunger = Mathf.Max(0, currentHunger);
        
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
    
    private void Die()
    {
        isDead = true;
        OnPlayerDeath?.Invoke();
        Debug.Log("Player has died!");
        
        // Disable player controls
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        
        if (playerLook != null) playerLook.enabled = false;
        
        // Unlock cursor
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void Respawn()
    {
        isDead = false;
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        timeSinceLastDamage = 0f;
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
        
        // Re-enable player controls
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;
        
        if (playerLook != null) playerLook.enabled = true;
        
        // Lock cursor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    // Public methods for testing
    public void TestDamage() 
    { 
        TakeDamage(10f); 
    }
    
    public void TestHeal() 
    { 
        Heal(20f); 
    }
    
    public void TestEat() 
    { 
        AddHunger(30f); 
    }
}