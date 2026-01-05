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