using UnityEngine;

[CreateAssetMenu(fileName = "NewTool", menuName = "Inventory/Tool Data")]
public class ToolData : ScriptableObject
{
    [Header("Tool Info")]
    public string toolName = "Axe";
    public Sprite icon;
    [TextArea(2, 4)]
    public string description = "A tool for gathering resources";
    
    [Header("Base Stats")]
    public float baseDamage = 10f;
    public float attackSpeed = 1f; // Attacks per second
    public float attackRange = 3f;
    
    [Header("Tool Type")]
    public ToolType toolType = ToolType.Axe;
    
    [Header("Durability")]
    public bool hasDurability = true;
    public int maxDurability = 100;
    [HideInInspector]
    public int currentDurability;
    
    [Header("Bonus Damage Targets")]
    [Tooltip("Drag prefabs/objects here that this tool does extra damage to")]
    public BonusDamageTarget[] bonusDamageTargets;
    
    [Header("Visual/Audio")]
    public GameObject worldPrefab; // 3D model for holding
    public AudioClip swingSound;
    public AudioClip hitSound;
    
    void OnEnable()
    {
        // Initialize durability
        currentDurability = maxDurability;
    }
    
    /// <summary>
    /// Calculate damage against a specific target
    /// </summary>
    public float CalculateDamage(GameObject target)
    {
        if (target == null) return baseDamage;
        
        float totalDamage = baseDamage;
        
        // Check if target matches any bonus damage targets
        foreach (BonusDamageTarget bonusTarget in bonusDamageTargets)
        {
            if (bonusTarget.targetPrefab == null) continue;
            
            // Check if the hit object matches the bonus target prefab
            // This works for instantiated prefabs
            if (IsPrefabMatch(target, bonusTarget.targetPrefab))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                Debug.Log($"{toolName} hit {target.name}: Base({baseDamage}) + Bonus({bonusTarget.bonusDamage}) = {totalDamage}");
                return totalDamage;
            }
            
            // Also check by tag if specified
            if (!string.IsNullOrEmpty(bonusTarget.targetTag) && target.CompareTag(bonusTarget.targetTag))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                Debug.Log($"{toolName} hit {target.name} by tag: Base({baseDamage}) + Bonus({bonusTarget.bonusDamage}) = {totalDamage}");
                return totalDamage;
            }
        }
        
        Debug.Log($"{toolName} hit {target.name}: {baseDamage} (no bonus)");
        return totalDamage;
    }
    
    /// <summary>
    /// Check if a GameObject instance matches a prefab
    /// </summary>
    bool IsPrefabMatch(GameObject instance, GameObject prefab)
    {
        #if UNITY_EDITOR
        // In editor, check prefab connection
        GameObject prefabRoot = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (prefabRoot == prefab) return true;
        #endif
        
        // Fallback: Check by name (less reliable but works at runtime)
        string instanceName = instance.name.Replace("(Clone)", "").Trim();
        string prefabName = prefab.name;
        
        return instanceName == prefabName;
    }
    
    /// <summary>
    /// Reduce durability after use
    /// </summary>
    public void UseTool()
    {
        if (!hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            Debug.Log($"{toolName} broke!");
        }
    }
    
    /// <summary>
    /// Repair the tool
    /// </summary>
    public void Repair(int amount)
    {
        if (!hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, maxDurability);
    }
    
    /// <summary>
    /// Check if tool is broken
    /// </summary>
    public bool IsBroken()
    {
        return hasDurability && currentDurability <= 0;
    }
    
    /// <summary>
    /// Get durability as percentage
    /// </summary>
    public float GetDurabilityPercent()
    {
        if (!hasDurability) return 1f;
        return (float)currentDurability / maxDurability;
    }
}

[System.Serializable]
public class BonusDamageTarget
{
    [Tooltip("The prefab this tool does bonus damage to (e.g., Tree prefab)")]
    public GameObject targetPrefab;
    
    [Tooltip("Or match by tag (optional, alternative to prefab)")]
    public string targetTag = "";
    
    [Tooltip("Extra damage dealt to this target")]
    public float bonusDamage = 30f;
    
    [Tooltip("Name/description for organization")]
    public string targetName = "Tree";
}

public enum ToolType
{
    Axe,
    Pickaxe,
    Hammer,
    Shovel,
    Sword,
    Scythe
}