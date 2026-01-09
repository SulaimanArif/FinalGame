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
    public float gravity = -9.81f; 
    
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
    public bool useCreatureMoverAnimations = false; 
    
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
    private static readonly int IsGrazingHash = Animator.StringToHash("IsGrazing");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    
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
    
    private Vector3 spawnPoint;
    private Vector3 wanderTarget;
    private float stateTimer;
    private Vector3 verticalVelocity; // For gravity
    
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
            useCreatureMoverAnimations = true; 
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
        
        if (characterController != null && !useCreatureMover)
        {
            if (characterController.isGrounded && verticalVelocity.y < 0)
            {
                verticalVelocity.y = -2f;
            }
            else
            {
                verticalVelocity.y += gravity * Time.deltaTime;
            }
            
            characterController.Move(verticalVelocity * Time.deltaTime);
        }
        
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (isFleeing)
        {
            fleeTimer -= Time.deltaTime;
            if (fleeTimer <= 0f || distanceToPlayer >= fleeDistance)
            {
                isFleeing = false;
            }
        }
        
        if (aggroType == AggroType.Passive && isFleeing)
        {
            SetState(AIState.Fleeing);
            FleeFromPlayer();
            UpdateAnimator(distanceToPlayer);
            return;
        }
        
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
        if (currentState == AIState.Wandering)
        {
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
            SetState(AIState.Wandering);
            
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
        }
    }
    
    void UpdateAnimator(float distanceToPlayer)
    {
        if (!useAnimations || animator == null) return;
        
        if (useCreatureMoverAnimations)
        {
            float vert = 0f;
            float state = 0f;
            
            switch (currentState)
            {
                case AIState.Idle:
                case AIState.Grazing:
                    vert = 0f;
                    state = 0f;
                    break;
                    
                case AIState.Wandering:
                    vert = 0.5f;
                    state = 0f;
                    break;
                    
                case AIState.Chasing:
                case AIState.Fleeing:
                    vert = 1f;
                    state = 1f;
                    break;
                    
                case AIState.Attacking:
                    vert = 0f;
                    state = 0f;
                    break;
            }
            
            animator.SetFloat(VertHash, vert);
            animator.SetFloat(StateHash, state);
   
            SetAnimatorBoolIfExists(IsGrazingHash, currentState == AIState.Grazing);
        }
        else
        {
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
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
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
}