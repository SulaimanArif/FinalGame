using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }
    
    [Header("Settings")]
    
    public bool lockOnStart = true;
    
    [Header("Debug")]
    public bool showDebugLogs = true;
    
    private bool isLocked = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showDebugLogs)
            {
                Debug.Log("[CursorManager] Instance created");
            }
        }
        else
        {
            if (showDebugLogs)
            {
                Debug.Log("[CursorManager] Instance created");
            }
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        if (lockOnStart)
        {
            LockCursor();
        }
    }
    
    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isLocked = true;
        
        if (showDebugLogs)
        {
            Debug.Log("[CursorManager] Cursor LOCKED - Gameplay mode");
        }
    }
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isLocked = false;
        
        if (showDebugLogs)
        {
            Debug.Log("[CursorManager] Cursor UNLOCKED - UI mode");
        }
    }
    
    public void ToggleCursor()
    {
        if (isLocked)
        {
            UnlockCursor();
        }
        else
        {
            LockCursor();
        }
    }
    
    public bool IsLocked()
    {
        return isLocked;
    }
    
    void Update()
    {

    }
}