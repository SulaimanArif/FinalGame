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