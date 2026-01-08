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
    public InventoryUI inventoryUI; 
    
    [Header("Input")]
    public PlayerInputActions playerInputActions;
    
    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float xRotation = 0f;
    
    void Awake()
    {
        playerInputActions = new PlayerInputActions();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main.transform;
        }
        
        if (inventoryUI == null)
        {
            inventoryUI = FindObjectOfType<InventoryUI>();
        }
    }
    
    void OnEnable()
    {
        playerInputActions.Player.Enable();
    }
    
    void OnDisable()
    {
        playerInputActions.Player.Disable();
    }
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        
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
        bool isPlayerFrozen = IsPlayerFrozen();
        
        CheckGround();
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
        
        if (!isPlayerFrozen)
        {
            HandleMovement();
            HandleCamera();
            HandleJump();
        }
    }
    
    bool IsPlayerFrozen()
    {
        if (inventoryUI != null && inventoryUI.IsInventoryOpen())
        {
            return true;
        }
        
        if (CursorManager.Instance != null && !CursorManager.Instance.IsLocked())
        {
            return true;
        }
        
        return false;
    }
    
    void CheckGround()
    {
        isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
        
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }
    }
    
    void HandleMovement()
    {
        Vector2 moveInput = playerInputActions.Player.Movement.ReadValue<Vector2>();
        
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        controller.Move(move * walkSpeed * Time.deltaTime);
    }
    
    void HandleCamera()
    {
        if (playerCamera == null) return;
        
        Vector2 lookInput = playerInputActions.Player.Look.ReadValue<Vector2>();
        
        if (IsPlayerFrozen())
        {
            return;
        }
        
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);
        playerCamera.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        transform.Rotate(Vector3.up * mouseX);
    }
    
    void HandleJump()
    {
        if (playerInputActions.Player.Jump.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }
}