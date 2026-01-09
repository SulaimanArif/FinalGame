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
    public float attackSpeed = 1f;
    public float attackRange = 3f;
    
    [Header("Tool Type")]
    public ToolType toolType = ToolType.Axe;
    
    [Header("Durability")]
    public bool hasDurability = true;
    public int maxDurability = 100;
    [HideInInspector]
    public int currentDurability;
    
    [Header("Bonus Damage Targets")]
    public BonusDamageTarget[] bonusDamageTargets;
    
    [Header("Visual/Audio")]
    public GameObject worldPrefab;
    public AudioClip swingSound;
    public AudioClip hitSound;
    
    void OnEnable()
    {
        currentDurability = maxDurability;
    }
    
    public float CalculateDamage(GameObject target)
    {
        if (target == null) return baseDamage;
        
        float totalDamage = baseDamage;
        
        foreach (BonusDamageTarget bonusTarget in bonusDamageTargets)
        {
            if (bonusTarget.targetPrefab == null) continue;
            
            if (IsPrefabMatch(target, bonusTarget.targetPrefab))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                return totalDamage;
            }
            
            if (!string.IsNullOrEmpty(bonusTarget.targetTag) && target.CompareTag(bonusTarget.targetTag))
            {
                totalDamage += bonusTarget.bonusDamage;
                
                return totalDamage;
            }
        }
        
        return totalDamage;
    }
    
    bool IsPrefabMatch(GameObject instance, GameObject prefab)
    {
        #if UNITY_EDITOR
        GameObject prefabRoot = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(instance);
        if (prefabRoot == prefab) return true;
        #endif
        
        string instanceName = instance.name.Replace("(Clone)", "").Trim();
        string prefabName = prefab.name;
        
        return instanceName == prefabName;
    }
    
    public void UseTool()
    {
        if (!hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
        }
    }
    
    public void Repair(int amount)
    {
        if (!hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, maxDurability);
    }
    
    public bool IsBroken()
    {
        return hasDurability && currentDurability <= 0;
    }
    
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