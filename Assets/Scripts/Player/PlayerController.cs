using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float gravity = -9.81f;
    public float jumpHeight = 2f;
    
    [Header("Camera")]
    public Transform playerCamera;
    public float mouseSensitivity = 2f;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;
    
    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundDistance = 0.4f;
    public LayerMask groundMask;
    
    [Header("References")]
    public InventoryUI inventoryUI; // Reference to check if inventory is open
    
    [Header("Input")]
    public PlayerInputActions playerInputActions;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    
    void Awake()
    {
        // Initialize input actions
        playerInputActions = new PlayerInputActions();
        
        // Get camera reference if not set
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
        
        // Find InventoryUI if not assigned
        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
            if (inventoryUI == null && showDebugLogs)
            {
                Debug.LogError("[PlayerController] InventoryUI not found!");
            }
        }
    }
    
    void OnEnable()
    {
        // Enable input actions
        playerInputActions.Player.Enable();
    }
    
    void OnDisable()
    {
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
        // Check if player is frozen (inventory open)
        bool isPlayerFrozen = IsPlayerFrozen();
        
        // Always apply gravity
        CheckGround();
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
        // Only handle movement and camera if NOT frozen
        if (!isPlayerFrozen)
        {
            HandleMovement();
            HandleCamera();
            HandleJump();
        }
    }
    
    bool IsPlayerFrozen()
    {
        // Check if inventory is open
        if (inventoryUI != null && inventoryUI.IsInventoryOpen())
        {
            return true;
        }
        
        // Check CursorManager
        if (CursorManager.Instance != null && !CursorManager.Instance.IsLocked())
        {
            return true;
        }
        
        return false;
    }
    
    void CheckGround()
    {
        // Ground check
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    
    void HandleMovement()
    {
        // Read movement input directly
        Vector2 moveInput = playerInputActions.Player.Movement.ReadValue<Vector2>();
        
        // Movement
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }
    
    void HandleCamera()
    {
        if (playerCamera == null) return;
        
        // Read look input directly - this should be zero if we're frozen
        Vector2 lookInput = playerInputActions.Player.Look.ReadValue<Vector2>();
        
        // Extra safety: If somehow we got here while frozen, don't rotate
        if (IsPlayerFrozen())
        {
            if (showDebugLogs && lookInput.magnitude > 0.01f)
            {
                Debug.LogWarning("[PlayerController] Look input detected while frozen! Ignoring.");
            }
            return;
        }
        
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        
        // Vertical camera rotation (pitch)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        // Horizontal player rotation (yaw)
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleJump()
    {
        // Read jump input
        if (playerInputActions.Player.Jump.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (showDebugLogs) Debug.Log("Jump performed!");
        }
    }
}