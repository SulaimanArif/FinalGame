# All Scripts

## Building

### BuildingData.cs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewBuilding", menuName = "Building/Building Data")]
public class BuildingData : ScriptableObject
{
    [Header("Building Info")]
    public string buildingName = "Wall";
    public GameObject prefab;
    public Sprite icon;
    
    [Header("Placement")]
    public Vector3 snapSize = new Vector3(2f, 2f, 0.2f); // Width, Height, Depth
    public Vector3 pivotOffset = Vector3.zero; // Offset from center if needed
    public bool canRotate = true;
    public float rotationIncrement = 90f;
    
    [Header("Snapping")]
    public bool snapToBuildings = true; // Snap to edges of other buildings
    public float snapDistance = 0.5f; // How close to snap (0.5 units)
    
    [Header("Material Cost")]
    public BuildingCost[] costs;
    
    [Header("Preview")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
}

[System.Serializable]
public class BuildingCost
{
    public ItemData material;
    public int amount;
}```

### BuildingHealth.cs:

```csharp
using UnityEngine;

/// <summary>
/// Health component for buildings that enemies can attack
/// Automatically added to buildings when placed
/// </summary>
public class BuildingHealth : MonoBehaviour
{
    [Header("Building Stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    
    [Header("Visual Feedback")]
    public Material damagedMaterial; // Optional: Material when damaged
    public bool changeColorOnDamage = true;
    public Color healthyColor = Color.white;
    public Color damagedColor = Color.red;
    
    [Header("Drop Resources")]
    public bool dropResourcesOnDestroy = true;
    public float resourceDropPercentage = 0.5f; // Return 50% of materials
    
    private Renderer[] renderers;
    private Material[] originalMaterials;
    private float damageFlashTime = 0f;
    
    void Awake()
    {
        currentHealth = maxHealth;
        renderers = GetComponentsInChildren<Renderer>();
        
        // Store original materials
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
        // Flash effect
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
            // Gradually change color based on health
            UpdateHealthColor();
        }
    }
    
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        // Flash red
        FlashDamage();
        
        Debug.Log($"{gameObject.name} took {damage} damage! ({currentHealth}/{maxHealth})");
        
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
        if (damageFlashTime > 0) return; // Don't override flash
        
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
        Debug.Log($"{gameObject.name} was destroyed!");
        
        // TODO: Drop resources if needed
        // if (dropResourcesOnDestroy)
        // {
        //     DropResources();
        // }
        
        // Destroy the building
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
}```

### BuildingSystem.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public InventorySystem inventorySystem;
    public LayerMask placementMask; // What surfaces can we build on
    public LayerMask buildingMask; // Detect other buildings for snapping
    
    [Header("Build Mode")]
    public bool isBuildModeActive = false;
    private InputAction toggleBuildModeAction;
    private InputAction rotateAction;
    private InputAction placeAction;
    public float maxPlacementDistance = 10f;
    
    [Header("Available Buildings")]
    public BuildingData[] availableBuildings;
    
    [Header("Materials")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    private BuildingData currentBuilding;
    private GameObject previewObject;
    private Vector3 currentPlacementPosition;
    private Quaternion currentPlacementRotation;
    private bool canPlace = false;
    private float currentRotation = 0f;
    private List<GameObject> placedBuildings = new List<GameObject>();
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
    }
    
    void OnEnable()
    {
        inputActions ??= new PlayerInputActions();

        toggleBuildModeAction = inputActions.Player.ToggleBuildMode;
        rotateAction = inputActions.Player.RotateBuilding;
        placeAction = inputActions.Player.UseItem;

        toggleBuildModeAction.performed += OnToggleBuildMode;
        rotateAction.performed += OnRotateBuilding;
        placeAction.performed += OnPlaceBuilding;

        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        toggleBuildModeAction.performed -= OnToggleBuildMode;
        rotateAction.performed -= OnRotateBuilding;
        placeAction.performed -= OnPlaceBuilding;

        inputActions.Player.Disable();
    }

    void OnToggleBuildMode(InputAction.CallbackContext ctx)
    {
        ToggleBuildMode();
    }

    void OnRotateBuilding(InputAction.CallbackContext ctx)
    {
        if (!isBuildModeActive || currentBuilding == null || !currentBuilding.canRotate)
            return;

        RotateBuilding();
    }
    
    void Update()
    {
        if (!isBuildModeActive) return;

        if (currentBuilding != null)
        {
            UpdateBuildingPreview();
        }
    }
    
    public void ToggleBuildMode()
    {
        isBuildModeActive = !isBuildModeActive;
        
        if (isBuildModeActive)
        {
            Debug.Log("Build mode ACTIVATED");
            // Lock cursor for building
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // Select first building by default
            if (availableBuildings.Length > 0)
            {
                SelectBuilding(0);
            }
        }
        else
        {
            Debug.Log("Build mode DEACTIVATED");
            ClearPreview();
        }
    }
    
    public void SelectBuilding(int index)
    {
        if (index < 0 || index >= availableBuildings.Length) return;
        
        currentBuilding = availableBuildings[index];
        currentRotation = 0f;
        CreatePreview();
        
        Debug.Log($"Selected building: {currentBuilding.buildingName}");
    }
    
    void CreatePreview()
    {
        ClearPreview();
        
        if (currentBuilding == null || currentBuilding.prefab == null) return;
        
        previewObject = Instantiate(currentBuilding.prefab);
        previewObject.name = "BuildingPreview";
        
        // Disable colliders on preview
        foreach (Collider col in previewObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        
        // Set preview material
        foreach (Renderer rend in previewObject.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = validPlacementMaterial;
            }
            rend.materials = mats;
        }
    }
    
    void ClearPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }
    
    void UpdateBuildingPreview()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, maxPlacementDistance, placementMask | buildingMask))
        {
            // Make preview visible
            if (previewObject != null && !previewObject.activeSelf)
            {
                previewObject.SetActive(true);
            }
            
            // Calculate snapped position
            Vector3 snapPosition = CalculateSnapPosition(hit.point, hit.normal);
            currentPlacementPosition = snapPosition;
            currentPlacementRotation = Quaternion.Euler(0, currentRotation, 0);
            
            // Check if placement is valid
            canPlace = CanPlaceBuilding(snapPosition, currentPlacementRotation);
            
            // Update preview
            if (previewObject != null)
            {
                // Apply pivot offset if exists
                Vector3 finalPosition = currentPlacementPosition;
                if (currentBuilding.pivotOffset != Vector3.zero)
                {
                    finalPosition += currentPlacementRotation * currentBuilding.pivotOffset;
                }
                
                previewObject.transform.position = finalPosition;
                previewObject.transform.rotation = currentPlacementRotation;
                
                // Update material based on validity
                Material previewMat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
                foreach (Renderer rend in previewObject.GetComponentsInChildren<Renderer>())
                {
                    Material[] mats = new Material[rend.materials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = previewMat;
                    }
                    rend.materials = mats;
                }
            }
        }
        else
        {
            // Hide preview if not pointing at valid surface
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
            canPlace = false;
        }
    }
    
    Vector3 CalculateSnapPosition(Vector3 hitPoint, Vector3 hitNormal)
    {
        // Snap to grid based on building's snap size
        Vector3 snapSize = currentBuilding.snapSize;
        
        // Apply rotation to snap size for proper alignment
        Vector3 rotatedSnapSize = snapSize;
        float rotationAngle = Mathf.Abs(currentRotation % 180f);
        if (rotationAngle > 80f && rotationAngle < 100f) // If rotated ~90 degrees
        {
            // Swap X and Z for 90/270 degree rotations
            rotatedSnapSize = new Vector3(snapSize.z, snapSize.y, snapSize.x);
        }
        
        Vector3 snapped = new Vector3(
            Mathf.Round(hitPoint.x / rotatedSnapSize.x) * rotatedSnapSize.x,
            Mathf.Round(hitPoint.y / snapSize.y) * snapSize.y,
            Mathf.Round(hitPoint.z / rotatedSnapSize.z) * rotatedSnapSize.z
        );
        
        return snapped;
    }
    
    bool CanPlaceBuilding(Vector3 position, Quaternion rotation)
    {
        // Check if player has required materials
        if (!HasRequiredMaterials())
        {
            return false;
        }
        
        // Check for overlapping buildings
        Collider[] overlaps = Physics.OverlapBox(
            position,
            currentBuilding.snapSize / 2f,
            rotation,
            buildingMask
        );
        
        return overlaps.Length == 0;
    }
    
    bool HasRequiredMaterials()
    {
        if (inventorySystem == null || currentBuilding == null) return false;
        
        foreach (BuildingCost cost in currentBuilding.costs)
        {
            int available = inventorySystem.GetItemCount(cost.material);
            if (available < cost.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    void OnPlaceBuilding(InputAction.CallbackContext context)
    {
        if (!isBuildModeActive || !canPlace || currentBuilding == null) return;
        
        PlaceBuilding();
    }
    
    void PlaceBuilding()
    {
        // Remove materials from inventory
        foreach (BuildingCost cost in currentBuilding.costs)
        {
            inventorySystem.RemoveItem(cost.material, cost.amount);
        }
        
        // Apply pivot offset if needed
        Vector3 finalPosition = currentPlacementPosition;
        if (currentBuilding.pivotOffset != Vector3.zero)
        {
            finalPosition += currentPlacementRotation * currentBuilding.pivotOffset;
        }
        
        // Instantiate actual building
        GameObject building = Instantiate(
            currentBuilding.prefab,
            finalPosition,
            currentPlacementRotation
        );
        
        building.name = currentBuilding.buildingName;
        
        // Set to building layer
        building.layer = LayerMask.NameToLayer("Building");
        foreach (Transform child in building.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = LayerMask.NameToLayer("Building");
        }
        
        // Add BuildingHealth component if it doesn't have one
        if (building.GetComponent<BuildingHealth>() == null)
        {
            BuildingHealth buildingHealth = building.AddComponent<BuildingHealth>();
            buildingHealth.maxHealth = 100f; // Default health, can customize per building type
            Debug.Log($"Added BuildingHealth to {building.name}");
        }
        
        placedBuildings.Add(building);
        
        Debug.Log($"Placed {currentBuilding.buildingName} at {finalPosition} with rotation {currentRotation}Â°");
    }
    
    void RotateBuilding()
    {
        currentRotation += currentBuilding.rotationIncrement;
        if (currentRotation >= 360f) currentRotation = 0f;
        
        Debug.Log($"Rotated building to {currentRotation}Â°");
    }
    
    void OnDrawGizmos()
    {
        if (isBuildModeActive && currentBuilding != null && previewObject != null)
        {
            // Draw placement bounds
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(currentPlacementPosition, currentPlacementRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, currentBuilding.snapSize);
        }
    }
}```

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
    
    [Header("Building Attack (Day 3+)")]
    public bool canAttackBuildings = true;
    public float buildingAttackRange = 2.5f;
    public LayerMask buildingMask;
    private GameObject targetBuilding = null;
    
    [Header("Detection")]
    public float detectionRange = 10f;
    public LayerMask playerMask;
    
    [Header("Movement")]
    public float moveSpeed = 3.5f;
    public float rotationSpeed = 5f;
    public bool useCreatureMover = false;
    public float gravity = -9.81f; // Gravity for CharacterController
    
    [Header("Wandering Behavior")]
    public bool enableWandering = true;
    public float wanderSpeed = 2f;
    public float minWanderTime = 3f;
    public float maxWanderTime = 8f;
    public float minIdleTime = 2f;
    public float maxIdleTime = 5f;
    public float wanderRadius = 15f;
    
    [Header("Grazing Behavior")]
    public bool canGraze = true;
    public float grazeChance = 0.3f;
    public float minGrazeTime = 2f;
    public float maxGrazeTime = 4f;
    
    [Header("Flee Behavior (Passive)")]
    public float fleeSpeed = 5f;
    public float fleeDistance = 15f;
    public float fleeDuration = 5f;
    
    [Header("Animation")]
    public Animator animator;
    public bool useAnimations = true;
    public bool useCreatureMoverAnimations = false; // Use Vert/State instead of IsWalking/Speed
    
    // Standard animation parameter names
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    
    // CreatureMover animation parameter names
    private static readonly int VertHash = Animator.StringToHash("Vert");
    private static readonly int StateHash = Animator.StringToHash("State");
    
    private Transform player;
    private bool isAggro = false;
    private bool isFleeing = false;
    private float fleeTimer = 0f;
    private float lastAttackTime;
    private Rigidbody rb;
    private Health health;
    private AIState currentState = AIState.Idle;
    
    // Wandering
    private Vector3 spawnPoint;
    private Vector3 wanderTarget;
    private float stateTimer;
    private Vector3 verticalVelocity; // For gravity
    
    // CreatureMover support
    private Controller.CreatureMover creatureMover;
    private CharacterController characterController;
    
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<Health>();
        creatureMover = GetComponent<Controller.CreatureMover>();
        characterController = GetComponent<CharacterController>();
        
        if (creatureMover != null)
        {
            useCreatureMover = true;
            useCreatureMoverAnimations = true; // Auto-detect CreatureMover animations
        }
        
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        
        if (health != null)
        {
            health.OnDeath.AddListener(OnDeath);
        }
    }
    
    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        spawnPoint = transform.position;
        
        if (enableWandering)
        {
            StartNewWanderState();
        }
    }
    
    void Update()
    {
        if (player == null || health.IsDead()) return;
        
        // Apply gravity to CharacterController
        if (characterController != null && !useCreatureMover)
        {
            if (characterController.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f; // Small downward force to keep grounded
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }
            
            characterController.Move(verticalVelocity * Time.deltaTime);
        }
        
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
            // Wandering behavior when not aggro
            if (enableWandering)
            {
                UpdateWandering();
            }
            else
            {
                SetState(AIState.Idle);
                StopMovement();
            }
        }
        
        UpdateAnimator(distanceToPlayer);
    }
    
    void UpdateWandering()
    {
        stateTimer -= Time.deltaTime;
        
        if (stateTimer <= 0)
        {
            StartNewWanderState();
        }
        
        switch (currentState)
        {
            case AIState.Idle:
            case AIState.Grazing:
                // Stop all movement when idle/grazing
                if (useCreatureMover && creatureMover != null)
                {
                    creatureMover.SetInput(Vector2.zero, transform.position + transform.forward, false, false);
                }
                else if (rb != null)
                {
                    rb.velocity = new Vector3(0, rb.velocity.y, 0);
                }
                break;
                
            case AIState.Wandering:
                WanderToTarget();
                break;
        }
    }
    
    void StartNewWanderState()
    {
        // Randomly decide next state
        if (currentState == AIState.Wandering)
        {
            // After wandering, go idle or graze
            if (canGraze && Random.value < grazeChance)
            {
                SetState(AIState.Grazing);
                stateTimer = Random.Range(minGrazeTime, maxGrazeTime);
            }
            else
            {
                SetState(AIState.Idle);
                stateTimer = Random.Range(minIdleTime, maxIdleTime);
            }
        }
        else
        {
            // After idle/grazing, start wandering
            SetState(AIState.Wandering);
            
            // Pick random point within wander radius of spawn point
            Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
            wanderTarget = spawnPoint + new Vector3(randomCircle.x, 0, randomCircle.y);
            stateTimer = Random.Range(minWanderTime, maxWanderTime);
        }
    }
    
    void WanderToTarget()
    {
        Vector3 direction = (wanderTarget - transform.position).normalized;
        direction.y = 0;
        
        if (useCreatureMover && creatureMover != null)
        {
            Vector2 inputAxis = new Vector2(0, 1);
            creatureMover.SetInput(inputAxis, wanderTarget, false, false); // Changed to false for run
        }
        else if (characterController != null)
        {
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 movement = direction * wanderSpeed * Time.deltaTime;
            characterController.Move(movement);
        }
        else if (rb != null)
        {
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            Vector3 targetPosition = rb.position + direction * wanderSpeed * Time.fixedDeltaTime;
            rb.MovePosition(targetPosition);
        }
        else
        {
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
            }
            
            transform.position += direction * wanderSpeed * Time.deltaTime;
        }
        
        // Check if reached target
        float distanceToTarget = Vector3.Distance(transform.position, wanderTarget);
        if (distanceToTarget < 1f)
        {
            StartNewWanderState();
        }
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
            Vector2 inputAxis = new Vector2(0, 1);
            Vector3 lookTarget = player.position;
            
            creatureMover.SetInput(inputAxis, lookTarget, true, false);
        }
        else if (characterController != null)
        {
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
        Vector3 fleeDirection = (transform.position - player.position).normalized;
        fleeDirection.y = 0;
        
        if (useCreatureMover && creatureMover != null)
        {
            Vector2 inputAxis = new Vector2(0, 1);
            Vector3 lookTarget = transform.position + fleeDirection * 10f;
            
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
    }
    
    void AttackPlayer()
    {
        StopMovement();
        
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0;
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
        
        if (Time.time >= lastAttackTime + attackCooldown)
        {
            lastAttackTime = Time.time;
            
            if (useAnimations && animator != null)
            {
                animator.SetTrigger(AttackTriggerHash);
            }
            
            PlayerStats playerStats = player.GetComponent<PlayerStats>();
            if (playerStats != null)
            {
                playerStats.TakeDamage(attackDamage);
                
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
        
        if (useCreatureMoverAnimations)
        {
            // Use CreatureMover's Vert/State system
            float vert = 0f; // Movement magnitude (0-1)
            float state = 0f; // 0 = walk, 1 = run
            
            switch (currentState)
            {
                case AIState.Idle:
                case AIState.Grazing:
                    vert = 0f;
                    state = 0f;
                    break;
                    
                case AIState.Wandering:
                    vert = 0.5f; // Slow walk
                    state = 0f; // Walking
                    break;
                    
                case AIState.Chasing:
                case AIState.Fleeing:
                    vert = 1f; // Full speed
                    state = 1f; // Running
                    break;
                    
                case AIState.Attacking:
                    vert = 0f;
                    state = 0f;
                    break;
            }
            
            animator.SetFloat(VertHash, vert);
            animator.SetFloat(StateHash, state);
            
            // Handle grazing if parameter exists
            SetAnimatorBoolIfExists(IsGrazingHash, currentState == AIState.Grazing);
        }
        else
        {
            // Use standard IsWalking/Speed system
            float normalizedSpeed = 0f;
            bool isGrazing = false;
            bool isWalking = false;
            bool isRunning = false;
            
            switch (currentState)
            {
                case AIState.Idle:
                    normalizedSpeed = 0f;
                    break;
                    
                case AIState.Wandering:
                    normalizedSpeed = 0.5f;
                    isWalking = true;
                    break;
                    
                case AIState.Grazing:
                    normalizedSpeed = 0f;
                    isGrazing = true;
                    break;
                    
                case AIState.Chasing:
                    normalizedSpeed = 1f;
                    isWalking = true;
                    isRunning = true;
                    break;
                    
                case AIState.Fleeing:
                    normalizedSpeed = 1f;
                    isWalking = true;
                    isRunning = true;
                    break;
                    
                case AIState.Attacking:
                    normalizedSpeed = 0f;
                    break;
            }
            
            animator.SetFloat(SpeedHash, normalizedSpeed);
            animator.SetBool(IsWalkingHash, isWalking);
            animator.SetBool(IsRunningHash, isRunning);
            SetAnimatorBoolIfExists(IsGrazingHash, isGrazing);
        }
    }
    
    void SetAnimatorBoolIfExists(int paramHash, bool value)
    {
        if (animator.parameters.Length > 0)
        {
            foreach (var param in animator.parameters)
            {
                if (param.nameHash == paramHash)
                {
                    animator.SetBool(paramHash, value);
                    break;
                }
            }
        }
    }
    
    void OnDeath()
    {
        if (useAnimations && animator != null)
        {
            animator.SetTrigger(DeathTriggerHash);
        }
        
        if (creatureMover != null) creatureMover.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (rb != null) rb.isKinematic = true;
        
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
            isFleeing = true;
            fleeTimer = fleeDuration;
        }
    }
    
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
        
        // Draw building attack range
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, buildingAttackRange);
        
        if (enableWandering)
        {
            Vector3 center = Application.isPlaying ? spawnPoint : transform.position;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(center, wanderRadius);
            
            if (Application.isPlaying && currentState == AIState.Wandering)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(wanderTarget, 0.5f);
                Gizmos.DrawLine(transform.position, wanderTarget);
            }
        }
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
    Wandering,
    Grazing,
    Chasing,
    Attacking,
    Fleeing
}   ```

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
}```

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
}```

## Game

### GameProgressionSystem.cs:

```csharp
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages 7-day survival game progression
/// Each day enemies get stronger, survive 7 days to win
/// </summary>
public class GameProgressionSystem : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalDaysToSurvive = 7;
    public int currentDay = 1;
    
    [Header("Player Reference")]
    public PlayerStats playerStats; // Hook into player death
    
    [Header("Day/Night Cycle")]
    public DayNightCycle dayNightCycle;
    public float nightStartTime = 0.75f; // When night begins (0.75 = 75% through day)
    public float dayStartTime = 0.25f; // When day begins (0.25 = 25% through day)
    
    [Header("Difficulty Scaling")]
    [Tooltip("Enemy health multiplier per day")]
    public float healthScalePerDay = 1.3f; // 30% more HP each day
    
    [Tooltip("Enemy damage multiplier per day")]
    public float damageScalePerDay = 1.25f; // 25% more damage each day
    
    [Tooltip("Enemy speed multiplier per day")]
    public float speedScalePerDay = 1.1f; // 10% faster each day
    
    [Tooltip("More enemies spawn each day")]
    public int additionalEnemiesPerDay = 2;
    
    [Header("Events")]
    public UnityEvent<int> OnDayChanged; // Passes new day number
    public UnityEvent<int> OnNightStarted; // Passes current day
    public UnityEvent OnGameWon;
    public UnityEvent OnGameLost;
    
    [Header("UI")]
    public bool showDebugInfo = true;
    
    private bool isNightTime = false;
    private bool hasWon = false;
    private bool hasLost = false;
    private int nightsSurvived = 0;
    
    void Start()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<DayNightCycle>();
        }
        
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }
        
        // Hook into player death event
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.AddListener(OnPlayerDeath);
        }
        
        if (dayNightCycle != null)
        {
            // Start at day time
            dayNightCycle.SetTimeOfDay(0.3f);
        }
        
        // Notify systems of starting day
        OnDayChanged?.Invoke(currentDay);
        
        if (showDebugInfo)
        {
            Debug.Log($"=== GAME STARTED ===");
            Debug.Log($"Survive {totalDaysToSurvive} nights to win!");
            Debug.Log($"Starting Day {currentDay}");
        }
    }
    
    void Update()
    {
        if (hasWon || hasLost) return;
        
        CheckDayNightTransition();
    }
    
    void CheckDayNightTransition()
    {
        if (dayNightCycle == null) return;
        
        float timeOfDay = dayNightCycle.GetTimeOfDay();
        bool isCurrentlyNight = timeOfDay >= nightStartTime || timeOfDay < dayStartTime;
        
        // Night just started
        if (isCurrentlyNight && !isNightTime)
        {
            isNightTime = true;
            OnNightStart();
        }
        // Day just started
        else if (!isCurrentlyNight && isNightTime)
        {
            isNightTime = false;
            OnDayStart();
        }
    }
    
    void OnNightStart()
    {
        if (showDebugInfo)
        {
            Debug.Log($"=== NIGHT {currentDay} STARTED ===");
            Debug.Log($"Enemies are {GetHealthMultiplier():F1}x stronger!");
            Debug.Log($"Survive until dawn...");
        }
        
        OnNightStarted?.Invoke(currentDay);
        
        // Apply difficulty scaling to spawned enemies
        ApplyDifficultyToEnemies();
    }
    
    void OnDayStart()
    {
        nightsSurvived++;
        
        if (showDebugInfo)
        {
            Debug.Log($"=== DAY {currentDay} - SURVIVED THE NIGHT ===");
            Debug.Log($"Nights survived: {nightsSurvived}/{totalDaysToSurvive}");
        }
        
        // Check win condition
        if (nightsSurvived >= totalDaysToSurvive)
        {
            WinGame();
            return;
        }
        
        // Progress to next day
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        
        if (showDebugInfo)
        {
            Debug.Log($"=== DAY {currentDay} ===");
            Debug.Log($"Prepare for tonight...");
            Debug.Log($"Next night enemies will be even stronger!");
        }
    }
    
    void ApplyDifficultyToEnemies()
    {
        // Find all active enemies and buff them
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            ApplyScalingToEnemy(enemy);
        }
        
        if (showDebugInfo && enemies.Length > 0)
        {
            Debug.Log($"Applied Day {currentDay} scaling to {enemies.Length} enemies");
        }
    }
    
    public void ApplyScalingToEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        // Scale health
        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            float healthMultiplier = GetHealthMultiplier();
            float newMaxHealth = health.GetMaxHealth() * healthMultiplier;
            
            // Use reflection to set max health (since it's private)
            System.Reflection.FieldInfo maxHealthField = typeof(Health).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null)
            {
                maxHealthField.SetValue(health, newMaxHealth);
                
                // Also set current health to max
                System.Reflection.FieldInfo currentHealthField = typeof(Health).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentHealthField != null)
                {
                    currentHealthField.SetValue(health, newMaxHealth);
                }
            }
        }
        
        // Scale damage
        AIBehavior ai = enemy.GetComponent<AIBehavior>();
        if (ai != null)
        {
            float damageMultiplier = GetDamageMultiplier();
            ai.attackDamage *= damageMultiplier;
            
            // Scale speed
            float speedMultiplier = GetSpeedMultiplier();
            ai.moveSpeed *= speedMultiplier;
            ai.fleeSpeed *= speedMultiplier;
        }
    }
    
    public float GetHealthMultiplier()
    {
        return Mathf.Pow(healthScalePerDay, currentDay - 1);
    }
    
    public float GetDamageMultiplier()
    {
        return Mathf.Pow(damageScalePerDay, currentDay - 1);
    }
    
    public float GetSpeedMultiplier()
    {
        return Mathf.Pow(speedScalePerDay, currentDay - 1);
    }
    
    public int GetExtraEnemiesForDay()
    {
        return (currentDay - 1) * additionalEnemiesPerDay;
    }
    
    void WinGame()
    {
        if (hasWon) return;
        
        hasWon = true;
        
        if (showDebugInfo)
        {
            Debug.Log("======================");
            Debug.Log("=== YOU WIN! ===");
            Debug.Log($"Survived {totalDaysToSurvive} nights!");
            Debug.Log("======================");
        }
        
        OnGameWon?.Invoke();
        
        // Pause game or show victory screen
        Time.timeScale = 0f;
    }
    
    public void LoseGame()
    {
        if (hasLost || hasWon) return;
        
        hasLost = true;
        
        if (showDebugInfo)
        {
            Debug.Log("======================");
            Debug.Log("=== GAME OVER ===");
            Debug.Log($"Survived {nightsSurvived}/{totalDaysToSurvive} nights");
            Debug.Log($"Died on Day {currentDay}");
            Debug.Log("======================");
        }
        
        OnGameLost?.Invoke();
        
        // Show game over screen
        Time.timeScale = 0f;
    }
    
    public bool HasWon() => hasWon;
    public bool HasLost() => hasLost;
    public int GetCurrentDay() => currentDay;
    public int GetNightsSurvived() => nightsSurvived;
    public bool IsNightTime() => isNightTime;
    
    // Call this when player dies
    public void OnPlayerDeath()
    {
        LoseGame();
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        
        // Day counter
        GUI.Label(new Rect(10, 10, 300, 30), $"Day {currentDay} / {totalDaysToSurvive}", style);
        GUI.Label(new Rect(10, 35, 300, 30), $"Nights Survived: {nightsSurvived}", style);
        
        // Current time
        string timeOfDayStr = isNightTime ? "NIGHT" : "DAY";
        GUI.Label(new Rect(10, 60, 300, 30), $"Time: {timeOfDayStr}", style);
        
        // Difficulty info
        GUI.Label(new Rect(10, 90, 400, 30), $"Enemy HP: {GetHealthMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 115, 400, 30), $"Enemy Damage: {GetDamageMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 140, 400, 30), $"Enemy Speed: {GetSpeedMultiplier():F1}x", style);
    }
}```

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
}```

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
}```

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
                        Debug.Log($"âœ“ Icon SET for {material.item.itemName}: {material.item.icon.name}");
                    }
                    else
                    {
                        Debug.LogWarning($"âœ— Material {material.item.itemName} has NO ICON!");
                    }
                }
                else
                {
                    Debug.LogWarning("âœ— Material.item is NULL!");
                }
            }
            else
            {
                Debug.LogWarning("âœ— matIcon component is NULL!");
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
}```

### InventorySlot.cs:

```csharp
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventorySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
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
    private Canvas canvas;
    private InventoryUI inventoryUI;
    
    // Drag data
    private GameObject draggedIcon;
    private RectTransform draggedRectTransform;
    private CanvasGroup draggedCanvasGroup;
    
    void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        
        // InventoryUI is not in parent hierarchy, so find it in scene
        inventoryUI = FindObjectOfType<InventoryUI>();
        
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI not found in scene!");
        }
    }
    
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
    
    public void SetSelected(bool selected)
    {
        if (selectedBorder != null)
        {
            selectedBorder.SetActive(selected);
        }
    }
    
    // Pointer Click - Select item or unequip
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"=== SLOT CLICKED === Position: ({slotX}, {slotY}), Item: {currentItem?.itemName ?? "EMPTY"}");
        
        if (inventoryUI == null)
        {
            Debug.LogError("InventoryUI is NULL!");
            return;
        }
        
        if (currentItem != null)
        {
            Debug.Log("Selecting item...");
            inventoryUI.SelectItem(this);
        }
        else
        {
            Debug.Log("Unequipping item...");
            inventoryUI.UnequipItem();
        }
    }
    
    // Drag Begin
    public void OnBeginDrag(PointerEventData eventData)
    {

        Debug.Log($"=== BEGIN DRAG === Item: {currentItem?.itemName ?? "EMPTY"}");
        if (currentItem == null) return;
        
        // Create dragged icon
        draggedIcon = new GameObject("DraggedIcon");
        draggedIcon.transform.SetParent(canvas.transform, false);
        draggedIcon.transform.SetAsLastSibling();
        
        draggedRectTransform = draggedIcon.AddComponent<RectTransform>();
        draggedRectTransform.sizeDelta = new Vector2(60, 60);
        
        Image image = draggedIcon.AddComponent<Image>();
        image.sprite = currentItem.icon;
        image.raycastTarget = false;
        
        draggedCanvasGroup = draggedIcon.AddComponent<CanvasGroup>();
        draggedCanvasGroup.alpha = 0.6f;
        draggedCanvasGroup.blocksRaycasts = false;
        
        // Make original slot semi-transparent
        iconImage.color = new Color(1, 1, 1, 0.5f);
    }
    
    // Drag
    public void OnDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            draggedRectTransform.position = eventData.position;
        }
    }
    
    // Drag End
    // Drag End
    public void OnEndDrag(PointerEventData eventData)
    {
        if (draggedIcon != null)
        {
            Destroy(draggedIcon);
        }
        
        // Restore original slot alpha
        iconImage.color = Color.white;
        
        // Check if dropped on another slot
        GameObject targetObject = eventData.pointerCurrentRaycast.gameObject;
        InventorySlot targetSlot = null;
        
        // Check if we hit a slot or a child of a slot
        if (targetObject != null)
        {
            targetSlot = targetObject.GetComponent<InventorySlot>();
            if (targetSlot == null)
            {
                // Maybe we hit a child (like the icon or amount text)
                targetSlot = targetObject.GetComponentInParent<InventorySlot>();
            }
        }
        
        if (targetSlot != null && targetSlot != this && inventoryUI != null)
        {
            inventoryUI.SwapItems(this, targetSlot);
        }
    }

    public void OnSlotClicked()
    {
        if (inventoryUI == null) return;
        
        if (currentItem != null)
        {
            // Select this item
            inventoryUI.SelectItem(this);
        }
        else
        {
            // Clicked empty slot - unequip
            inventoryUI.UnequipItem();
        }
    }
    
    public ItemData GetItem() => currentItem;
    public int GetAmount() => currentAmount;
}```

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

        if (item.itemType == ItemType.Tool && item.toolData != null)
        {
            // Tools don't stack - add individually
            while (remainingAmount > 0)
            {
                Vector2Int emptySlot = FindEmptySlot();
                
                if (emptySlot.x == -1)
                {
                    Debug.Log("Inventory is full!");
                    OnInventoryChanged?.Invoke();
                    return false;
                }
                
                // Create new tool instance
                slots[emptySlot.x, emptySlot.y].item = item;
                slots[emptySlot.x, emptySlot.y].amount = 1;
                slots[emptySlot.x, emptySlot.y].toolInstance = new ToolInstance(item.toolData);
                
                remainingAmount--;
            }
            
            OnItemAdded?.Invoke(item, amount);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
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
    public ToolInstance toolInstance; // Track tool durability per slot
}```

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

    [Header("Item Details")]
    public GameObject itemDetailPanel;
    public Image detailIcon;
    public TextMeshProUGUI detailNameText;
    public TextMeshProUGUI detailDescriptionText;
    public TextMeshProUGUI detailTypeText;
    public TextMeshProUGUI detailStatsText;

    private InventorySlot selectedSlot;
    
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
                
                // Wire up button if it exists
                Button button = slotObj.GetComponent<Button>();
                if (button != null)
                {
                    button.onClick.AddListener(slot.OnSlotClicked);
                }
                
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


    public void SelectItem(InventorySlot slot)
    {

        Debug.Log($"=== SELECT ITEM CALLED ===");
        Debug.Log($"Slot position: ({slot.slotX}, {slot.slotY})");
        Debug.Log($"Slot item: {slot.GetItem()?.itemName ?? "NULL"}");
        Debug.Log($"Slot amount: {slot.GetAmount()}");
        // Deselect previous slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }
        
        // Select new slot
        selectedSlot = slot;
        selectedSlot.SetSelected(true);
        
        // Get the actual item from the slot
        ItemData itemToEquip = slot.GetItem();
        
        // Update held item display
        PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
        if (itemUse != null)
        {
            itemUse.SetSelectedItem(itemToEquip);
        }
        
        // Show item details
        ShowItemDetails(itemToEquip);
        
        Debug.Log($"Selected item: {(itemToEquip != null ? itemToEquip.itemName : "NULL")}");
    }

    public void UnequipItem()
    {
        // Deselect current slot
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
            selectedSlot = null;
        }
        
        // Clear held item
        PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
        if (itemUse != null)
        {
            itemUse.SetSelectedItem(null);
        }
        
        // Hide item details
        HideItemDetails();
        
        Debug.Log("Unequipped item");
    }

    public void SwapItems(InventorySlot slotA, InventorySlot slotB)
    {
        if (inventorySystem == null) return;
        
        // Get slot data
        InventorySlotData dataA = inventorySystem.GetSlot(slotA.slotX, slotA.slotY);
        InventorySlotData dataB = inventorySystem.GetSlot(slotB.slotX, slotB.slotY);
        
        if (dataA == null || dataB == null) return;
        
        // Swap the data
        ItemData tempItem = dataA.item;
        int tempAmount = dataA.amount;
        
        dataA.item = dataB.item;
        dataA.amount = dataB.amount;
        
        dataB.item = tempItem;
        dataB.amount = tempAmount;
        
        // IMPORTANT: Trigger inventory changed event
        inventorySystem.OnInventoryChanged?.Invoke();
        
        // Refresh UI
        RefreshInventoryUI();
        
        // If we swapped with the selected slot, update held item
        if (selectedSlot == slotA || selectedSlot == slotB)
        {
            PlayerItemUse itemUse = FindObjectOfType<PlayerItemUse>();
            if (itemUse != null && selectedSlot != null)
            {
                itemUse.SetSelectedItem(selectedSlot.GetItem());
            }
        }
        
        Debug.Log($"Swapped items between slot ({slotA.slotX},{slotA.slotY}) and ({slotB.slotX},{slotB.slotY})");
    }

    void ShowItemDetails(ItemData item)
    {
        if (itemDetailPanel == null || item == null)
        {
            HideItemDetails();
            return;
        }
        
        itemDetailPanel.SetActive(true);
        
        // Icon
        if (detailIcon != null)
        {
            detailIcon.sprite = item.icon;
            detailIcon.enabled = item.icon != null;
        }
        
        // Name
        if (detailNameText != null)
        {
            detailNameText.text = item.itemName;
        }
        
        // Description
        if (detailDescriptionText != null)
        {
            detailDescriptionText.text = item.description;
        }
        
        // Type
        if (detailTypeText != null)
        {
            detailTypeText.text = $"Type: {item.itemType}";
        }
        
        // Stats (food stats, etc)
        if (detailStatsText != null)
        {
            string stats = "";
            
            if (item.isEdible)
            {
                stats += $"<color=#90EE90>Hunger: +{item.hungerRestoreAmount}</color>\n";
                if (item.healthRestoreAmount > 0)
                {
                    stats += $"<color=#FF6B6B>Health: +{item.healthRestoreAmount}</color>\n";
                }
            }
            
            if (item.isStackable)
            {
                stats += $"Max Stack: {item.maxStackSize}";
            }
            
            detailStatsText.text = stats;
        }
    }

    void HideItemDetails()
    {
        if (itemDetailPanel != null)
        {
            itemDetailPanel.SetActive(false);
        }
    }
}```

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
    
    [Header("Food Properties")]
    public bool isEdible = false;
    public float hungerRestoreAmount = 20f;
    public float healthRestoreAmount = 0f; // Optional: some food might heal too

    [Header("Tool Properties (if Tool type)")]
    public ToolData toolData; // Reference to tool data

    // This will be set at runtime for each tool instance
    [System.NonSerialized]
    public ToolInstance toolInstance;
}

public enum ItemType
{
    Resource,
    Food,
    Tool,
    Weapon,
    Consumable
}```

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
}```

## Player

### BuildingHammer.cs:

```csharp
using UnityEngine;

/// <summary>
/// Building Hammer tool that activates build mode when equipped
/// Add this component to the Player GameObject
/// </summary>
public class BuildingHammer : MonoBehaviour
{
    [Header("References")]
    public BuildingSystem buildingSystem;
    public HeldItemDisplay heldItemDisplay;
    
    [Header("Hammer Item")]
    public ItemData hammerItem; // Assign your hammer ItemData
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private bool isHammerEquipped = false;
    private ItemData currentlyEquippedItem = null;
    
    void Awake()
    {
        if (buildingSystem == null)
        {
            buildingSystem = GetComponent<BuildingSystem>();
        }
        
        if (heldItemDisplay == null)
        {
            heldItemDisplay = GetComponentInChildren<HeldItemDisplay>();
            if (heldItemDisplay == null)
            {
                heldItemDisplay = FindObjectOfType<HeldItemDisplay>();
            }
        }
    }
    
    void Start()
    {
        // Make sure build mode starts deactivated
        if (buildingSystem != null)
        {
            if (buildingSystem.isBuildModeActive)
            {
                buildingSystem.ToggleBuildMode();
            }
        }
    }
    
    void Update()
    {
        CheckEquippedItem();
    }
    
    void CheckEquippedItem()
    {
        if (heldItemDisplay == null || hammerItem == null) return;
        
        // Get currently held item
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        // Check if equipped item changed
        if (heldItem != currentlyEquippedItem)
        {
            currentlyEquippedItem = heldItem;
            
            // Check if it's the hammer
            if (heldItem == hammerItem)
            {
                EquipHammer();
            }
            else
            {
                UnequipHammer();
            }
        }
    }
    
    void EquipHammer()
    {
        if (isHammerEquipped) return;
        
        isHammerEquipped = true;
        
        // Activate build mode
        if (buildingSystem != null && !buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
            
            if (showDebugInfo)
            {
                Debug.Log("ðŸ”¨ Hammer equipped - Build mode ACTIVATED");
            }
        }
    }
    
    void UnequipHammer()
    {
        if (!isHammerEquipped) return;
        
        isHammerEquipped = false;
        
        // Deactivate build mode
        if (buildingSystem != null && buildingSystem.isBuildModeActive)
        {
            buildingSystem.ToggleBuildMode();
            
            if (showDebugInfo)
            {
                Debug.Log("ðŸ”¨ Hammer unequipped - Build mode DEACTIVATED");
            }
        }
    }
    
    // Public method to check if hammer is equipped (useful for other systems)
    public bool IsHammerEquipped()
    {
        return isHammerEquipped;
    }
    
    // Force unequip (e.g., when player dies or enters combat)
    public void ForceUnequip()
    {
        if (heldItemDisplay != null)
        {
            heldItemDisplay.HideItem();
        }
        
        currentlyEquippedItem = null;
        UnequipHammer();
    }
}```

### HeldItemDisplay.cs:

```csharp
using UnityEngine;

public class HeldItemDisplay : MonoBehaviour
{
    [Header("References")]
    public Transform itemHoldPosition; // Position in front of camera
    public Camera playerCamera;
    
    [Header("Settings")]
    public Vector3 holdPositionOffset = new Vector3(0.5f, -0.3f, 0.8f); // Right, Down, Forward
    public Vector3 holdRotationOffset = new Vector3(0f, -90f, 0f);
    public float itemScale = 0.5f;
    
    [Header("Animation")]
    public bool enableBobbing = true;
    public float bobSpeed = 2f;
    public float bobAmount = 0.02f;
    
    private GameObject currentHeldItem;
    private ItemData currentItemData;
    private Vector3 originalPosition;
    
    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        // Create hold position if it doesn't exist
        if (itemHoldPosition == null)
        {
            GameObject holdPosObj = new GameObject("ItemHoldPosition");
            holdPosObj.transform.parent = playerCamera.transform;
            holdPosObj.transform.localPosition = holdPositionOffset;
            holdPosObj.transform.localRotation = Quaternion.Euler(holdRotationOffset);
            itemHoldPosition = holdPosObj.transform;
        }
        
        originalPosition = itemHoldPosition.localPosition;
    }
    
    void Update()
    {
        if (currentHeldItem != null && enableBobbing)
        {
            // Bobbing animation
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            itemHoldPosition.localPosition = originalPosition + new Vector3(0, bobOffset, 0);
        }
    }
    
    public void ShowItem(ItemData itemData)
    {
        if (itemData == null)
        {
            HideItem();
            return;
        }
        
        // If same item, don't recreate
        if (currentItemData == itemData && currentHeldItem != null)
        {
            return;
        }
        
        // Clear previous item
        HideItem();
        
        currentItemData = itemData;
        
        // Spawn the item's world prefab
        if (itemData.worldPrefab != null)
        {
            currentHeldItem = Instantiate(itemData.worldPrefab, itemHoldPosition);
            currentHeldItem.transform.localPosition = Vector3.zero;
            currentHeldItem.transform.localRotation = Quaternion.identity;
            currentHeldItem.transform.localScale = Vector3.one * itemScale;
            
            // Remove WorldItem script FIRST (it has RequireComponent dependencies)
            WorldItem worldItem = currentHeldItem.GetComponent<WorldItem>();
            if (worldItem != null) Destroy(worldItem);
            
            // Now we can safely remove physics components
            Rigidbody rb = currentHeldItem.GetComponent<Rigidbody>();
            if (rb != null) Destroy(rb);
            
            Collider[] colliders = currentHeldItem.GetComponents<Collider>();
            foreach (Collider col in colliders)
            {
                if (col != null) Destroy(col);
            }
            
            // Set layer to avoid raycast issues
            SetLayerRecursively(currentHeldItem, LayerMask.NameToLayer("Ignore Raycast"));
            
            Debug.Log($"Now holding: {itemData.itemName}");
        }
        else
        {
            Debug.LogWarning($"Item {itemData.itemName} has no world prefab to display!");
        }
    }
    
    public void HideItem()
    {
        if (currentHeldItem != null)
        {
            Destroy(currentHeldItem);
            currentHeldItem = null;
        }
        
        currentItemData = null;
    }
    
    public ItemData GetCurrentItem()
    {
        return currentItemData;
    }
    
    public bool IsHoldingItem()
    {
        return currentHeldItem != null;
    }
    
    void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}```

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
}```

### PlayerItemUse.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerItemUse : MonoBehaviour
{
    [Header("References")]
    public InventorySystem inventorySystem;
    public PlayerStats playerStats;
    public HeldItemDisplay heldItemDisplay;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    private ItemData selectedItem; // Track which item is selected in inventory
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }
        
        if (heldItemDisplay == null)
        {
            heldItemDisplay = GetComponentInChildren<HeldItemDisplay>();
            if (heldItemDisplay == null)
            {
                heldItemDisplay = FindObjectOfType<HeldItemDisplay>();
            }
        }
    }
    
    void OnEnable()
    {
        inputActions.Player.Enable();
        inputActions.Player.UseItem.performed += OnUseItemPerformed;
    }
    
    void OnDisable()
    {
        inputActions.Player.UseItem.performed -= OnUseItemPerformed;
        inputActions.Player.Disable();
    }
    
    void OnUseItemPerformed(InputAction.CallbackContext context)
    {
        UseSelectedItem();
    }
    
    public void SetSelectedItem(ItemData item)
    {
        selectedItem = item;
        Debug.Log($"Selected item: {item?.itemName ?? "None"}");
        
        // Update held item display
        if (heldItemDisplay != null)
        {
            heldItemDisplay.ShowItem(item);
        }
    }
    
    public void UseSelectedItem()
    {
        if (selectedItem == null)
        {
            Debug.Log("No item selected!");
            return;
        }
        
        UseItem(selectedItem);
    }
    
    public void UseItem(ItemData item)
    {
        if (item == null || inventorySystem == null) return;
        
        // Check if player has the item
        int itemCount = inventorySystem.GetItemCount(item);
        if (itemCount <= 0)
        {
            Debug.Log($"You don't have any {item.itemName}!");
            return;
        }
        
        // Handle different item types
        if (item.isEdible)
        {
            EatFood(item);
        }
        else
        {
            Debug.Log($"{item.itemName} cannot be used!");
        }
    }
    
    // Replace the EatFood method with this updated version:

    void EatFood(ItemData foodItem)
    {
        if (playerStats == null)
        {
            Debug.LogError("PlayerStats not found!");
            return;
        }
        
        // Check if hunger is already full
        if (playerStats.Hunger >= playerStats.MaxHunger && foodItem.healthRestoreAmount <= 0)
        {
            Debug.Log("You're not hungry!");
            return;
        }
        
        // Restore hunger
        if (foodItem.hungerRestoreAmount > 0)
        {
            playerStats.AddHunger(foodItem.hungerRestoreAmount);
            Debug.Log($"Ate {foodItem.itemName}! Restored {foodItem.hungerRestoreAmount} hunger.");
        }
        
        // Restore health (if applicable)
        if (foodItem.healthRestoreAmount > 0)
        {
            playerStats.Heal(foodItem.healthRestoreAmount);
            Debug.Log($"Healed {foodItem.healthRestoreAmount} HP!");
        }
        
        // Remove one from inventory
        bool removed = inventorySystem.RemoveItem(foodItem, 1);
        
        // FIX: Check if item is now depleted
        if (removed)
        {
            int remainingCount = inventorySystem.GetItemCount(foodItem);
            
            if (remainingCount <= 0)
            {
                // Item depleted - clear from hand
                if (heldItemDisplay != null)
                {
                    heldItemDisplay.HideItem();
                }
                selectedItem = null;
                Debug.Log($"{foodItem.itemName} depleted - removed from hand");
            }
        }
    }
}```

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
}```

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
}```

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
}```

## Tools

### ToolCombatSystem.cs:

```csharp
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles tool-based combat and resource gathering
/// Add to Player GameObject
/// </summary>
public class ToolCombatSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public HeldItemDisplay heldItemDisplay;
    public InventorySystem inventorySystem;
    public PlayerInputActions inputActions;
    
    [Header("Tool Settings")]
    public ToolData currentTool;
    public ToolInstance currentToolInstance; // Track current tool instance
    public ItemData currentToolItem; // Track current item
    public LayerMask hitMask; // What can be hit
    
    [Header("Visual Feedback")]
    public bool showHitMarker = true;
    public float hitMarkerDuration = 0.2f;
    
    [Header("Audio")]
    public AudioSource audioSource;
    
    private float lastAttackTime;
    private bool isToolEquipped = false;
    
    void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (heldItemDisplay == null)
        {
            heldItemDisplay = GetComponentInChildren<HeldItemDisplay>();
        }
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
        
        if (inputActions == null)
        {
            inputActions = new PlayerInputActions();
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }
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
    
    void Update()
    {
        CheckEquippedTool();
    }
    
    void CheckEquippedTool()
    {
        if (heldItemDisplay == null) return;
        
        // Get currently held item
        ItemData heldItem = heldItemDisplay.GetCurrentItem();
        
        // Check if it's a tool
        if (heldItem != null && heldItem.itemType == ItemType.Tool)
        {
            // Find the tool instance in inventory
            ToolInstance toolInstance = FindToolInstanceForItem(heldItem);
            
            if (toolInstance != currentToolInstance)
            {
                currentToolInstance = toolInstance;
                currentTool = toolInstance?.toolData;
                currentToolItem = heldItem;
                isToolEquipped = currentTool != null;
                
                if (isToolEquipped)
                {
                    Debug.Log($"Equipped tool: {currentTool.toolName} ({currentToolInstance.currentDurability}/{currentTool.maxDurability})");
                }
            }
        }
        else
        {
            currentTool = null;
            currentToolInstance = null;
            currentToolItem = null;
            isToolEquipped = false;
        }
    }
    
    ToolInstance FindToolInstanceForItem(ItemData item)
    {
        if (inventorySystem == null) return null;
        
        // Search inventory for this specific item and get its tool instance
        for (int x = 0; x < inventorySystem.width; x++)
        {
            for (int y = 0; y < inventorySystem.height; y++)
            {
                InventorySlotData slotData = inventorySystem.GetSlot(x, y);
                if (slotData != null && slotData.item == item && slotData.toolInstance != null)
                {
                    return slotData.toolInstance;
                }
            }
        }
        
        return null;
    }
    
    ToolData FindToolDataForItem(ItemData item)
    {
        // Direct reference first
        if (item.toolData != null)
        {
            return item.toolData;
        }
        
        // Load all ToolData assets
        ToolData[] allTools = Resources.FindObjectsOfTypeAll<ToolData>();
        
        foreach (ToolData tool in allTools)
        {
            // Match by name
            if (tool.toolName.Equals(item.itemName, System.StringComparison.OrdinalIgnoreCase))
            {
                return tool;
            }
        }
        
        return null;
    }
    
    void OnAttackPerformed(InputAction.CallbackContext context)
    {
        // Add null checks at the start
        if (!isToolEquipped || currentTool == null || currentToolInstance == null)
        {
            return;
        }
        
        // Check if tool is broken
        if (currentToolInstance.IsBroken())
        {
            Debug.Log($"{currentTool.toolName} is broken!");
            DestroyBrokenTool();
            return; // IMPORTANT: Return after destroying
        }
        
        // Check attack cooldown
        float cooldown = 1f / currentTool.attackSpeed;
        if (Time.time < lastAttackTime + cooldown) return;
        
        PerformToolAttack();
        lastAttackTime = Time.time;
    }

    void PerformToolAttack()
    {
        // Add safety check at start
        if (currentTool == null || currentToolInstance == null)
        {
            Debug.LogWarning("PerformToolAttack called but tool is null!");
            return;
        }
        
        // Play swing sound
        if (audioSource != null && currentTool.swingSound != null)
        {
            audioSource.PlayOneShot(currentTool.swingSound);
        }
        
        // Raycast from camera center
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, currentTool.attackRange, hitMask))
        {
            // Calculate damage based on what we hit
            float damage = currentTool.CalculateDamage(hit.collider.gameObject);
            
            // Try to damage the object
            Health targetHealth = hit.collider.GetComponent<Health>();
            
            if (targetHealth != null)
            {
                targetHealth.TakeDamage(damage, transform.position, 0f);
                
                // Play hit sound
                if (audioSource != null && currentTool.hitSound != null)
                {
                    audioSource.PlayOneShot(currentTool.hitSound);
                }
                
                // Show hit marker
                if (showHitMarker)
                {
                    ShowHitMarker(hit.point);
                }
                
                Debug.Log($"Hit {hit.collider.name} for {damage} damage!");
            }
            
            // Reduce tool durability - with null check
            if (currentToolInstance != null)
            {
                currentToolInstance.UseTool();
                
                // Check if tool broke - with null check
                if (currentToolInstance != null && currentToolInstance.IsBroken())
                {
                    Debug.Log($"{currentTool.toolName} broke!");
                    DestroyBrokenTool();
                    return; // IMPORTANT: Return immediately after destroying
                }
            }
        }
        
        // Visual feedback
        Debug.DrawRay(ray.origin, ray.direction * currentTool.attackRange, Color.red, 0.5f);
    }

    void DestroyBrokenTool()
    {
        if (currentToolItem == null || inventorySystem == null)
        {
            Debug.LogWarning("DestroyBrokenTool called but references are null");
            // Still clear local references
            currentTool = null;
            currentToolInstance = null;
            currentToolItem = null;
            isToolEquipped = false;
            return;
        }
        
        Debug.Log($"Destroying broken {currentToolItem.itemName}");
        
        // Clear from hand FIRST (before removing from inventory)
        if (heldItemDisplay != null)
        {
            heldItemDisplay.HideItem();
        }
        
        // Clear references BEFORE removing from inventory
        ItemData itemToRemove = currentToolItem;
        currentTool = null;
        currentToolInstance = null;
        currentToolItem = null;
        isToolEquipped = false;
        
        // Remove from inventory LAST
        inventorySystem.RemoveItem(itemToRemove, 1);
        
        Debug.Log("Tool destroyed successfully");
    }
    
    void ShowHitMarker(Vector3 worldPosition)
    {
        // Simple hit marker - you can enhance this with particles or UI
        Debug.Log($"HIT at {worldPosition}");
        
        // TODO: Instantiate hit effect particle
        // GameObject hitEffect = Instantiate(hitEffectPrefab, worldPosition, Quaternion.identity);
        // Destroy(hitEffect, hitMarkerDuration);
    }
    
    public ToolData GetCurrentTool()
    {
        return currentTool;
    }
    
    public ToolInstance GetCurrentToolInstance()
    {
        return currentToolInstance;
    }
    
    public bool IsToolEquipped()
    {
        return isToolEquipped;
    }
    
    void OnDrawGizmosSelected()
    {
        if (currentTool == null || playerCamera == null) return;
        
        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(playerCamera.transform.position, playerCamera.transform.forward * currentTool.attackRange);
    }
}```

### ToolData.cs:

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "Inventory/Tool Data")]
public class ToolData : ScriptableObject
{
    [Header("Tool Info")]
    public string toolName = "Axe";
    public Sprite icon;
    [TextArea(2, 4)]
    public string description = "A tool for gathering resources";
    
    [Header("Base Stats")]
    public float baseDamage = 10f;
    public float attackSpeed = 1f; // Attacks per second
    public float attackRange = 3f;
    
    [Header("Tool Type")]
    public ToolType toolType = ToolType.Axe;
    
    [Header("Durability")]
    public bool hasDurability = true;
    public int maxDurability = 100;
    [HideInInspector]
    public int currentDurability;
    
    [Header("Bonus Damage Targets")]
    [Tooltip("Drag prefabs/objects here that this tool does extra damage to")]
    public BonusDamageTarget[] bonusDamageTargets;
    
    [Header("Visual/Audio")]
    public GameObject worldPrefab; // 3D model for holding
    public AudioClip swingSound;
    public AudioClip hitSound;
    
    void OnEnable()
    {
        // Initialize durability
        currentDurability = maxDurability;
    }
    
    /// <summary>
    /// Calculate damage against a specific target
    /// </summary>
    public float CalculateDamage(GameObject target)
    {
        if (target == null) return baseDamage;
        
        float totalDamage = baseDamage;
        
        // Check if target matches any bonus damage targets
        foreach (BonusDamageTarget bonusTarget in bonusDamageTargets)
        {
            if (bonusTarget.targetPrefab == null) continue;
            
            // Check if the hit object matches the bonus target prefab
            // This works for instantiated prefabs
            if (IsPrefabMatch(target, bonusTarget.targetPrefab))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                Debug.Log($"{toolName} hit {target.name}: Base({baseDamage}) + Bonus({bonusTarget.bonusDamage}) = {totalDamage}");
                return totalDamage;
            }
            
            // Also check by tag if specified
            if (!string.IsNullOrEmpty(bonusTarget.targetTag) && target.CompareTag(bonusTarget.targetTag))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                Debug.Log($"{toolName} hit {target.name} by tag: Base({baseDamage}) + Bonus({bonusTarget.bonusDamage}) = {totalDamage}");
                return totalDamage;
            }
        }
        
        Debug.Log($"{toolName} hit {target.name}: {baseDamage} (no bonus)");
        return totalDamage;
    }
    
    /// <summary>
    /// Check if a GameObject instance matches a prefab
    /// </summary>
    bool IsPrefabMatch(GameObject instance, GameObject prefab)
    {
        #if UNITY_EDITOR
        // In editor, check prefab connection
        GameObject prefabRoot = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (prefabRoot == prefab) return true;
        #endif
        
        // Fallback: Check by name (less reliable but works at runtime)
        string instanceName = instance.name.Replace("(Clone)", "").Trim();
        string prefabName = prefab.name;
        
        return instanceName == prefabName;
    }
    
    /// <summary>
    /// Reduce durability after use
    /// </summary>
    public void UseTool()
    {
        if (!hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            Debug.Log($"{toolName} broke!");
        }
    }
    
    /// <summary>
    /// Repair the tool
    /// </summary>
    public void Repair(int amount)
    {
        if (!hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, maxDurability);
    }
    
    /// <summary>
    /// Check if tool is broken
    /// </summary>
    public bool IsBroken()
    {
        return hasDurability && currentDurability <= 0;
    }
    
    /// <summary>
    /// Get durability as percentage
    /// </summary>
    public float GetDurabilityPercent()
    {
        if (!hasDurability) return 1f;
        return (float)currentDurability / maxDurability;
    }
}

[System.Serializable]
public class BonusDamageTarget
{
    [Tooltip("The prefab this tool does bonus damage to (e.g., Tree prefab)")]
    public GameObject targetPrefab;
    
    [Tooltip("Or match by tag (optional, alternative to prefab)")]
    public string targetTag = "";
    
    [Tooltip("Extra damage dealt to this target")]
    public float bonusDamage = 30f;
    
    [Tooltip("Name/description for organization")]
    public string targetName = "Tree";
}

public enum ToolType
{
    Axe,
    Pickaxe,
    Hammer,
    Shovel,
    Sword,
    Scythe
}```

### ToolInstance.cs:

```csharp
using UnityEngine;

/// <summary>
/// Represents a single instance of a tool with its own durability
/// Store this alongside ItemData in inventory slots
/// </summary>
[System.Serializable]
public class ToolInstance
{
    public ToolData toolData;
    public int currentDurability;
    
    public ToolInstance(ToolData data)
    {
        toolData = data;
        currentDurability = data.maxDurability;
    }
    
    /// <summary>
    /// Use the tool, reducing durability
    /// </summary>
    public void UseTool()
    {
        if (!toolData.hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            Debug.Log($"{toolData.toolName} broke!");
        }
    }
    
    /// <summary>
    /// Repair the tool
    /// </summary>
    public void Repair(int amount)
    {
        if (!toolData.hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, toolData.maxDurability);
    }
    
    /// <summary>
    /// Check if tool is broken
    /// </summary>
    public bool IsBroken()
    {
        return toolData.hasDurability && currentDurability <= 0;
    }
    
    /// <summary>
    /// Get durability as percentage
    /// </summary>
    public float GetDurabilityPercent()
    {
        if (!toolData.hasDurability) return 1f;
        return (float)currentDurability / toolData.maxDurability;
    }
}```

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
}```

## World

### AnimalSpawner.cs:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class AnimalSpawner : MonoBehaviour
{
    [System.Serializable]
    public class AnimalSpawnData
    {
        public GameObject animalPrefab;
        public BiomeData biome;
        [Range(0f, 1f)]
        public float spawnChance = 0.5f; // Chance to spawn in valid location
        public int minGroupSize = 1;
        public int maxGroupSize = 3;
        public float groupSpreadRadius = 5f;
    }
    
    [Header("Spawn Settings")]
    public AnimalSpawnData[] animalSpawns;
    public float spawnCheckInterval = 5f; // Check every 5 seconds
    public float despawnDistance = 100f; // Remove animals this far away
    public float spawnDistance = 80f; // Spawn animals at this distance
    
    [Header("Population Limits")]
    public int maxAnimalsPerBiomeUnit = 5; // Max animals per 1000 square units of biome
    public int globalMaxAnimals = 50; // Absolute max animals in world
    
    [Header("References")]
    public Transform player;
    public TerrainGenerator terrainGenerator;
    public LayerMask groundMask;
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private List<GameObject> spawnedAnimals = new List<GameObject>();
    private float nextSpawnCheckTime = 0f;
    private Dictionary<BiomeData, int> biomeAnimalCounts = new Dictionary<BiomeData, int>();
    private Dictionary<BiomeData, float> biomeAreas = new Dictionary<BiomeData, float>();
    
    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<TerrainGenerator>();
        }
        
        // Calculate biome areas
        CalculateBiomeAreas();
        
        // Initial spawn
        SpawnAnimalsAroundPlayer();
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnCheckTime)
        {
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
            
            CleanupDistantAnimals();
            SpawnAnimalsAroundPlayer();
        }
    }
    
    void CalculateBiomeAreas()
    {
        if (terrainGenerator == null) return;
        
        int width = terrainGenerator.mapWidth;
        int height = terrainGenerator.mapHeight;
        
        // Sample the terrain to estimate biome areas
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(
            width, height, 
            terrainGenerator.scale * 2, 
            terrainGenerator.seed + 1, 
            3, 0.5f, 2f, 
            terrainGenerator.offset
        );
        
        Dictionary<BiomeData, int> biomeCells = new Dictionary<BiomeData, int>();
        
        for (int y = 0; y < height; y += 5) // Sample every 5 cells for performance
        {
            for (int x = 0; x < width; x += 5)
            {
                BiomeData biome = GetBiomeAtMoisture(moistureMap[x, y]);
                if (biome != null)
                {
                    if (!biomeCells.ContainsKey(biome))
                        biomeCells[biome] = 0;
                    
                    biomeCells[biome]++;
                }
            }
        }
        
        // Convert cell counts to approximate areas
        float cellArea = 25f; // Each sampled cell represents ~5x5 = 25 square units
        foreach (var kvp in biomeCells)
        {
            biomeAreas[kvp.Key] = kvp.Value * cellArea;
            biomeAnimalCounts[kvp.Key] = 0;
            
            if (showDebugInfo)
            {
                Debug.Log($"Biome {kvp.Key.biomeName}: Area = {biomeAreas[kvp.Key]} unitsÂ²");
            }
        }
    }
    
    void SpawnAnimalsAroundPlayer()
    {
        if (player == null) return;
        
        // Remove null references
        spawnedAnimals.RemoveAll(a => a == null);
        
        // Check if we're at global limit
        if (spawnedAnimals.Count >= globalMaxAnimals)
        {
            if (showDebugInfo)
                Debug.Log("Global animal limit reached");
            return;
        }
        
        // Try to spawn animals in a ring around the player
        int spawnAttempts = 10;
        
        for (int i = 0; i < spawnAttempts; i++)
        {
            // Random position around player
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(spawnDistance * 0.7f, spawnDistance);
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x * distance, 0, randomCircle.y * distance);
            
            // Get biome at this position
            BiomeData biome = GetBiomeAtWorldPosition(spawnPos);
            if (biome == null) continue;
            
            // Check biome-specific limit
            int maxForBiome = GetMaxAnimalsForBiome(biome);
            int currentInBiome = GetAnimalCountInBiome(biome);
            
            if (currentInBiome >= maxForBiome)
            {
                if (showDebugInfo && i == 0)
                    Debug.Log($"Biome {biome.biomeName} at animal limit ({currentInBiome}/{maxForBiome})");
                continue;
            }
            
            // Try to spawn animal for this biome
            TrySpawnAnimalGroup(spawnPos, biome);
        }
    }
    
    void TrySpawnAnimalGroup(Vector3 position, BiomeData biome)
    {
        // Find valid animal spawn data for this biome
        List<AnimalSpawnData> validSpawns = new List<AnimalSpawnData>();
        
        if (showDebugInfo)
        {
            Debug.Log($"=== Trying to spawn in biome: {biome?.biomeName ?? "NULL"} ===");
        }
        
        foreach (AnimalSpawnData spawnData in animalSpawns)
        {
            if (showDebugInfo)
            {
                Debug.Log($"Checking spawn: {spawnData.animalPrefab?.name ?? "NULL"} for biome {spawnData.biome?.biomeName ?? "NULL"}");
            }
            
            if (spawnData.biome == biome && Random.value <= spawnData.spawnChance)
            {
                validSpawns.Add(spawnData);
                if (showDebugInfo)
                {
                    Debug.Log($"âœ“ Valid spawn found: {spawnData.animalPrefab.name}");
                }
            }
        }
        
        if (validSpawns.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"âœ— No valid spawns for biome: {biome?.biomeName}");
            }
            return;
        }
        
        // Pick random animal type
        AnimalSpawnData selectedSpawn = validSpawns[Random.Range(0, validSpawns.Count)];
        
        // Spawn a group
        int groupSize = Random.Range(selectedSpawn.minGroupSize, selectedSpawn.maxGroupSize + 1);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawning group of {groupSize} {selectedSpawn.animalPrefab.name}");
        }
        
        for (int i = 0; i < groupSize; i++)
        {
            // Check global limit
            if (spawnedAnimals.Count >= globalMaxAnimals) break;
            
            // Random offset for group spread
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * selectedSpawn.groupSpreadRadius;
                offset = new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            Vector3 spawnPos = position + offset;
            
            // Raycast to find ground
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
            {
                GameObject animal = Instantiate(selectedSpawn.animalPrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                spawnedAnimals.Add(animal);
                
                // Track biome count
                if (biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]++;
                }
                
                if (showDebugInfo && i == 0)
                {
                    Debug.Log($"âœ“ Spawned {selectedSpawn.animalPrefab.name} at {hit.point} in {biome.biomeName}");
                }
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning($"âœ— Failed to find ground at {spawnPos}");
            }
        }
    }
    
    void CleanupDistantAnimals()
    {
        if (player == null) return;
        
        for (int i = spawnedAnimals.Count - 1; i >= 0; i--)
        {
            if (spawnedAnimals[i] == null)
            {
                spawnedAnimals.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(player.position, spawnedAnimals[i].transform.position);
            
            if (distance > despawnDistance)
            {
                // Decrease biome count
                Vector3 animalPos = spawnedAnimals[i].transform.position;
                BiomeData biome = GetBiomeAtWorldPosition(animalPos);
                if (biome != null && biomeAnimalCounts.ContainsKey(biome))
                {
                    biomeAnimalCounts[biome]--;
                }
                
                Destroy(spawnedAnimals[i]);
                spawnedAnimals.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Despawned animal (too far)");
            }
        }
    }
    
    int GetMaxAnimalsForBiome(BiomeData biome)
    {
        if (!biomeAreas.ContainsKey(biome)) return 5;
        
        float area = biomeAreas[biome];
        int max = Mathf.CeilToInt(area / 1000f * maxAnimalsPerBiomeUnit);
        return Mathf.Max(max, 3); // Minimum 3 per biome
    }
    
    int GetAnimalCountInBiome(BiomeData biome)
    {
        if (!biomeAnimalCounts.ContainsKey(biome)) return 0;
        return biomeAnimalCounts[biome];
    }
    
    BiomeData GetBiomeAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return null;
        
        // Convert world position to terrain coordinates
        float moistureValue = GetMoistureAtWorldPosition(worldPos);
        BiomeData biome = GetBiomeAtMoisture(moistureValue);
        
        if (showDebugInfo && Random.value < 0.01f) // Log occasionally to avoid spam
        {
            Debug.Log($"Position {worldPos} -> Moisture: {moistureValue:F2} -> Biome: {biome?.biomeName ?? "NULL"}");
        }
        
        return biome;
    }
    
    float GetMoistureAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return 0.5f;
        
        // Sample moisture noise at this position - MUST match TerrainGenerator's moisture calculation
        float scale = terrainGenerator.scale * 2;
        
        // Account for seed offset
        System.Random prng = new System.Random(terrainGenerator.seed + 1);
        float offsetX = prng.Next(-100000, 100000) + terrainGenerator.offset.x;
        float offsetY = prng.Next(-100000, 100000) + terrainGenerator.offset.y;
        
        float halfWidth = terrainGenerator.mapWidth / 2f;
        float halfHeight = terrainGenerator.mapHeight / 2f;
        
        float sampleX = (worldPos.x - halfWidth) / scale + offsetX;
        float sampleY = (worldPos.z - halfHeight) / scale + offsetY;
        
        return Mathf.PerlinNoise(sampleX, sampleY);
    }
    
    BiomeData GetBiomeAtMoisture(float moisture)
    {
        if (terrainGenerator == null || terrainGenerator.biomes == null) return null;
        
        foreach (BiomeData biome in terrainGenerator.biomes)
        {
            if (moisture >= biome.moistureMin && moisture <= biome.moistureMax)
            {
                return biome;
            }
        }
        
        if (showDebugInfo)
        {
            Debug.LogWarning($"No biome found for moisture {moisture:F2}");
        }
        
        return terrainGenerator.biomes.Length > 0 ? terrainGenerator.biomes[0] : null;
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // Draw spawn distance
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, spawnDistance);
        
        // Draw despawn distance
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
    }
}```

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

    [Header("Grass")]
    public GameObject grassPrefab; // A simple grass blade mesh
    [Range(0f, 1f)]
    public float grassDensity = 0.3f;
    public int grassPerPatch = 5; // How many grass blades per spawn point
    public float grassSpreadRadius = 0.5f; // Spread within each patch
}```

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
}```

### EnemySpawner.cs:

```csharp
using UnityEngine;
using System.Collections.Generic;

public class EnemySpawner : MonoBehaviour
{
    [System.Serializable]
    public class EnemySpawnData
    {
        public GameObject enemyPrefab;
        public BiomeData biome;
        [Range(0f, 1f)]
        public float spawnChance = 0.5f;
        public int minGroupSize = 1;
        public int maxGroupSize = 2;
        public float groupSpreadRadius = 3f;
    }
    
    [Header("Spawn Settings")]
    public EnemySpawnData[] enemySpawns;
    public float spawnCheckInterval = 10f; // Check less frequently than animals
    public float despawnDistance = 120f;
    public float minSpawnDistance = 50f; // Don't spawn too close to player
    public float maxSpawnDistance = 100f; // Don't spawn too far
    
    [Header("Night Spawning")]
    public DayNightCycle dayNightCycle;
    public bool onlySpawnAtNight = true;
    [Range(0f, 1f)]
    public float nightStartTime = 0.75f; // When night begins (0.75 = 6pm)
    [Range(0f, 1f)]
    public float nightEndTime = 0.25f; // When night ends (0.25 = 6am)
    
    [Header("Population Limits")]
    public int maxEnemiesPerBiomeUnit = 3;
    public int globalMaxEnemies = 30;
    
    [Header("References")]
    public Transform player;
    public TerrainGenerator terrainGenerator;
    public LayerMask groundMask;
    public GameProgressionSystem progressionSystem; // NEW
    
    [Header("Debug")]
    public bool showDebugInfo = true;
    
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    private float nextSpawnCheckTime = 0f;
    private Dictionary<BiomeData, int> biomeEnemyCounts = new Dictionary<BiomeData, int>();
    private Dictionary<BiomeData, float> biomeAreas = new Dictionary<BiomeData, float>();
    private bool wasNight = false;
    
    void Start()
    {
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        if (terrainGenerator == null)
        {
            terrainGenerator = FindObjectOfType<TerrainGenerator>();
        }
        
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<DayNightCycle>();
        }
        
        if (progressionSystem == null)
        {
            progressionSystem = FindObjectOfType<GameProgressionSystem>();
        }
        
        CalculateBiomeAreas();
    }
    
    void Update()
    {
        if (Time.time >= nextSpawnCheckTime)
        {
            nextSpawnCheckTime = Time.time + spawnCheckInterval;
            
            CleanupDistantEnemies();
            
            if (ShouldSpawn())
            {
                SpawnEnemiesAroundPlayer();
            }
        }
        
        // Check for night transition
        bool isNight = IsNightTime();
        if (isNight && !wasNight)
        {
            // Just became night
            if (showDebugInfo)
            {
                Debug.Log("Night has fallen - enemies will spawn!");
            }
        }
        else if (!isNight && wasNight)
        {
            // Just became day
            if (showDebugInfo)
            {
                Debug.Log("Day has come - no more enemy spawns");
            }
        }
        wasNight = isNight;
    }
    
    bool ShouldSpawn()
    {
        if (onlySpawnAtNight && !IsNightTime())
        {
            return false;
        }
        
        return true;
    }
    
    bool IsNightTime()
    {
        if (dayNightCycle == null) return true; // Always spawn if no day/night cycle
        
        float timeOfDay = dayNightCycle.GetTimeOfDay();
        
        // Night wraps around midnight (e.g., 0.75 to 1.0, then 0.0 to 0.25)
        if (nightStartTime > nightEndTime)
        {
            return timeOfDay >= nightStartTime || timeOfDay <= nightEndTime;
        }
        else
        {
            return timeOfDay >= nightStartTime && timeOfDay <= nightEndTime;
        }
    }
    
    void CalculateBiomeAreas()
    {
        if (terrainGenerator == null) return;
        
        int width = terrainGenerator.mapWidth;
        int height = terrainGenerator.mapHeight;
        
        float[,] moistureMap = NoiseGenerator.GenerateNoiseMap(
            width, height, 
            terrainGenerator.scale * 2, 
            terrainGenerator.seed + 1, 
            3, 0.5f, 2f, 
            terrainGenerator.offset
        );
        
        Dictionary<BiomeData, int> biomeCells = new Dictionary<BiomeData, int>();
        
        for (int y = 0; y < height; y += 5)
        {
            for (int x = 0; x < width; x += 5)
            {
                BiomeData biome = GetBiomeAtMoisture(moistureMap[x, y]);
                if (biome != null)
                {
                    if (!biomeCells.ContainsKey(biome))
                        biomeCells[biome] = 0;
                    
                    biomeCells[biome]++;
                }
            }
        }
        
        float cellArea = 25f;
        foreach (var kvp in biomeCells)
        {
            biomeAreas[kvp.Key] = kvp.Value * cellArea;
            biomeEnemyCounts[kvp.Key] = 0;
            
            if (showDebugInfo)
            {
                Debug.Log($"Biome {kvp.Key.biomeName}: Area = {biomeAreas[kvp.Key]} unitsÂ²");
            }
        }
    }
    
    void SpawnEnemiesAroundPlayer()
    {
        if (player == null) return;
        
        spawnedEnemies.RemoveAll(e => e == null);
        
        if (spawnedEnemies.Count >= globalMaxEnemies)
        {
            if (showDebugInfo)
                Debug.Log("Global enemy limit reached");
            return;
        }
        
        int spawnAttempts = 10;
        
        for (int i = 0; i < spawnAttempts; i++)
        {
            // Random position in a ring around player (not too close, not too far)
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float distance = Random.Range(minSpawnDistance, maxSpawnDistance);
            Vector3 spawnPos = player.position + new Vector3(randomCircle.x * distance, 0, randomCircle.y * distance);
            
            // Make sure it's not too close to player
            float distToPlayer = Vector3.Distance(spawnPos, player.position);
            if (distToPlayer < minSpawnDistance)
            {
                continue;
            }
            
            BiomeData biome = GetBiomeAtWorldPosition(spawnPos);
            if (biome == null) continue;
            
            int maxForBiome = GetMaxEnemiesForBiome(biome);
            int currentInBiome = GetEnemyCountInBiome(biome);
            
            if (currentInBiome >= maxForBiome)
            {
                continue;
            }
            
            TrySpawnEnemyGroup(spawnPos, biome);
        }
    }
    
    void TrySpawnEnemyGroup(Vector3 position, BiomeData biome)
    {
        List<EnemySpawnData> validSpawns = new List<EnemySpawnData>();
        
        if (showDebugInfo)
        {
            Debug.Log($"=== Trying to spawn enemy in biome: {biome?.biomeName ?? "NULL"} ===");
        }
        
        foreach (EnemySpawnData spawnData in enemySpawns)
        {
            if (spawnData.biome == biome && Random.value <= spawnData.spawnChance)
            {
                validSpawns.Add(spawnData);
                if (showDebugInfo)
                {
                    Debug.Log($"âœ“ Valid enemy spawn found: {spawnData.enemyPrefab.name}");
                }
            }
        }
        
        if (validSpawns.Count == 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"âœ— No valid enemy spawns for biome: {biome?.biomeName}");
            }
            return;
        }
        
        EnemySpawnData selectedSpawn = validSpawns[Random.Range(0, validSpawns.Count)];
        int groupSize = Random.Range(selectedSpawn.minGroupSize, selectedSpawn.maxGroupSize + 1);
        
        if (showDebugInfo)
        {
            Debug.Log($"Spawning group of {groupSize} {selectedSpawn.enemyPrefab.name}");
        }
        
        for (int i = 0; i < groupSize; i++)
        {
            if (spawnedEnemies.Count >= globalMaxEnemies) break;
            
            Vector3 offset = Vector3.zero;
            if (i > 0)
            {
                Vector2 randomOffset = Random.insideUnitCircle * selectedSpawn.groupSpreadRadius;
                offset = new Vector3(randomOffset.x, 0, randomOffset.y);
            }
            
            Vector3 spawnPos = position + offset;
            
            // Final check: not too close to player
            if (Vector3.Distance(spawnPos, player.position) < minSpawnDistance)
            {
                continue;
            }
            
            RaycastHit hit;
            if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f, groundMask))
            {
                GameObject enemy = Instantiate(selectedSpawn.enemyPrefab, hit.point, Quaternion.Euler(0, Random.Range(0, 360), 0));
                
                // Apply difficulty scaling if progression system exists
                if (progressionSystem != null)
                {
                    progressionSystem.ApplyScalingToEnemy(enemy);
                }
                
                // Tag enemy so progression system can find it
                if (!enemy.CompareTag("Enemy"))
                {
                    enemy.tag = "Enemy";
                }
                
                spawnedEnemies.Add(enemy);
                
                if (biomeEnemyCounts.ContainsKey(biome))
                {
                    biomeEnemyCounts[biome]++;
                }
                
                if (showDebugInfo && i == 0)
                {
                    Debug.Log($"âœ“ Spawned {selectedSpawn.enemyPrefab.name} at {hit.point} in {biome.biomeName}");
                }
            }
        }
    }
    
    void CleanupDistantEnemies()
    {
        if (player == null) return;
        
        for (int i = spawnedEnemies.Count - 1; i >= 0; i--)
        {
            if (spawnedEnemies[i] == null)
            {
                spawnedEnemies.RemoveAt(i);
                continue;
            }
            
            float distance = Vector3.Distance(player.position, spawnedEnemies[i].transform.position);
            
            if (distance > despawnDistance)
            {
                Vector3 enemyPos = spawnedEnemies[i].transform.position;
                BiomeData biome = GetBiomeAtWorldPosition(enemyPos);
                if (biome != null && biomeEnemyCounts.ContainsKey(biome))
                {
                    biomeEnemyCounts[biome]--;
                }
                
                Destroy(spawnedEnemies[i]);
                spawnedEnemies.RemoveAt(i);
                
                if (showDebugInfo)
                    Debug.Log($"Despawned enemy (too far)");
            }
        }
    }
    
    int GetMaxEnemiesForBiome(BiomeData biome)
    {
        if (!biomeAreas.ContainsKey(biome)) return 3;
        
        float area = biomeAreas[biome];
        int max = Mathf.CeilToInt(area / 1000f * maxEnemiesPerBiomeUnit);
        return Mathf.Max(max, 2);
    }
    
    int GetEnemyCountInBiome(BiomeData biome)
    {
        if (!biomeEnemyCounts.ContainsKey(biome)) return 0;
        return biomeEnemyCounts[biome];
    }
    
    BiomeData GetBiomeAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return null;
        
        float moistureValue = GetMoistureAtWorldPosition(worldPos);
        BiomeData biome = GetBiomeAtMoisture(moistureValue);
        
        return biome;
    }
    
    float GetMoistureAtWorldPosition(Vector3 worldPos)
    {
        if (terrainGenerator == null) return 0.5f;
        
        float scale = terrainGenerator.scale * 2;
        
        System.Random prng = new System.Random(terrainGenerator.seed + 1);
        float offsetX = prng.Next(-100000, 100000) + terrainGenerator.offset.x;
        float offsetY = prng.Next(-100000, 100000) + terrainGenerator.offset.y;
        
        float halfWidth = terrainGenerator.mapWidth / 2f;
        float halfHeight = terrainGenerator.mapHeight / 2f;
        
        float sampleX = (worldPos.x - halfWidth) / scale + offsetX;
        float sampleY = (worldPos.z - halfHeight) / scale + offsetY;
        
        return Mathf.PerlinNoise(sampleX, sampleY);
    }
    
    BiomeData GetBiomeAtMoisture(float moisture)
    {
        if (terrainGenerator == null || terrainGenerator.biomes == null) return null;
        
        foreach (BiomeData biome in terrainGenerator.biomes)
        {
            if (moisture >= biome.moistureMin && moisture <= biome.moistureMax)
            {
                return biome;
            }
        }
        
        return terrainGenerator.biomes.Length > 0 ? terrainGenerator.biomes[0] : null;
    }
    
    // Force cleanup all enemies (useful for day transition)
    public void DespawnAllEnemies()
    {
        if (showDebugInfo)
        {
            Debug.Log($"Despawning all {spawnedEnemies.Count} enemies");
        }
        
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        
        spawnedEnemies.Clear();
        
        foreach (var key in biomeEnemyCounts.Keys)
        {
            biomeEnemyCounts[key] = 0;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (player == null) return;
        
        // Draw min spawn distance (safe zone)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, minSpawnDistance);
        
        // Draw max spawn distance
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(player.position, maxSpawnDistance);
        
        // Draw despawn distance
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(player.position, despawnDistance);
    }
    
    void OnGUI()
    {
        if (showDebugInfo)
        {
            GUI.Label(new Rect(10, 100, 300, 20), $"Enemies: {spawnedEnemies.Count}/{globalMaxEnemies}");
            GUI.Label(new Rect(10, 120, 300, 20), $"Night: {IsNightTime()}");
            
            if (dayNightCycle != null)
            {
                GUI.Label(new Rect(10, 140, 300, 20), $"Time: {dayNightCycle.GetTimeOfDay():F2}");
            }
        }
    }
}```

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
    void SpawnGrass(float[,] moistureMap)
    {
        if (vegetationParent == null) return;
        
        int width = moistureMap.GetLength(0);
        int height = moistureMap.GetLength(1);
        
        // Sample less frequently for grass patches
        for (int y = 0; y < height; y += 1) // Adjust spacing
        {
            for (int x = 0; x < width; x += 1)
            {
                BiomeData biome = GetBiomeAtPosition(moistureMap[x, y]);
                
                if (biome != null && biome.grassPrefab != null)
                {
                    if (Random.value < biome.grassDensity)
                    {
                        SpawnGrassPatch(new Vector2(x, y), biome);
                    }
                }
            }
        }
    }

    void SpawnGrassPatch(Vector2 position, BiomeData biome)
    {
        // Spawn multiple grass blades in a small area
        for (int i = 0; i < biome.grassPerPatch; i++)
        {
            Vector2 offset = Random.insideUnitCircle * biome.grassSpreadRadius;
            Vector3 rayStart = new Vector3(position.x + offset.x, 100f, position.y + offset.y);
            
            RaycastHit hit;
            if (Physics.Raycast(rayStart, Vector3.down, out hit, 200f, groundMask))
            {
                // Align grass with terrain normal
                Quaternion rotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
                
                GameObject grass = Instantiate(biome.grassPrefab, hit.point, rotation, vegetationParent);
                float scale = Random.Range(0.8f, 1.2f);
                grass.transform.localScale = new Vector3(scale, scale, scale);
            }
        }
    }

    System.Collections.IEnumerator SpawnVegetationDelayed(float[,] moistureMap)
    {
        yield return new WaitForFixedUpdate();
        
        SpawnVegetation(moistureMap);
        SpawnGrass(moistureMap);
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
}```


