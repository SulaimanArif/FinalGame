using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }
    
    [Header("Settings")]
    public bool lockOnStart = true;
    
    private bool isLocked = false;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
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
    }
    
    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isLocked = false;
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