using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerLook : MonoBehaviour
{
    [Header("Look Settings")]
    public float mouseSensitivity = SettingsData.mouseSensitivity;
    public Transform playerBody;
    
    [Header("Input")]
    public PlayerInputActions playerInputActions;
    
    private float xRotation = 0f;
    private Vector2 lookInput;
    
    void Awake()
    {
        playerInputActions = new PlayerInputActions();
    }
    
    void OnEnable()
    {
        playerInputActions.Player.Enable();
        
        playerInputActions.Player.Look.performed += OnLookPerformed;
        playerInputActions.Player.Look.canceled += OnLookCanceled;
    }
    
    void OnDisable()
    {
        playerInputActions.Player.Look.performed -= OnLookPerformed;
        playerInputActions.Player.Look.canceled -= OnLookCanceled;
        
        playerInputActions.Player.Disable();
    }
    
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        float mouseX = lookInput.x * mouseSensitivity * Time.deltaTime;
        float mouseY = lookInput.y * mouseSensitivity * Time.deltaTime;
        
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        
        playerBody.Rotate(Vector3.up * mouseX);
    }
    
    private void OnLookPerformed(InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }
    
    private void OnLookCanceled(InputAction.CallbackContext context)
    {
        lookInput = Vector2.zero;
    }
}