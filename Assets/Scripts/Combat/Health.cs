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
    public float knockbackResistance = 1f;
    
    [Header("Visual Feedback")]
    public bool flashOnDamage = true;
    public Color damageColor = Color.red;
    public float flashDuration = 0.2f;
    
    [Header("Item Drops")]
    public ItemDrop[] itemDrops;
    public float dropHeight = 1f;
    
    [Header("Events")]
    public UnityEvent<float> OnDamaged;
    public UnityEvent OnDeath;
    
    private bool isDead = false;
    private Rigidbody rb;
    private CharacterController characterController;
    private AIBehavior aiBehavior;
    private MaterialPropertyBlock mpb;
    private Color[] originalColors;

    private Vector3 knockbackVelocity;
    private float knockbackDecay = 5f;
    
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Coroutine flashCoroutine;
    
    void Awake()
    {
        currentHealth = maxHealth;
        rb = GetComponent<Rigidbody>();
        characterController = GetComponent<CharacterController>();
        aiBehavior = GetComponent<AIBehavior>();
        
        renderers = GetComponentsInChildren<Renderer>();
        
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
        
        if (flashOnDamage)
        {
            FlashDamage();
        }
        
        if (aiBehavior != null)
        {
            aiBehavior.OnAttacked(damageSource);
        }
        
        if (canBeKnockedBack && knockbackForce > 0f)
        {
            Vector3 knockbackDirection = (transform.position - damageSource).normalized;
            knockbackDirection.y = 0.3f; 
            
            float finalKnockback = knockbackForce * knockbackResistance;
            
            if (rb != null)
            {
                rb.AddForce(knockbackDirection * finalKnockback, ForceMode.Impulse);
            }
            else if (characterController != null)
            {
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
        
        DropItems();
        
        Destroy(gameObject, 0.1f);
    }

    Vector3 GetGroundPosition(Vector3 origin)
    {
        RaycastHit hit;

        Vector3 rayStart = origin + Vector3.up * 0.5f;

        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.05f;
        }

        return origin;
    }

    
    void DropItems()
    {
        foreach (ItemDrop drop in itemDrops)
        {
            if (drop.itemData == null) continue;
            
            if (Random.value > drop.dropChance) continue;
            
            int amount = Random.Range(drop.minAmount, drop.maxAmount + 1);
            
            if (amount <= 0) continue;
            
            Vector3 dropPosition = GetGroundPosition(transform.position);
            
            if (drop.itemData.worldPrefab != null)
            {
                GameObject droppedItem = Instantiate(drop.itemData.worldPrefab, dropPosition, Quaternion.identity);
                WorldItem worldItem = droppedItem.GetComponent<WorldItem>();
                
                if (worldItem != null)
                {
                    worldItem.SetItemData(drop.itemData, amount);
                }
                
                Rigidbody itemRb = droppedItem.GetComponent<Rigidbody>();
                if (itemRb == null)
                {
                    itemRb = droppedItem.AddComponent<Rigidbody>();
                    itemRb.mass = 0.5f;
                }
                
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
    public float dropChance = 1f;
    public int minAmount = 1;
    public int maxAmount = 1;
}