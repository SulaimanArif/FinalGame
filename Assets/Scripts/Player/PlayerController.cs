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