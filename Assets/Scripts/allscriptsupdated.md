# All Scripts

## Combat

### AIBehaviour.cs:

```csharp
using UnityEngine;

public class AIBehavior : MonoBehaviour
{
    [Header("AI Type")]
    public AggroType aggroType = AggroType.Passive;
    
    [Header("Combat Stats")]
    public float attackDamage = 10f;
    public float attackKnockback = 3f;
    public float attackRange = 2f;
    public float attackCooldown = 1f;
    
    [Header("Detection")]
    public float detectionRange = 10f;
    public LayerMask playerMask;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 5f;
    public bool useCreatureMover = false; // Toggle for CreatureMover script
    
    [Header("Flee Behavior (Passive)")]
    public float fleeSpeed = 5f; // Speed when running away
    public float fleeDistance = 15f; // How far to run
    public float fleeDuration = 5f; // How long to flee after being hit
    
    [Header("Animation")]
    public Animator animator;
    public bool useAnimations = true;
    
    // Animation parameter names
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    
    private Transform player;
    private bool isAggro = false;
    private bool isFleeing = false;
    private float fleeTimer = 0f;
    private float lastAttackTime;
    private Rigidbody rb;
    private Health health;
    private AIState currentState = AIState.Idle;
    
    // CreatureMover support
    private Controller.CreatureMover creatureMover;
    private CharacterController characterController;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        creatureMover = GetComponent<Controller.CreatureMover>();
        characterController = GetComponent<CharacterController>();
        
        // Auto-detect movement type
        if (creatureMover != null)
        {
            useCreatureMover = true;
        }
        
        // Auto-find animator if not assigned
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        // Subscribe to death event
        if (health != null)
        {
            health.OnDeath.AddListener(OnDeath);
        }
    }
    
    void Start()
    {
        // Find player
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
    }
    
    void Update()
    {
        if (player == null || health.IsDead()) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        // Update flee timer
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f || distanceToPlayer >= fleeDistance)
            {
                isFleeing = false;
            }
        }
        
        // Passive AI flees when attacked
        if (aggroType == AggroType.Passive && isFleeing)
        {
            SetState(AIState.Fleeing);
            FleeFromPlayer();
            UpdateAnimator(distanceToPlayer);
            return;
        }
        
        // Determine if should be aggro
        switch (aggroType)
        {
            case AggroType.Passive:
                isAggro = false;
                break;
                
            case AggroType.Aggressive:
                if (distanceToPlayer <= detectionRange)
                {
                    isAggro = true;
                }
                break;
                
            case AggroType.Neutral:
                if (distanceToPlayer > detectionRange * 1.5f)
                {
                    isAggro = false;
                }
                break;
        }
        
        // Behavior based on aggro state
        if (isAggro)
        {
            if (distanceToPlayer <= attackRange)
            {
                SetState(AIState.Attacking);
                AttackPlayer();
            }
            else if (distanceToPlayer <= detectionRange)
            {
                SetState(AIState.Chasing);
                ChasePlayer();
            }
            else
            {
                SetState(AIState.Idle);
                StopMovement();
            }
        }
        else
        {
            SetState(AIState.Idle);
            StopMovement();
        }
        
        // Update animator parameters
        UpdateAnimator(distanceToPlayer);
    }
    
    void SetState(AIState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
    }
    
    void ChasePlayer()
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        
        if (useCreatureMover && creatureMover != null)
        {
            // Use CreatureMover system
            Vector2 inputAxis = new Vector2(0, 1); // Move forward
            Vector3 lookTarget = player.position;
            
            creatureMover.SetInput(inputAxis, lookTarget, true, false);
        }
        else if (characterController != null)
        {
            // Use CharacterController directly
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 movement = direction * moveSpeed * Time.deltaTime;
            characterController.Move(movement);
        }
        else if (rb != null)
        {
            // Use Rigidbody
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 targetPosition = rb.position + direction * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            // Fallback: direct transform movement
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            transform.position += direction * moveSpeed * Time.deltaTime;
        }
    }
    
    void FleeFromPlayer()
    {
        // Run away from player
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        fleeDirection.y = 0;
        
        if (useCreatureMover && creatureMover != null)
        {
            // Use CreatureMover - move backward/turn away
            Vector2 inputAxis = new Vector2(0, 1); // Move forward (creature faces away)
            Vector3 lookTarget = transform.position + fleeDirection * 10f; // Look away from player
            
            creatureMover.SetInput(inputAxis, lookTarget, true, false);
        }
        else if (characterController != null)
        {
            if (fleeDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 movement = fleeDirection * fleeSpeed * Time.deltaTime;
            characterController.Move(movement);
        }
        else if (rb != null)
        {
            if (fleeDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 targetPosition = rb.position + fleeDirection * fleeSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            if (fleeDirection != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(fleeDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;
        }
    }
    
    void StopMovement()
    {
        if (useCreatureMover && creatureMover != null)
        {
            creatureMover.SetInput(Vector2.zero, player != null ? player.position : transform.position + transform.forward, false, false);
        }
        else if (rb != null)
        {
            rb.velocity = new Vector3(0, rb.velocity.y, 0);
        }
        // CharacterController stops automatically when no movement applied
    }
    
    void AttackPlayer()
    {
        StopMovement();
        
        // Look at player
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        // Attack if cooldown passed
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            
            // Trigger attack animation
            if (useAnimations && animator != null)
            {
                animator.SetTrigger(AttackTriggerHash);
            }
            
            // Deal damage to player
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
                
                // Apply knockback to player
                Vector3 knockbackDir = (player.position - transform.position).normalized;
                Rigidbody playerRb = player.GetComponent<Rigidbody>();
                CharacterController playerController = player.GetComponent<CharacterController>();
                
                if (playerRb != null)
                {
                    playerRb.AddForce(knockbackDir * attackKnockback, ForceMode.Impulse);
                }
                else if (playerController != null)
                {
                    PlayerCombat playerCombat = player.GetComponent<PlayerCombat>();
                    if (playerCombat != null)
                    {
                        playerCombat.ApplyKnockback(knockbackDir * attackKnockback);
                    }
                }
            }
            
            Debug.Log($"{gameObject.name} attacked player for {attackDamage} damage!");
        }
    }
    
    void UpdateAnimator(float distanceToPlayer)
    {
        if (!useAnimations || animator == null) return;
        
        float normalizedSpeed = 0f;
        
        switch (currentState)
        {
            case AIState.Idle:
                normalizedSpeed = 0f;
                animator.SetBool(IsWalkingHash, false);
                animator.SetBool(IsRunningHash, false);
                break;
                
            case AIState.Chasing:
                normalizedSpeed = 1f;
                animator.SetBool(IsWalkingHash, true);
                animator.SetBool(IsRunningHash, true);
                break;
                
            case AIState.Fleeing:
                normalizedSpeed = 1f;
                animator.SetBool(IsWalkingHash, true);
                animator.SetBool(IsRunningHash, true); // Run animation when fleeing
                break;
                
            case AIState.Attacking:
                normalizedSpeed = 0f;
                animator.SetBool(IsWalkingHash, false);
                animator.SetBool(IsRunningHash, false);
                break;
        }
        
        animator.SetFloat(SpeedHash, normalizedSpeed);
    }
    
    void OnDeath()
    {
        // Play death animation
        if (useAnimations && animator != null)
        {
            animator.SetTrigger(DeathTriggerHash);
        }
        
        // Disable movement components
        if (creatureMover != null) creatureMover.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (rb != null) rb.isKinematic = true;
        
        // Disable AI behavior
        enabled = false;
    }
    
    public void OnAttacked(Vector3 attackSource)
    {
        if (aggroType == AggroType.Neutral)
        {
            isAggro = true;
        }
        else if (aggroType == AggroType.Passive)
        {
            // Start fleeing
            isFleeing = true;
            fleeTimer = fleeDuration;
        }
    }
    
    // Animation event callback
    public void OnAttackHit()
    {
        Debug.Log("Attack animation hit frame!");
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}

public enum AggroType
{
    Passive,
    Aggressive,
    Neutral
}

public enum AIState
{
    Idle,
    Chasing,
    Attacking,
    Fleeing
}
```

### Health.cs:

```csharp
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
```

### PlayerCombat.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCombat : MonoBehaviour
{
    [Header("Combat Stats")]
    public float attackDamage = 20f;
    public float attackKnockback = 5f;
    public float attackRange = 3f;
    public float attackCooldown = 0.5f;
    
    [Header("Attack Detection")]
    public Transform attackPoint; // Point from which to raycast
    public LayerMask attackMask; // What can be attacked
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    [Header("Knockback (when hit)")]
    private Vector3 knockbackVelocity;
    private float knockbackDecay = 5f;
    
    private float lastAttackTime;
    private Camera mainCamera;
    private CharacterController characterController;
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        characterController = GetComponent<CharacterController>();
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        mainCamera = Camera.main;
        
        // Create attack point if it doesn't exist
        if (attackPoint == null)
        {
            GameObject attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.parent = mainCamera.transform;
            attackPointObj.transform.localPosition = Vector3.forward;
            attackPoint = attackPointObj.transform;
        }
    }
    
    void Update()
    {
        // Apply knockback decay
        if (knockbackVelocity.magnitude > 0.1f)
        {
            characterController.Move(knockbackVelocity * Time.deltaTime);
            knockbackVelocity = Vector3.Lerp(knockbackVelocity, Vector3.zero, knockbackDecay * Time.deltaTime);
        }
    }
    
    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            PerformAttack();
            lastAttackTime = Time.time;
        }
    }
    
    void PerformAttack()
    {
        // Raycast from camera center
        RaycastHit hit;
        Vector3 rayOrigin = mainCamera.transform.position;
        Vector3 rayDirection = mainCamera.transform.forward;
        
        if (Physics.Raycast(rayOrigin, rayDirection, out hit, attackRange, attackMask))
        {
            // Hit something
            Health targetHealth = hit.collider.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                // Deal damage with knockback
                targetHealth.TakeDamage(attackDamage, transform.position, attackKnockback);
                
                Debug.Log($"Hit {hit.collider.name} for {attackDamage} damage!");
            }
        }
        
        // Visual feedback (optional - add animation/sound here)
        Debug.DrawRay(rayOrigin, rayDirection * attackRange, Color.red, 0.5f);
    }
    
    public void ApplyKnockback(Vector3 knockback)
    {
        knockbackVelocity = knockback;
    }
    
    void OnDrawGizmosSelected()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera == null) return;
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawRay(mainCamera.transform.position, mainCamera.transform.forward * attackRange);
    }
}
```

## Inventory

### CraftingRecipe.cs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Inventory/Crafting Recipe")]
public class CraftingRecipe : ScriptableObject
{
    [Header("Recipe Info")]
    public string recipeName = "New Recipe";
    public ItemData resultItem;
    public int resultAmount = 1;
    
    [Header("Required Materials")]
    public CraftingMaterial[] requiredMaterials;
    
    [Header("UI")]
    public Sprite recipeIcon; // Icon to show in crafting menu (optional, can use result item icon)
    [TextArea(2, 4)]
    public string description = "Craft description";
    
    public bool CanCraft(InventorySystem inventory)
    {
        if (inventory == null || resultItem == null) return false;
        
        foreach (CraftingMaterial material in requiredMaterials)
        {
            int availableAmount = inventory.GetItemCount(material.item);
            
            if (availableAmount < material.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    public bool Craft(InventorySystem inventory)
    {
        if (!CanCraft(inventory)) return false;
        
        // Check if inventory has space for result
        if (!inventory.HasSpace() && !resultItem.isStackable)
        {
            Debug.Log("Inventory full!");
            return false;
        }
        
        // Remove materials
        foreach (CraftingMaterial material in requiredMaterials)
        {
            bool removed = inventory.RemoveItem(material.item, material.amount);
            if (!removed)
            {
                Debug.LogError("Failed to remove crafting materials!");
                return false;
            }
        }
        
        // Add result item
        bool added = inventory.AddItem(resultItem, resultAmount);
        
        if (!added)
        {
            Debug.LogError("Failed to add crafted item!");
            // Try to return materials (best effort)
            foreach (CraftingMaterial material in requiredMaterials)
            {
                inventory.AddItem(material.item, material.amount);
            }
            return false;
        }
        
        Debug.Log($"Successfully crafted {resultAmount}x {resultItem.itemName}!");
        return true;
    }
    
    public Sprite GetIcon()
    {
        if (recipeIcon != null) return recipeIcon;
        if (resultItem != null && resultItem.icon != null) return resultItem.icon;
        return null;
    }
}

[System.Serializable]
public class CraftingMaterial
{
    public ItemData item;
    public int amount = 1;
}
```

### CraftingSystem.cs:

```csharp
using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class CraftingSystem : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    
    [Header("Recipes")]
    public CraftingRecipe[] allRecipes;
    
    [Header("Events")]
    public UnityEvent<CraftingRecipe> OnRecipeCrafted;
    public UnityEvent OnRecipesUpdated;
    
    void Start()
    {
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        // Listen for inventory changes to update recipe availability
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged.AddListener(OnInventoryChanged);
        }
    }
    
    void OnInventoryChanged()
    {
        OnRecipesUpdated?.Invoke();
    }
    
    public bool CraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || inventorySystem == null) return false;
        
        bool success = recipe.Craft(inventorySystem);
        
        if (success)
        {
            OnRecipeCrafted?.Invoke(recipe);
        }
        
        return success;
    }
    
    public bool CanCraftRecipe(CraftingRecipe recipe)
    {
        if (recipe == null || inventorySystem == null) return false;
        return recipe.CanCraft(inventorySystem);
    }
    
    public List<CraftingRecipe> GetAvailableRecipes()
    {
        List<CraftingRecipe> available = new List<CraftingRecipe>();
        
        foreach (CraftingRecipe recipe in allRecipes)
        {
            if (CanCraftRecipe(recipe))
            {
                available.Add(recipe);
            }
        }
        
        return available;
    }
    
    public List<CraftingRecipe> GetAllRecipes()
    {
        return new List<CraftingRecipe>(allRecipes);
    }
}
```

### CraftingUI.cs:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class CraftingUI : MonoBehaviour
{
    [Header("References")]
    public CraftingSystem craftingSystem;
    public GameObject craftingPanel;
    public Transform recipesContainer;
    public GameObject recipeButtonPrefab;
    
    [Header("Recipe Details Panel")]
    public GameObject recipeDetailsPanel;
    public Image recipeIcon;
    public TextMeshProUGUI recipeNameText;
    public TextMeshProUGUI recipeDescriptionText;
    public Transform materialsContainer;
    public GameObject materialDisplayPrefab;
    public Button craftButton;
    public TextMeshProUGUI craftButtonText;
    
    private List<GameObject> spawnedRecipeButtons = new List<GameObject>();
    private List<GameObject> spawnedMaterialDisplays = new List<GameObject>();
    private CraftingRecipe selectedRecipe;
    
    void Start()
    {
        if (craftingSystem == null)
        {
            craftingSystem = FindObjectOfType<CraftingSystem>();
        }
        
        if (craftingSystem != null)
        {
            craftingSystem.OnRecipesUpdated.AddListener(RefreshRecipes);
        }
        
        if (craftButton != null)
        {
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }
        
        // Hide details panel initially
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(false);
        }
        
        RefreshRecipes();
    }
    
    public void RefreshRecipes()
    {
        // Clear existing buttons
        foreach (GameObject button in spawnedRecipeButtons)
        {
            Destroy(button);
        }
        spawnedRecipeButtons.Clear();
        
        if (craftingSystem == null) return;
        
        // Create button for each recipe
        List<CraftingRecipe> allRecipes = craftingSystem.GetAllRecipes();
        
        foreach (CraftingRecipe recipe in allRecipes)
        {
            GameObject buttonObj = Instantiate(recipeButtonPrefab, recipesContainer);
            
            // Setup button
            Button button = buttonObj.GetComponent<Button>();
            if (button != null)
            {
                CraftingRecipe recipeRef = recipe; // Capture for lambda
                button.onClick.AddListener(() => SelectRecipe(recipeRef));
            }
            
            // Setup visuals
            Image iconImage = buttonObj.transform.Find("Icon")?.GetComponent<Image>();
            if (iconImage != null && recipe.GetIcon() != null)
            {
                iconImage.sprite = recipe.GetIcon();
                iconImage.enabled = true;
            }
            
            TextMeshProUGUI nameText = buttonObj.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
            {
                nameText.text = recipe.recipeName;
            }
            
            // Check if can craft
            bool canCraft = craftingSystem.CanCraftRecipe(recipe);
            
            // Dim if can't craft
            CanvasGroup canvasGroup = buttonObj.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = buttonObj.AddComponent<CanvasGroup>();
            }
            canvasGroup.alpha = canCraft ? 1f : 0.5f;
            
            spawnedRecipeButtons.Add(buttonObj);
        }
    }
    
    void SelectRecipe(CraftingRecipe recipe)
    {
        selectedRecipe = recipe;
        
        Debug.Log($"=== SELECT RECIPE: {recipe.recipeName} ===");
        
        if (recipeDetailsPanel != null)
        {
            recipeDetailsPanel.SetActive(true);
        }
        
        // Update recipe details
        if (recipeIcon != null && recipe.GetIcon() != null)
        {
            recipeIcon.sprite = recipe.GetIcon();
            recipeIcon.enabled = true;
            recipeIcon.color = Color.white;
        }
        
        if (recipeNameText != null)
        {
            recipeNameText.text = recipe.recipeName;
        }
        
        if (recipeDescriptionText != null)
        {
            recipeDescriptionText.text = recipe.description;
        }
        
        // Clear previous materials display
        foreach (GameObject display in spawnedMaterialDisplays)
        {
            Destroy(display);
        }
        spawnedMaterialDisplays.Clear();
        
        Debug.Log($"Recipe has {recipe.requiredMaterials.Length} materials");
        
        // Display required materials
        int index = 0;
        foreach (CraftingMaterial material in recipe.requiredMaterials)
        {
            Debug.Log($"--- Material {index}: {material.item?.itemName ?? "NULL"} ---");
            
            GameObject displayObj = Instantiate(materialDisplayPrefab, materialsContainer);
            Debug.Log($"Instantiated display object: {displayObj.name}");
            
            // Print all children
            Debug.Log($"Display has {displayObj.transform.childCount} children:");
            for (int i = 0; i < displayObj.transform.childCount; i++)
            {
                Transform child = displayObj.transform.GetChild(i);
                Debug.Log($"  Child {i}: {child.name} - Components: {string.Join(", ", child.GetComponents<Component>().Select(c => c.GetType().Name))}");
            }
            
            // Find icon
            Transform iconTransform = displayObj.transform.Find("Image");
            Debug.Log($"Icon transform found: {iconTransform != null}");
            
            Image matIcon = null;
            if (iconTransform != null)
            {
                matIcon = iconTransform.GetComponent<Image>();
                Debug.Log($"Icon Image component found: {matIcon != null}");
            }
            
            // Find texts
            Transform nameTransform = displayObj.transform.Find("ItemName");
            Transform amountTransform = displayObj.transform.Find("Amount");
            Debug.Log($"ItemName found: {nameTransform != null}, Amount found: {amountTransform != null}");
            
            TextMeshProUGUI itemNameText = nameTransform?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI matAmountText = amountTransform?.GetComponent<TextMeshProUGUI>();
            
            // Set icon
            if (matIcon != null)
            {
                if (material.item != null)
                {
                    if (material.item.icon != null)
                    {
                        matIcon.sprite = material.item.icon;
                        matIcon.enabled = true;
                        matIcon.color = Color.white;
                        Debug.Log($"✓ Icon SET for {material.item.itemName}: {material.item.icon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"✗ Material {material.item.itemName} has NO ICON!");
                    }
                }
                else
                {
                    Debug.LogWarning("✗ Material.item is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("✗ matIcon component is NULL!");
            }
            
            // Set item name
            if (itemNameText != null && material.item != null)
            {
                itemNameText.text = material.item.itemName;
                Debug.Log($"Set item name: {material.item.itemName}");
            }
            
            // Set amount
            if (matAmountText != null && material.item != null)
            {
                int available = craftingSystem.inventorySystem.GetItemCount(material.item);
                matAmountText.text = $"{available}/{material.amount}";
                matAmountText.color = available >= material.amount ? Color.white : Color.red;
                Debug.Log($"Set amount: {available}/{material.amount}");
            }
            
            spawnedMaterialDisplays.Add(displayObj);
            index++;
        }
        
        Debug.Log("=== RECIPE SELECTION COMPLETE ===");
        
        // Update craft button
        UpdateCraftButton();
    }
    
    void UpdateCraftButton()
    {
        if (craftButton == null || selectedRecipe == null) return;
        
        bool canCraft = craftingSystem.CanCraftRecipe(selectedRecipe);
        craftButton.interactable = canCraft;
        
        if (craftButtonText != null)
        {
            craftButtonText.text = canCraft ? "CRAFT" : "INSUFFICIENT MATERIALS";
        }
    }
    
    void OnCraftButtonClicked()
    {
        if (selectedRecipe == null) return;
        
        bool success = craftingSystem.CraftRecipe(selectedRecipe);
        
        if (success)
        {
            // Refresh UI
            RefreshRecipes();
            UpdateCraftButton();
            
            // Update materials display
            if (selectedRecipe != null)
            {
                SelectRecipe(selectedRecipe); // Refresh details panel
            }
        }
    }
    
    void OnDestroy()
    {
        if (craftingSystem != null)
        {
            craftingSystem.OnRecipesUpdated.RemoveListener(RefreshRecipes);
        }
    }
}
```

### InventorySlot.cs:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image iconImage;
    public TextMeshProUGUI amountText;
    public GameObject selectedBorder;
    
    [Header("Slot Data")]
    public int slotX;
    public int slotY;
    
    private ItemData currentItem;
    private int currentAmount;
    
    public void Setup(int x, int y)
    {
        slotX = x;
        slotY = y;
        ClearSlot();
    }
    
    public void UpdateSlot(ItemData item, int amount)
    {
        currentItem = item;
        currentAmount = amount;
        
        if (item != null && amount > 0)
        {
            iconImage.sprite = item.icon;
            iconImage.enabled = true;
            iconImage.color = Color.white;
            
            if (item.isStackable && amount > 1)
            {
                amountText.text = amount.ToString();
                amountText.enabled = true;
            }
            else
            {
                amountText.enabled = false;
            }
        }
        else
        {
            ClearSlot();
        }
    }
    
    public void ClearSlot()
    {
        currentItem = null;
        currentAmount = 0;
        iconImage.enabled = false;
        amountText.enabled = false;
        
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(false);
        }
    }
    
    public void OnSlotClicked()
    {
        if (currentItem != null)
        {
            Debug.Log($"Clicked: {currentItem.itemName} x{currentAmount}");
            // Future: Implement item interaction, moving, dropping, etc.
        }
    }
    
    public ItemData GetItem() => currentItem;
    public int GetAmount() => currentAmount;
}
```

### InventorySystem.cs:

```csharp
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class InventorySystem : MonoBehaviour
{
    [Header("Inventory Settings")]
    public int width = 6;
    public int height = 5;
    
    [Header("Events")]
    public UnityEvent<ItemData, int> OnItemAdded;
    public UnityEvent<ItemData, int> OnItemRemoved;
    public UnityEvent OnInventoryChanged;
    
    private InventorySlotData[,] slots;
    
    void Awake()
    {
        InitializeInventory();
    }
    
    void InitializeInventory()
    {
        slots = new InventorySlotData[width, height];
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                slots[x, y] = new InventorySlotData();
            }
        }
    }
    
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        
        int remainingAmount = amount;
        
        // If stackable, try to add to existing stacks first
        if (item.isStackable)
        {
            for (int x = 0; x < width && remainingAmount > 0; x++)
            {
                for (int y = 0; y < height && remainingAmount > 0; y++)
                {
                    if (slots[x, y].item == item && slots[x, y].amount < item.maxStackSize)
                    {
                        int spaceInStack = item.maxStackSize - slots[x, y].amount;
                        int amountToAdd = Mathf.Min(spaceInStack, remainingAmount);
                        
                        slots[x, y].amount += amountToAdd;
                        remainingAmount -= amountToAdd;
                    }
                }
            }
        }
        
        // Add to empty slots
        while (remainingAmount > 0)
        {
            Vector2Int emptySlot = FindEmptySlot();
            
            if (emptySlot.x == -1)
            {
                // Inventory full
                Debug.Log("Inventory is full!");
                OnInventoryChanged?.Invoke();
                return false;
            }
            
            int amountToAdd = item.isStackable ? Mathf.Min(remainingAmount, item.maxStackSize) : 1;
            
            slots[emptySlot.x, emptySlot.y].item = item;
            slots[emptySlot.x, emptySlot.y].amount = amountToAdd;
            remainingAmount -= amountToAdd;
        }
        
        OnItemAdded?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;
        
        int remainingToRemove = amount;
        
        for (int x = 0; x < width && remainingToRemove > 0; x++)
        {
            for (int y = 0; y < height && remainingToRemove > 0; y++)
            {
                if (slots[x, y].item == item)
                {
                    int amountToRemove = Mathf.Min(slots[x, y].amount, remainingToRemove);
                    slots[x, y].amount -= amountToRemove;
                    remainingToRemove -= amountToRemove;
                    
                    if (slots[x, y].amount <= 0)
                    {
                        slots[x, y].item = null;
                        slots[x, y].amount = 0;
                    }
                }
            }
        }
        
        if (remainingToRemove > 0)
        {
            Debug.Log("Not enough items to remove!");
            return false;
        }
        
        OnItemRemoved?.Invoke(item, amount);
        OnInventoryChanged?.Invoke();
        return true;
    }
    
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (slots[x, y].item == item)
                {
                    count += slots[x, y].amount;
                }
            }
        }
        
        return count;
    }
    
    public InventorySlotData GetSlot(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return slots[x, y];
    }
    
    Vector2Int FindEmptySlot()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (slots[x, y].item == null)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        
        return new Vector2Int(-1, -1); // No empty slot found
    }
    
    public bool HasSpace()
    {
        return FindEmptySlot().x != -1;
    }
}

[System.Serializable]
public class InventorySlotData
{
    public ItemData item;
    public int amount;
}
```

### InventoryUI.cs:

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    public GameObject inventoryPanel;
    public Transform slotsParent;
    public GameObject slotPrefab;
    
    [Header("Tabs")]
    public GameObject inventoryTab;
    public GameObject craftingTab;
    public Button inventoryTabButton;
    public Button craftingTabButton;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    [Header("UI Settings")]
    public float slotSize = 80f;
    public float slotSpacing = 10f;
    
    private InventorySlot[,] slotUIArray;
    private bool isInventoryOpen = false;
    private bool isInventoryTabActive = true;
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (inventorySystem == null)
        {
            inventorySystem = FindObjectOfType<InventorySystem>();
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.Inventory.performed += OnInventoryToggle;
    }
    
    void OnDisable()
    {
        inputActions.Player.Inventory.performed -= OnInventoryToggle;
        inputActions.Player.Disable();
    }
    
    void Start()
    {
        CreateInventorySlots();
        inventoryPanel.SetActive(false);
        
        if (inventorySystem != null)
        {
            inventorySystem.OnInventoryChanged.AddListener(RefreshInventoryUI);
        }
        
        // Setup tab buttons
        if (inventoryTabButton != null)
        {
            inventoryTabButton.onClick.AddListener(() => SwitchTab(true));
            Debug.Log("Inventory tab button listener added");
        }
        else
        {
            Debug.LogError("Inventory Tab Button is not assigned!");
        }
        
        if (craftingTabButton != null)
        {
            craftingTabButton.onClick.AddListener(() => SwitchTab(false));
            Debug.Log("Crafting tab button listener added");
        }
        else
        {
            Debug.LogError("Crafting Tab Button is not assigned!");
        }
        
        // Default to inventory tab
        SwitchTab(true);
    }
    
    void CreateInventorySlots()
    {
        slotUIArray = new InventorySlot[inventorySystem.width, inventorySystem.height];
        
        for (int y = 0; y < inventorySystem.height; y++)
        {
            for (int x = 0; x < inventorySystem.width; x++)
            {
                GameObject slotObj = Instantiate(slotPrefab, slotsParent);
                RectTransform rectTransform = slotObj.GetComponent<RectTransform>();
                
                // Position slot
                float posX = x * (slotSize + slotSpacing);
                float posY = -y * (slotSize + slotSpacing);
                rectTransform.anchoredPosition = new Vector2(posX, posY);
                
                // Setup slot
                InventorySlot slot = slotObj.GetComponent<InventorySlot>();
                slot.Setup(x, y);
                slotUIArray[x, y] = slot;
            }
        }
    }
    
    void RefreshInventoryUI()
    {
        for (int x = 0; x < inventorySystem.width; x++)
        {
            for (int y = 0; y < inventorySystem.height; y++)
            {
                InventorySlotData slotData = inventorySystem.GetSlot(x, y);
                slotUIArray[x, y].UpdateSlot(slotData.item, slotData.amount);
            }
        }
    }
    
    void OnInventoryToggle(InputAction.CallbackContext context)
    {
        ToggleInventory();
    }
    
    public void ToggleInventory()
    {
        isInventoryOpen = !isInventoryOpen;
        inventoryPanel.SetActive(isInventoryOpen);
        
        // Lock/unlock cursor
        if (isInventoryOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    
    void SwitchTab(bool showInventory)
    {
        Debug.Log($"Switching tab to: {(showInventory ? "Inventory" : "Crafting")}");
        
        isInventoryTabActive = showInventory;
        
        if (inventoryTab != null)
        {
            inventoryTab.SetActive(showInventory);
            Debug.Log($"Inventory tab set to: {showInventory}");
        }
        else
        {
            Debug.LogError("Inventory Tab GameObject is not assigned!");
        }
        
        if (craftingTab != null)
        {
            craftingTab.SetActive(!showInventory);
            Debug.Log($"Crafting tab set to: {!showInventory}");
        }
        else
        {
            Debug.LogError("Crafting Tab GameObject is not assigned!");
        }
        
        // Update button colors
        UpdateTabButtonColors();
    }
    
    void UpdateTabButtonColors()
    {
        if (inventoryTabButton != null)
        {
            var colors = inventoryTabButton.colors;
            colors.normalColor = isInventoryTabActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            inventoryTabButton.colors = colors;
        }
        
        if (craftingTabButton != null)
        {
            var colors = craftingTabButton.colors;
            colors.normalColor = !isInventoryTabActive ? Color.white : new Color(0.7f, 0.7f, 0.7f);
            craftingTabButton.colors = colors;
        }
    }
}
```

### ItemData.cs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item")]
public class ItemData : ScriptableObject
{
    [Header("Basic Info")]
    public string itemName = "New Item";
    public string itemID = "item_00"; // Unique identifier
    [TextArea(3, 5)]
    public string description = "Item description";
    public Sprite icon;
    
    [Header("Stack Settings")]
    public bool isStackable = true;
    public int maxStackSize = 99;
    
    [Header("Item Type")]
    public ItemType itemType = ItemType.Resource;
    
    [Header("World Representation")]
    public GameObject worldPrefab; // The 3D model to spawn in world
}

public enum ItemType
{
    Resource,
    Food,
    Tool,
    Weapon,
    Consumable
}
```

### WorldItem.cs:

```csharp
using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class WorldItem : MonoBehaviour
{
    [Header("Item Data")]
    public ItemData itemData;
    public int amount = 1;
    
    [Header("Pickup Settings")]
    public float pickupRadius = 2f;
    public bool autoPickup = true;
    public float rotationSpeed = 50f;
    public float bobSpeed = 1f;
    public float bobHeight = 0.3f;
    
    [Header("Visual")]
    public GameObject visualModel;
    
    private Vector3 startPosition;
    private bool canBePickedUp = true;
    private Rigidbody rb;
    private bool hasLanded = false;
    
    void Start()
    {
        startPosition = transform.position;
        
        // Get or add rigidbody
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        
        // Configure rigidbody for items
        rb.mass = 0.5f;
        rb.drag = 0.5f;
        rb.angularDrag = 0.5f;
        
        // Setup collider as trigger
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
        
        // Visual setup
        if (visualModel == null)
        {
            visualModel = transform.childCount > 0 ? transform.GetChild(0).gameObject : null;
        }
    }
    
    void Update()
    {
        // Check if item has landed (stopped falling)
        if (!hasLanded && rb.velocity.magnitude < 0.1f)
        {
            hasLanded = true;
            startPosition = transform.position;
        }
        
        if (visualModel != null && hasLanded)
        {
            // Rotate
            visualModel.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
            
            // Bob up and down (only after landing)
            float newY = startPosition.y + bobHeight + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!canBePickedUp || !autoPickup) return;
        
        if (other.CompareTag("Player"))
        {
            InventorySystem inventory = other.GetComponent<InventorySystem>();
            
            if (inventory != null)
            {
                TryPickup(inventory);
            }
        }
    }
    
    public bool TryPickup(InventorySystem inventory)
    {
        if (itemData == null)
        {
            Debug.LogError("WorldItem has no ItemData assigned!");
            return false;
        }
        
        bool success = inventory.AddItem(itemData, amount);
        
        if (success)
        {
            Debug.Log($"Picked up {amount}x {itemData.itemName}");
            Destroy(gameObject);
            return true;
        }
        else
        {
            Debug.Log("Inventory is full!");
            return false;
        }
    }
    
    public void SetItemData(ItemData data, int itemAmount = 1)
    {
        itemData = data;
        amount = itemAmount;
        
        // Update visual if needed
        if (data.worldPrefab != null && visualModel == null)
        {
            visualModel = Instantiate(data.worldPrefab, transform);
        }
    }
}
```

## Player

### PlayerController.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("Input")]
    public PlayerInputActions playerInputActions;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private Vector2 moveInput;
    private bool jumpInput;
    
    void Awake()
    {
        // Initialize input actions
        playerInputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        // Enable input actions
        playerInputActions.Player.Enable();
        
        // Subscribe to input events
        playerInputActions.Player.Movement.performed += OnMovementPerformed;
        playerInputActions.Player.Movement.canceled += OnMovementCanceled;
        playerInputActions.Player.Jump.performed += OnJumpPerformed;
    }
    
    void OnDisable()
    {
        // Unsubscribe from input events
        playerInputActions.Player.Movement.performed -= OnMovementPerformed;
        playerInputActions.Player.Movement.canceled -= OnMovementCanceled;
        playerInputActions.Player.Jump.performed -= OnJumpPerformed;
        
        // Disable input actions
        playerInputActions.Player.Disable();
    }
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        // Create ground check if it doesn't exist
        if (groundCheck == null)
        {
            GameObject groundCheckObj = new GameObject("GroundCheck");
            groundCheckObj.transform.parent = transform;
            groundCheckObj.transform.localPosition = new Vector3(0, -1f, 0);
            groundCheck = groundCheckObj.transform;
        }
    }
    
    void Update()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
        
        // Movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * walkSpeed * Time.deltaTime);
        
        // Jump
        if (jumpInput && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpInput = false;
        }
        
        // Apply gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
    
    // Input callbacks
    private void OnMovementPerformed(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }
    
    private void OnMovementCanceled(InputAction.CallbackContext context)
    {
        moveInput = Vector2.zero;
    }
    
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        jumpInput = true;
    }
}
```

### PlayerLook.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerBody;
    
    [Header("Input")]
    public PlayerInputActions playerInputActions;
    
    private float xRotation = 0f;
    private Vector2 lookInput;
    
    void Awake()
    {
        // Initialize input actions
        playerInputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        // Enable input actions
        playerInputActions.Player.Enable();
        
        // Subscribe to look input
        playerInputActions.Player.Look.performed += OnLookPerformed;
        playerInputActions.Player.Look.canceled += OnLookCanceled;
    }
    
    void OnDisable()
    {
        // Unsubscribe from look input
        playerInputActions.Player.Look.performed -= OnLookPerformed;
        playerInputActions.Player.Look.canceled -= OnLookCanceled;
        
        // Disable input actions
        playerInputActions.Player.Disable();
    }
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        // Apply mouse sensitivity and delta time scaling
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        
        // Vertical rotation (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Horizontal rotation (yaw)
        playerBody.Rotate(Vector3.up * mouseX);
    }
    
    // Input callbacks
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }
}
```

### PlayerStats.cs:

```csharp
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
```

### PlayerUI.cs:

```csharp
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
```

## Utilities

### NoiseGenerator.cs:

```csharp
using UnityEngine;

public static class NoiseGenerator
{
    public static float[,] GenerateNoiseMap(int width, int height, float scale, int seed, int octaves, float persistence, float lacunarity, Vector2 offset)
    {
        float[,] noiseMap = new float[width, height];
        
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];
        
        for (int i = 0; i < octaves; i++)
        {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) + offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);
        }
        
        if (scale <= 0) scale = 0.0001f;
        
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;
        
        float halfWidth = width / 2f;
        float halfHeight = height / 2f;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float amplitude = 1;
                float frequency = 1;
                float noiseHeight = 0;
                
                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth) / scale * frequency + octaveOffsets[i].x;
                    float sampleY = (y - halfHeight) / scale * frequency + octaveOffsets[i].y;
                    
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                    
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }
                
                if (noiseHeight > maxNoiseHeight) maxNoiseHeight = noiseHeight;
                if (noiseHeight < minNoiseHeight) minNoiseHeight = noiseHeight;
                
                noiseMap[x, y] = noiseHeight;
            }
        }
        
        // Normalize
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                noiseMap[x, y] = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, noiseMap[x, y]);
            }
        }
        
        return noiseMap;
    }
}
```

## World

### BiomeData.cs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewBiome", menuName = "Survival Game/Biome")]
public class BiomeData : ScriptableObject
{
    [Header("Biome Identification")]
    public string biomeName;
    
    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public float heightMultiplier = 10f;
    public float heightOffset = 0f;
    
    [Header("Vegetation")]
    public GameObject[] vegetationPrefabs;
    [Range(0f, 1f)]
    public float vegetationDensity = 0.1f;
    
    [Header("Biome Threshold")]
    [Range(0f, 1f)]
    public float moistureMin = 0f;
    [Range(0f, 1f)]
    public float moistureMax = 1f;
}
```

### DayNightCycle.cs:

```csharp
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class DayNightCycle : MonoBehaviour
{
    [Header("Day/Night Settings")]
    public Light sunLight;
    public Light moonLight;
    public float dayDurationInSeconds = 120f;
    [Range(0f, 1f)]
    public float startTimeOfDay = 0.25f;
    
    [Header("Sun Light Settings (HDRP)")]
    public AnimationCurve lightIntensityCurve;
    public float maxSunIntensity = 100000f;
    public Gradient lightColorGradient;
    public AnimationCurve colorTemperatureCurve;
    public float minColorTemperature = 2000f;
    public float maxColorTemperature = 6500f;
    
    [Header("Moon Light Settings")]
    public float moonIntensity = 20000f;
    public Color moonColor = new Color(0.6f, 0.7f, 0.85f);
    public float moonColorTemperature = 7000f;
    
    [Header("Ambient Settings")]
    public Volume skyVolume;
    public AnimationCurve exposureCurve;
    public float dayExposure = 12f;
    public float nightExposure = 14f;
    
    private float timeOfDay = 0f;
    private HDAdditionalLightData hdSunLightData;
    private HDAdditionalLightData hdMoonLightData;
    private Exposure exposureOverride;
    
    void Start()
    {
        timeOfDay = startTimeOfDay;
        
        if (sunLight != null)
        {
            hdSunLightData = sunLight.GetComponent<HDAdditionalLightData>();
            if (hdSunLightData == null)
            {
                Debug.LogError("Sun Light is missing HDAdditionalLightData component!");
            }
        }
        
        if (moonLight != null)
        {
            hdMoonLightData = moonLight.GetComponent<HDAdditionalLightData>();
            if (hdMoonLightData == null)
            {
                Debug.LogError("Moon Light is missing HDAdditionalLightData component!");
            }
        }
        else
        {
            Debug.LogWarning("Moon Light is not assigned to DayNightCycle!");
        }
        
        if (skyVolume != null && skyVolume.profile.TryGet(out exposureOverride))
        {
            exposureOverride.mode.Override(ExposureMode.Fixed);
        }
        
        InitializeDefaultCurves();
    }
    
    void Update()
    {
        if (sunLight == null || hdSunLightData == null) return;
        
        timeOfDay += Time.deltaTime / dayDurationInSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;
        
        float sunAngle = (timeOfDay * 360f) - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        
        float intensityMultiplier = lightIntensityCurve.Evaluate(timeOfDay);
        hdSunLightData.SetIntensity(maxSunIntensity * intensityMultiplier);
        
        sunLight.color = lightColorGradient.Evaluate(timeOfDay);
        
        float colorTemp = Mathf.Lerp(minColorTemperature, maxColorTemperature, 
                                     colorTemperatureCurve.Evaluate(timeOfDay));
        hdSunLightData.SetColor(sunLight.color, colorTemp);
        
        if (moonLight != null && hdMoonLightData != null)
        {
            float moonAngle = sunAngle + 180f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);
            
            // More reliable night check based on timeOfDay
            bool isNightTime = timeOfDay < 0.25f || timeOfDay > 0.75f;
            
            if (isNightTime)
            {
                hdMoonLightData.SetIntensity(moonIntensity);
                moonLight.enabled = true;
            }
            else
            {
                moonLight.enabled = false;
            }
            
            hdMoonLightData.SetColor(moonColor, moonColorTemperature);
        }
        
        if (exposureOverride != null)
        {
            float exposure = Mathf.Lerp(nightExposure, dayExposure, 
                                       exposureCurve.Evaluate(timeOfDay));
            exposureOverride.fixedExposure.Override(exposure);
        }
    }
    
    void InitializeDefaultCurves()
    {
        if (lightIntensityCurve == null || lightIntensityCurve.keys.Length == 0)
        {
            lightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.3f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.3f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (colorTemperatureCurve == null || colorTemperatureCurve.keys.Length == 0)
        {
            colorTemperatureCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.2f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.2f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (exposureCurve == null || exposureCurve.keys.Length == 0)
        {
            exposureCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.5f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.5f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
        {
            lightColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            lightColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    public bool IsNight()
    {
        return timeOfDay >= 0.75f || timeOfDay <= 0.25f;
    }
    
    public float GetTimeOfDay()
    {
        return timeOfDay;
    }
    
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
    }
}
```

### SkyManager.cs:

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyManager : MonoBehaviour
{

    public float skySpeed;

    // Update is called once per frame
    void Update()
    {
        RenderSettings.skybox.SetFloat("_Rotation", Time.time * skySpeed);
    }
}
```

### TerrainGenerator.cs:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class TerrainGenerator : MonoBehaviour
{
    [Header("World Settings")]
    public int mapWidth = 100;
    public int mapHeight = 100;
    public float scale = 50f;
    public int seed = 0;
    
    [Header("Noise Settings")]
    public int octaves = 4;
    [Range(0, 1)]
    public float persistence = 0.5f;
    public float lacunarity = 2f;
    public Vector2 offset;
    
    [Header("Biomes")]
    public BiomeData[] biomes;
    
    [Header("Vegetation")]
    public Transform vegetationParent;
    public LayerMask groundMask;
    [Range(0.5f, 5f)]
    public float flattenRadius = 1.5f; // How far to flatten around vegetation
    [Range(0f, 1f)]
    public float flattenStrength = 0.8f; // How much to flatten (1 = completely flat)
    
    private GameObject terrainObject;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;
    private List<Vector2> vegetationPositions = new List<Vector2>();

    void Start()
    {
        GenerateWorld();
    }

    public void GenerateWorld()
    {
        // Clear existing terrain
        if (terrainObject != null)
            DestroyImmediate(terrainObject);
            
        // Clear existing vegetation
        if (vegetationParent != null)
        {
            foreach (Transform child in vegetationParent)
            {
                DestroyImmediate(child.gameObject);
            }
        }
        
        vegetationPositions.Clear();
        
        // Generate noise maps
        float[,] heightMap = NoiseGenerator.GenerateNoiseMap(mapWidth, mapHeight, scale, seed, octaves, persistence, lacunarity, offset);
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(mapWidth, mapHeight, scale * 2, seed + 1, 3, 0.5f, 2f, offset);
        
        // Determine vegetation positions FIRST
        DetermineVegetationPositions(moistureMap);
        
        // Flatten terrain around vegetation positions
        FlattenAroundVegetation(heightMap, moistureMap);
        
        // Create terrain mesh
        CreateTerrain(heightMap, moistureMap);
        
        // Wait a frame for physics to update, then spawn vegetation
        StartCoroutine(SpawnVegetationDelayed(moistureMap));
    }

    void DetermineVegetationPositions(float[,] moistureMap)
    {
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);
        
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                
                if (biome != null && biome.vegetationPrefabs.Length > 0)
                {
                    if (Random.value < biome.vegetationDensity)
                    {
                        float randomX = x + Random.Range(-0.5f, 0.5f);
                        float randomY = y + Random.Range(-0.5f, 0.5f);
                        vegetationPositions.Add(new Vector2(randomX, randomY));
                    }
                }
            }
        }
    }

    void FlattenAroundVegetation(float[,] heightMap, float[,] moistureMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        foreach (Vector2 vegPos in vegetationPositions)
        {
            int centerX = Mathf.RoundToInt(vegPos.x);
            int centerY = Mathf.RoundToInt(vegPos.y);
            
            // Get center height
            if (centerX < 0 || centerX >= width || centerY < 0 || centerY >= height) continue;
            
            int moistureX = Mathf.Clamp(centerX, 0, moistureMap.GetLength(0) - 1);
            int moistureY = Mathf.Clamp(centerY, 0, moistureMap.GetLength(1) - 1);
            BiomeData biome = GetBiomeAtPosition(moistureMap[moistureX, moistureY]);
            
            float targetHeight = heightMap[centerX, centerY];
            
            // Flatten in a radius around this position
            int radiusInCells = Mathf.CeilToInt(flattenRadius);
            
            for (int y = centerY - radiusInCells; y <= centerY + radiusInCells; y++)
            {
                for (int x = centerX - radiusInCells; x <= centerX + radiusInCells; x++)
                {
                    if (x < 0 || x >= width || y < 0 || y >= height) continue;
                    
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                    
                    if (distance <= flattenRadius)
                    {
                        // Smooth falloff from center to edge
                        float influence = 1f - (distance / flattenRadius);
                        influence = Mathf.Pow(influence, 2); // Smooth curve
                        influence *= flattenStrength;
                        
                        // Blend between original height and target height
                        heightMap[x, y] = Mathf.Lerp(heightMap[x, y], targetHeight, influence);
                    }
                }
            }
        }
    }

     void CreateTerrain(float[,] heightMap, float[,] moistureMap)
    {
        terrainObject = new GameObject("Terrain");
        terrainObject.transform.parent = transform;
        terrainObject.layer = LayerMask.NameToLayer("Ground");
        
        meshFilter = terrainObject.AddComponent<MeshFilter>();
        meshRenderer = terrainObject.AddComponent<MeshRenderer>();
        meshCollider = terrainObject.AddComponent<MeshCollider>();
        
        Mesh mesh = GenerateMesh(heightMap, moistureMap);
        meshFilter.mesh = mesh;
        meshCollider.sharedMesh = mesh;
        
        // Apply material based on dominant biome (simplified)
        if (biomes.Length > 0 && biomes[0].terrainMaterial != null)
        {
            meshRenderer.material = biomes[0].terrainMaterial;
        }
    }

    Mesh GenerateMesh(float[,] heightMap, float[,] moistureMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Mesh mesh = new Mesh();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        
        Vector3[] vertices = new Vector3[width * height];
        int[] triangles = new int[(width - 1) * (height - 1) * 6];
        Vector2[] uvs = new Vector2[width * height];
        
        int vertIndex = 0;
        int triIndex = 0;
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                float biomeHeight = biome != null ? heightMap[x, y] * biome.heightMultiplier + biome.heightOffset : heightMap[x, y] * 10f;
                
                vertices[vertIndex] = new Vector3(x, biomeHeight, y);
                uvs[vertIndex] = new Vector2(x / (float)width, y / (float)height);
                
                if (x < width - 1 && y < height - 1)
                {
                    triangles[triIndex] = vertIndex;
                    triangles[triIndex + 1] = vertIndex + width;
                    triangles[triIndex + 2] = vertIndex + width + 1;
                    
                    triangles[triIndex + 3] = vertIndex;
                    triangles[triIndex + 4] = vertIndex + width + 1;
                    triangles[triIndex + 5] = vertIndex + 1;
                    
                    triIndex += 6;
                }
                
                vertIndex++;
            }
        }
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        
        return mesh;
    }

    System.Collections.IEnumerator SpawnVegetationDelayed(float[,] moistureMap)
    {
        yield return new WaitForFixedUpdate();
        
        SpawnVegetation(moistureMap);
    }

    void SpawnVegetation(float[,] moistureMap)
    {
        if (vegetationParent == null) return;
        
        int index = 0;
        foreach (Vector2 vegPos in vegetationPositions)
        {
            int x = Mathf.RoundToInt(vegPos.x);
            int y = Mathf.RoundToInt(vegPos.y);
            
            if (x < 0 || x >= moistureMap.GetLength(0) || y < 0 || y >= moistureMap.GetLength(1)) continue;
            
            BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
            
            if (biome != null && biome.vegetationPrefabs.Length > 0)
            {
                GameObject prefab = biome.vegetationPrefabs[Random.Range(0, biome.vegetationPrefabs.Length)];
                
                // Raycast to find exact ground position
                RaycastHit hit;
                Vector3 rayStart = new Vector3(vegPos.x, 100f, vegPos.y);
                
                if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, groundMask))
                {
                    Vector3 position = hit.point;
                    Quaternion rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    
                    GameObject vegetation = Instantiate(prefab, position, rotation, vegetationParent);
                    float scale = Random.Range(0.8f, 1.2f);
                    vegetation.transform.localScale = new Vector3(scale, scale, scale);
                }
            }
            
            index++;
        }
    }

    BiomeData GetBiomeAtPosition(float moisture)
    {
        foreach (BiomeData biome in biomes)
        {
            if (moisture >= biome.moistureMin && moisture <= biome.moistureMax)
            {
                return biome;
            }
        }
        return biomes.Length > 0 ? biomes[0] : null;
    }
}
```


