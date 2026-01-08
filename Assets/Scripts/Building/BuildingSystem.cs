using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuildingSystem : MonoBehaviour
{
    public Material guideMaterial; // Translucent green material for the guide
    public GameObject defaultGuidePrefab; // Default guide prefab

    private GameObject currentGuide;
    private bool isBuildingModeActive = false;

    private string currentBuildingType = "Floor"; // Default building type
    private Dictionary<string, GameObject> buildingPrefabs = new Dictionary<string, GameObject>();

    // Event listener to toggle building mode
    public void ToggleBuildingMode()
    {
        isBuildingModeActive = !isBuildingModeActive;

        if (isBuildingModeActive)
        {
            ActivateBuildingMode();
        }
        else
        {
            DeactivateBuildingMode();
        }
    }

    private void ActivateBuildingMode()
    {
        if (defaultGuidePrefab != null)
        {
            currentGuide = Instantiate(defaultGuidePrefab);
            currentGuide.GetComponent<Renderer>().material = guideMaterial;
        }
    }

    private void DeactivateBuildingMode()
    {
        if (currentGuide != null)
        {
            Destroy(currentGuide);
        }
    }

    public void SetBuildingType(string buildingType)
    {
        if (buildingPrefabs.ContainsKey(buildingType))
        {
            currentBuildingType = buildingType;

            // Update the guide to match the selected building type
            if (currentGuide != null)
            {
                Destroy(currentGuide);
                currentGuide = Instantiate(buildingPrefabs[buildingType]);
                currentGuide.GetComponent<Renderer>().material = guideMaterial;
            }
        }
        else
        {
            Debug.LogWarning($"Building type {buildingType} not found in buildingPrefabs.");
        }
    }

    void Update()
    {
        if (isBuildingModeActive && currentGuide != null)
        {
            UpdateGuidePosition();

            if (Input.GetMouseButtonDown(0)) // Left mouse button to place
            {
                PlaceBuildingPiece();
            }
        }
    }

    private void UpdateGuidePosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 position = hit.point;

            // Snap to nearby pieces if close enough
            Collider[] nearbyColliders = Physics.OverlapSphere(position, 1.0f);
            foreach (var collider in nearbyColliders)
            {
                if (collider.CompareTag("BuildingPiece"))
                {
                    position = collider.transform.position;
                    break;
                }
            }

            currentGuide.transform.position = position;
        }
    }

    private void PlaceBuildingPiece()
    {
        if (buildingPrefabs.ContainsKey(currentBuildingType))
        {
            GameObject buildingPiece = Instantiate(buildingPrefabs[currentBuildingType], currentGuide.transform.position, Quaternion.identity);
            buildingPiece.tag = "BuildingPiece"; // Tag it for snapping logic
        }
        else
        {
            Debug.LogWarning($"Building prefab for {currentBuildingType} not found.");
        }
    }

    public void RegisterBuildingPrefab(string buildingType, GameObject prefab)
    {
        if (!buildingPrefabs.ContainsKey(buildingType))
        {
            buildingPrefabs.Add(buildingType, prefab);
        }
        else
        {
            Debug.LogWarning($"Building type {buildingType} is already registered.");
        }
    }
}
