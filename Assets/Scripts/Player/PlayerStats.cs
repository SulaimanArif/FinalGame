using UnityEngine;
using UnityEngine.Events;

public class PlayerStats : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;
    [SerializeField] private float healthRegenRate = 5f; 
    [SerializeField] private float healthRegenDelay = 3f; 
    
    [Header("Hunger Settings")]
    [SerializeField] private float maxHunger = 100f;
    [SerializeField] private float currentHunger = 100f;
    [SerializeField] private float hungerDepletionRate = 1f; 
    [SerializeField] private float hungerDamageRate = 2f; 
    [SerializeField] private float hungerRegenThreshold = 50f; 
    
    [Header("Events")]
    public UnityEvent<float, float> OnHealthChanged; 
    public UnityEvent<float, float> OnHungerChanged; 
    public UnityEvent OnPlayerDeath;
    
    [Header("References")]
    [SerializeField] private PlayerLook playerLook;
    
    private float timeSinceLastDamage = 0f;
    private bool isDead = false;
    
    public float Health => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercentage => currentHealth / maxHealth;
    
    public float Hunger => currentHunger;
    public float MaxHunger => maxHunger;
    public float HungerPercentage => currentHunger / maxHunger;
    
    public bool IsDead => isDead;
    
    void Start()
    {
        currentHealth = maxHealth;
        currentHunger = maxHunger;
        
        if (playerLook == null)
        {
            playerLook = GetComponentInChildren<PlayerLook>();
        }
        
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
        OnHungerChanged?.Invoke(currentHunger, maxHunger);
    }
    
    void Update()
    {
        if (isDead) return;
        
        DepleteHunger(hungerDepletionRate * Time.deltaTime);
        
        if (currentHunger <= 0)
        {
            TakeDamage(hungerDamageRate * Time.deltaTime, true);
        }
        else if (currentHunger >= hungerRegenThreshold)
        {

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
        
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = false;
        
        if (playerLook != null) playerLook.enabled = false;
        
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
        
        PlayerController controller = GetComponent<PlayerController>();
        if (controller != null) controller.enabled = true;
        
        if (playerLook != null) playerLook.enabled = true;
        
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
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