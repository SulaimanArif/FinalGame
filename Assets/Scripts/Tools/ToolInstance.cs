using UnityEngine;

/// <summary>
/// Represents a single instance of a tool with its own durability
/// Store this alongside ItemData in inventory slots
/// </summary>
[System.Serializable]
public class ToolInstance
{
    public ToolData toolData;
    public int currentDurability;
    
    public ToolInstance(ToolData data)
    {
        toolData = data;
        currentDurability = data.maxDurability;
    }
    
    /// <summary>
    /// Use the tool, reducing durability
    /// </summary>
    public void UseTool()
    {
        if (!toolData.hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
            Debug.Log($"{toolData.toolName} broke!");
        }
    }
    
    /// <summary>
    /// Repair the tool
    /// </summary>
    public void Repair(int amount)
    {
        if (!toolData.hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, toolData.maxDurability);
    }
    
    /// <summary>
    /// Check if tool is broken
    /// </summary>
    public bool IsBroken()
    {
        return toolData.hasDurability && currentDurability <= 0;
    }
    
    /// <summary>
    /// Get durability as percentage
    /// </summary>
    public float GetDurabilityPercent()
    {
        if (!toolData.hasDurability) return 1f;
        return (float)currentDurability / toolData.maxDurability;
    }
}