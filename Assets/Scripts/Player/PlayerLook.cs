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