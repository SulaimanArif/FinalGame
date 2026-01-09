using UnityEngine;

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

    public void UseTool()
    {
        if (!toolData.hasDurability) return;
        
        currentDurability--;
        
        if (currentDurability <= 0)
        {
            currentDurability = 0;
        }
    }
    
    public void Repair(int amount)
    {
        if (!toolData.hasDurability) return;
        
        currentDurability = Mathf.Min(currentDurability + amount, toolData.maxDurability);
    }
    
    public bool IsBroken()
    {
        return toolData.hasDurability && currentDurability <= 0;
    }
    
    public float GetDurabilityPercent()
    {
        if (!toolData.hasDurability) return 1f;
        return (float)currentDurability / toolData.maxDurability;
    }
}