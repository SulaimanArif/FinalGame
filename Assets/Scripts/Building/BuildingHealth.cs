using UnityEngine;

public class BuildingHealth : MonoBehaviour
{
    [Header("Building Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visual Feedback")]
    public Material damagedMaterial; 
    public bool changeColorOnDamage = true;
    public Color healthyColor = Color.white;
    public Color damagedColor = Color.red;
    
    [Header("Drop Resources")]
    public bool dropResourcesOnDestroy = true;
    public float resourceDropPercentage = 0.5f; 
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private float damageFlashTime = 0f;
    
    void Awake()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        
        if (renderers.Length > 0)
        {
            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
            }
        }
    }
    
    void Update()
    {
        if (damageFlashTime > 0)
        {
            damageFlashTime -= Time.deltaTime;
            
            if (damageFlashTime <= 0)
            {
                ResetColor();
            }
        }
        else if (changeColorOnDamage)
        {
            UpdateHealthColor();
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        FlashDamage();
        
        
        if (currentHealth <= 0)
        {
            DestroyBuilding();
        }
    }
    
    void FlashDamage()
    {
        damageFlashTime = 0.2f;
        
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.material.color = damagedColor;
            }
        }
    }
    
    void ResetColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null && originalMaterials[i] != null)
            {
                renderers[i].material = originalMaterials[i];
            }
        }
    }
    
    void UpdateHealthColor()
    {
        if (damageFlashTime > 0) return; 
        
        float healthPercent = currentHealth / maxHealth;
        Color targetColor = Color.Lerp(damagedColor, healthyColor, healthPercent);
        
        foreach (Renderer rend in renderers)
        {
            if (rend != null)
            {
                rend.material.color = targetColor;
            }
        }
    }
    
    void DestroyBuilding()
    {
        
        
        Destroy(gameObject);
    }
    
    public float GetHealthPercent()
    {
        return currentHealth / maxHealth;
    }
    
    public bool IsDead()
    {
        return currentHealth <= 0;
    }
}