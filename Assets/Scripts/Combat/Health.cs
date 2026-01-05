using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Health : MonoBehaviour
{
    [Header("Health Settings")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;
    
    [Header("Knockback Settings")]
    public bool canBeKnockedBack = true;
    public float knockbackResistance = 1f; // Multiplier for incoming knockback
    
    [Header("Visual Feedback")]
    public bool flashOnDamage = true;
    public Color damageColor = Color.red;
    public float flashDuration = 0.2f;
    
    [Header("Item Drops")]
    public ItemDrop[] itemDrops;
    public float dropHeight = 1f; // Height above ground to spawn items
    
    [Header("Events")]
    public UnityEvent<float> OnDamaged; // Passes damage amount
    public UnityEvent OnDeath;
    
    private bool isDead = false;
    private Rigidbody rb;
    private CharacterController characterController;
    private AIBehavior aiBehavior;
    private MaterialPropertyBlock mpb;
    private Color[] originalColors;

    
    // Knockback for CharacterController
    private Vector3 knockbackVelocity;
    private float knockbackDecay = 5f;
    
    // Visual feedback
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashCoroutine;
    
    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        aiBehavior = GetComponent<AIBehavior>();
        
        // Get all renderers (including children)
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original materials
        mpb = new MaterialPropertyBlock();
        originalColors = new Color[renderers.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].GetPropertyBlock(mpb);
            originalColors[i] = renderers[i].sharedMaterial.color;
        }

    }
    
    void Update()
    {
        // Apply knockback for CharacterController
        if (characterController != null && knockbackVelocity.magnitude > 0.1f)
        {
            characterController.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }
    }
    
    public void TakeDamage(float damage, Vector3 damageSource, float knockbackForce = 0f)
    {
        if (isDead) return;
        
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        OnDamaged?.Invoke(damage);
        
        // Visual feedback
        if (flashOnDamage)
        {
            FlashDamage();
        }
        
        // Alert AI that it was attacked
        if (aiBehavior != null)
        {
            aiBehavior.OnAttacked(damageSource);
        }
        
        // Apply knockback
        if (canBeKnockedBack && knockbackForce > 0f)
        {
            Vector3 knockbackDirection = (transform.position - damageSource).normalized;
            knockbackDirection.y = 0.3f; // Slight upward force
            
            float finalKnockback = knockbackForce * knockbackResistance;
            
            if (rb != null)
            {
                // Rigidbody knockback
                rb.AddForce(knockbackDirection * finalKnockback, ForceMode.Impulse);
            }
            else if (characterController != null)
            {
                // CharacterController knockback (stored velocity)
                knockbackVelocity = knockbackDirection * finalKnockback;
            }
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void FlashDamage()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }
        flashCoroutine = StartCoroutine(FlashRoutine());
    }
    
    IEnumerator FlashRoutine()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            mpb.SetColor("_BaseColor", damageColor);
            renderers[i].SetPropertyBlock(mpb);
        }

        yield return new WaitForSeconds(flashDuration);

        for (int i = 0; i < renderers.Length; i++)
        {
            mpb.SetColor("_BaseColor", originalColors[i]);
            renderers[i].SetPropertyBlock(mpb);
        }
    }


    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        OnDeath?.Invoke();
        
        // Drop items
        DropItems();
        
        // Destroy this GameObject
        Destroy(gameObject, 0.1f);
    }

    Vector3 GetGroundPosition(Vector3 origin)
    {
        RaycastHit hit;

        // Start ray slightly above to avoid hitting own collider
        Vector3 rayStart = origin + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.05f; // small offset to avoid clipping
        }

        // Fallback if no ground found
        return origin;
    }

    
    void DropItems()
    {
        foreach (ItemDrop drop in itemDrops)
        {
            if (drop.itemData == null) continue;
            
            // Random chance check
            if (Random.value > drop.dropChance) continue;
            
            // Random amount within range
            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            
            if (amount <= 0) continue;
            
            // Spawn item at position above ground
            Vector3 dropPosition = GetGroundPosition(transform.position);
            
            if (drop.itemData.worldPrefab != null)
            {
                GameObject droppedItem = Instantiate(drop.itemData.worldPrefab, dropPosition, Quaternion.identity);
                WorldItem worldItem = droppedItem.GetComponent<WorldItem>();
                
                if (worldItem != null)
                {
                    worldItem.SetItemData(drop.itemData, amount);
                }
                
                // Add slight random force and ensure rigidbody exists
                Rigidbody itemRb = droppedItem.GetComponent<Rigidbody>();
                if (itemRb == null)
                {
                    itemRb = droppedItem.AddComponent<Rigidbody>();
                    itemRb.mass = 0.5f;
                }
                
                // Apply random outward force
                Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0.5f, Random.Range(-1f, 1f)).normalized;
                itemRb.AddForce(randomDir * 3f, ForceMode.Impulse);
            }
        }
    }
    
    public float GetHealthPercentage() => currentHealth / maxHealth;
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;
    public bool IsDead() => isDead;
}



[System.Serializable]
public class ItemDrop
{
    public ItemData itemData;
    [Range(0f, 1f)]
    public float dropChance = 1f; // 1 = always drops
    public int minAmount = 1;
    public int maxAmount = 1;
}