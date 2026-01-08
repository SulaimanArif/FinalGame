using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class BuildingSystem : MonoBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public InventorySystem inventorySystem;
    public LayerMask placementMask; 
    public LayerMask buildingMask; 
    
    [Header("Build Mode")]
    public bool isBuildModeActive = false;
    private InputAction toggleBuildModeAction;
    private InputAction rotateAction;
    private InputAction placeAction;
    public float maxPlacementDistance = 10f;
    
    [Header("Available Buildings")]
    public BuildingData[] availableBuildings;
    
    [Header("Materials")]
    public Material validPlacementMaterial;
    public Material invalidPlacementMaterial;
    
    [Header("Input")]
    public PlayerInputActions inputActions;
    
    private BuildingData currentBuilding;
    private GameObject previewObject;
    private Vector3 currentPlacementPosition;
    private Quaternion currentPlacementRotation;
    private bool canPlace = false;
    private float currentRotation = 0f;
    private List<GameObject> placedBuildings = new List<GameObject>();
    
    void Awake()
    {
        inputActions = new PlayerInputActions();
        
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
        
        if (inventorySystem == null)
        {
            inventorySystem = GetComponent<InventorySystem>();
        }
    }
    
    void OnEnable()
    {
        inputActions ??= new PlayerInputActions();

        toggleBuildModeAction = inputActions.Player.ToggleBuildMode;
        rotateAction = inputActions.Player.RotateBuilding;
        placeAction = inputActions.Player.UseItem;

        toggleBuildModeAction.performed += OnToggleBuildMode;
        rotateAction.performed += OnRotateBuilding;
        placeAction.performed += OnPlaceBuilding;

        inputActions.Player.Enable();
    }

    void OnDisable()
    {
        toggleBuildModeAction.performed -= OnToggleBuildMode;
        rotateAction.performed -= OnRotateBuilding;
        placeAction.performed -= OnPlaceBuilding;

        inputActions.Player.Disable();
    }

    void OnToggleBuildMode(InputAction.CallbackContext ctx)
    {
        ToggleBuildMode();
    }

    void OnRotateBuilding(InputAction.CallbackContext ctx)
    {
        if (!isBuildModeActive || currentBuilding == null || !currentBuilding.canRotate)
            return;

        RotateBuilding();
    }
    
    void Update()
    {
        if (!isBuildModeActive) return;

        if (currentBuilding != null)
        {
            UpdateBuildingPreview();
        }
    }
    
    public void ToggleBuildMode()
    {
        isBuildModeActive = !isBuildModeActive;
        
        if (isBuildModeActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (availableBuildings.Length > 0)
            {
                SelectBuilding(0);
            }
        }
        else
        {
            ClearPreview();
        }
    }
    
    public void SelectBuilding(int index)
    {
        if (index < 0 || index >= availableBuildings.Length) return;
        
        currentBuilding = availableBuildings[index];
        currentRotation = 0f;
        CreatePreview();
        
    }
    
    void CreatePreview()
    {
        ClearPreview();
        
        if (currentBuilding == null || currentBuilding.prefab == null) return;
        
        previewObject = Instantiate(currentBuilding.prefab);
        previewObject.name = "BuildingPreview";
        
        foreach (Collider col in previewObject.GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
        
        foreach (Renderer rend in previewObject.GetComponentsInChildren<Renderer>())
        {
            Material[] mats = new Material[rend.materials.Length];
            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = validPlacementMaterial;
            }
            rend.materials = mats;
        }
    }
    
    void ClearPreview()
    {
        if (previewObject != null)
        {
            Destroy(previewObject);
            previewObject = null;
        }
    }
    
    void UpdateBuildingPreview()
    {
        RaycastHit hit;
        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        
        if (Physics.Raycast(ray, out hit, maxPlacementDistance, placementMask | buildingMask))
        {
            if (previewObject != null && !previewObject.activeSelf)
            {
                previewObject.SetActive(true);
            }
            
            Vector3 snapPosition = CalculateSnapPosition(hit.point, hit.normal);
            currentPlacementPosition = snapPosition;
            currentPlacementRotation = Quaternion.Euler(0, currentRotation, 0);
            
            canPlace = CanPlaceBuilding(snapPosition, currentPlacementRotation);
            
            if (previewObject != null)
            {
                Vector3 finalPosition = currentPlacementPosition;
                if (currentBuilding.pivotOffset != Vector3.zero)
                {
                    finalPosition += currentPlacementRotation * currentBuilding.pivotOffset;
                }
                
                previewObject.transform.position = finalPosition;
                previewObject.transform.rotation = currentPlacementRotation;
                
                Material previewMat = canPlace ? validPlacementMaterial : invalidPlacementMaterial;
                foreach (Renderer rend in previewObject.GetComponentsInChildren<Renderer>())
                {
                    Material[] mats = new Material[rend.materials.Length];
                    for (int i = 0; i < mats.Length; i++)
                    {
                        mats[i] = previewMat;
                    }
                    rend.materials = mats;
                }
            }
        }
        else
        {
            if (previewObject != null)
            {
                previewObject.SetActive(false);
            }
            canPlace = false;
        }
    }
    
    Vector3 CalculateSnapPosition(Vector3 hitPoint, Vector3 hitNormal)
    {
        Vector3 snapSize = currentBuilding.snapSize;
        
        Vector3 rotatedSnapSize = snapSize;
        float rotationAngle = Mathf.Abs(currentRotation % 180f);
        if (rotationAngle > 80f && rotationAngle < 100f) 
        {
            rotatedSnapSize = new Vector3(snapSize.z, snapSize.y, snapSize.x);
        }
        
        Vector3 snapped = new Vector3(
            Mathf.Round(hitPoint.x / rotatedSnapSize.x) * rotatedSnapSize.x,
            Mathf.Round(hitPoint.y / snapSize.y) * snapSize.y,
            Mathf.Round(hitPoint.z / rotatedSnapSize.z) * rotatedSnapSize.z
        );
        
        return snapped;
    }
    
    bool CanPlaceBuilding(Vector3 position, Quaternion rotation)
    {
        if (!HasRequiredMaterials())
        {
            return false;
        }
        
        Collider[] overlaps = Physics.OverlapBox(
            position,
            currentBuilding.snapSize / 2f,
            rotation,
            buildingMask
        );
        
        return overlaps.Length == 0;
    }
    
    bool HasRequiredMaterials()
    {
        if (inventorySystem == null || currentBuilding == null) return false;
        
        foreach (BuildingCost cost in currentBuilding.costs)
        {
            int available = inventorySystem.GetItemCount(cost.material);
            if (available < cost.amount)
            {
                return false;
            }
        }
        
        return true;
    }
    
    void OnPlaceBuilding(InputAction.CallbackContext context)
    {
        if (!isBuildModeActive || !canPlace || currentBuilding == null) return;
        
        PlaceBuilding();
    }
    
    void PlaceBuilding()
    {
        foreach (BuildingCost cost in currentBuilding.costs)
        {
            inventorySystem.RemoveItem(cost.material, cost.amount);
        }
        
        Vector3 finalPosition = currentPlacementPosition;
        if (currentBuilding.pivotOffset != Vector3.zero)
        {
            finalPosition += currentPlacementRotation * currentBuilding.pivotOffset;
        }
        
        GameObject building = Instantiate(
            currentBuilding.prefab,
            finalPosition,
            currentPlacementRotation
        );
        
        building.name = currentBuilding.buildingName;
        
        building.layer = LayerMask.NameToLayer("Building");
        foreach (Transform child in building.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = LayerMask.NameToLayer("Building");
        }
        
        if (building.GetComponent<BuildingHealth>() == null)
        {
            BuildingHealth buildingHealth = building.AddComponent<BuildingHealth>();
            buildingHealth.maxHealth = 100f; 
        }
        
        placedBuildings.Add(building);
        
    }
    
    void RotateBuilding()
    {
        currentRotation += currentBuilding.rotationIncrement;
        if (currentRotation >= 360f) currentRotation = 0f;
        
    }
    
    void OnDrawGizmos()
    {
        if (isBuildModeActive && currentBuilding != null && previewObject != null)
        {
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(currentPlacementPosition, currentPlacementRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, currentBuilding.snapSize);
        }
    }
}