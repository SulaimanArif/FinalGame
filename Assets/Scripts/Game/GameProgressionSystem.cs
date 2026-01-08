using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Manages 7-day survival game progression
/// Each day enemies get stronger, survive 7 days to win
/// </summary>
public class GameProgressionSystem : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalDaysToSurvive = 7;
    public int currentDay = 1;
    
    [Header("Player Reference")]
    public PlayerStats playerStats; // Hook into player death
    
    [Header("Day/Night Cycle")]
    public DayNightCycle dayNightCycle;
    public float nightStartTime = 0.75f; // When night begins (0.75 = 75% through day)
    public float dayStartTime = 0.25f; // When day begins (0.25 = 25% through day)
    
    [Header("Difficulty Scaling")]
    [Tooltip("Enemy health multiplier per day")]
    public float healthScalePerDay = 1.3f; // 30% more HP each day
    
    [Tooltip("Enemy damage multiplier per day")]
    public float damageScalePerDay = 1.25f; // 25% more damage each day
    
    [Tooltip("Enemy speed multiplier per day")]
    public float speedScalePerDay = 1.1f; // 10% faster each day
    
    [Tooltip("More enemies spawn each day")]
    public int additionalEnemiesPerDay = 2;
    
    [Header("Events")]
    public UnityEvent<int> OnDayChanged; // Passes new day number
    public UnityEvent<int> OnNightStarted; // Passes current day
    public UnityEvent OnGameWon;
    public UnityEvent OnGameLost;
    
    [Header("UI")]
    public bool showDebugInfo = true;
    
    private bool isNightTime = false;
    private bool hasWon = false;
    private bool hasLost = false;
    private int nightsSurvived = 0;
    
    void Start()
    {
        if (dayNightCycle == null)
        {
            dayNightCycle = FindObjectOfType<DayNightCycle>();
        }
        
        if (playerStats == null)
        {
            playerStats = FindObjectOfType<PlayerStats>();
        }
        
        // Hook into player death event
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.AddListener(OnPlayerDeath);
        }
        
        if (dayNightCycle != null)
        {
            // Start at day time
            dayNightCycle.SetTimeOfDay(0.3f);
        }
        
        // Notify systems of starting day
        OnDayChanged?.Invoke(currentDay);
        
        if (showDebugInfo)
        {
            Debug.Log($"=== GAME STARTED ===");
            Debug.Log($"Survive {totalDaysToSurvive} nights to win!");
            Debug.Log($"Starting Day {currentDay}");
        }
    }
    
    void Update()
    {
        if (hasWon || hasLost) return;
        
        CheckDayNightTransition();
    }
    
    void CheckDayNightTransition()
    {
        if (dayNightCycle == null) return;
        
        float timeOfDay = dayNightCycle.GetTimeOfDay();
        bool isCurrentlyNight = timeOfDay >= nightStartTime || timeOfDay < dayStartTime;
        
        // Night just started
        if (isCurrentlyNight && !isNightTime)
        {
            isNightTime = true;
            OnNightStart();
        }
        // Day just started
        else if (!isCurrentlyNight && isNightTime)
        {
            isNightTime = false;
            OnDayStart();
        }
    }
    
    void OnNightStart()
    {
        if (showDebugInfo)
        {
            Debug.Log($"=== NIGHT {currentDay} STARTED ===");
            Debug.Log($"Enemies are {GetHealthMultiplier():F1}x stronger!");
            Debug.Log($"Survive until dawn...");
        }
        
        OnNightStarted?.Invoke(currentDay);
        
        // Apply difficulty scaling to spawned enemies
        ApplyDifficultyToEnemies();
    }
    
    void OnDayStart()
    {
        nightsSurvived++;
        
        if (showDebugInfo)
        {
            Debug.Log($"=== DAY {currentDay} - SURVIVED THE NIGHT ===");
            Debug.Log($"Nights survived: {nightsSurvived}/{totalDaysToSurvive}");
        }
        
        // Check win condition
        if (nightsSurvived >= totalDaysToSurvive)
        {
            WinGame();
            return;
        }
        
        // Progress to next day
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
        
        if (showDebugInfo)
        {
            Debug.Log($"=== DAY {currentDay} ===");
            Debug.Log($"Prepare for tonight...");
            Debug.Log($"Next night enemies will be even stronger!");
        }
    }
    
    void ApplyDifficultyToEnemies()
    {
        // Find all active enemies and buff them
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            ApplyScalingToEnemy(enemy);
        }
        
        if (showDebugInfo && enemies.Length > 0)
        {
            Debug.Log($"Applied Day {currentDay} scaling to {enemies.Length} enemies");
        }
    }
    
    public void ApplyScalingToEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        // Scale health
        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            float healthMultiplier = GetHealthMultiplier();
            float newMaxHealth = health.GetMaxHealth() * healthMultiplier;
            
            // Use reflection to set max health (since it's private)
            System.Reflection.FieldInfo maxHealthField = typeof(Health).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null)
            {
                maxHealthField.SetValue(health, newMaxHealth);
                
                // Also set current health to max
                System.Reflection.FieldInfo currentHealthField = typeof(Health).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentHealthField != null)
                {
                    currentHealthField.SetValue(health, newMaxHealth);
                }
            }
        }
        
        // Scale damage
        AIBehavior ai = enemy.GetComponent<AIBehavior>();
        if (ai != null)
        {
            float damageMultiplier = GetDamageMultiplier();
            ai.attackDamage *= damageMultiplier;
            
            // Scale speed
            float speedMultiplier = GetSpeedMultiplier();
            ai.moveSpeed *= speedMultiplier;
            ai.fleeSpeed *= speedMultiplier;
        }
    }
    
    public float GetHealthMultiplier()
    {
        return Mathf.Pow(healthScalePerDay, currentDay - 1);
    }
    
    public float GetDamageMultiplier()
    {
        return Mathf.Pow(damageScalePerDay, currentDay - 1);
    }
    
    public float GetSpeedMultiplier()
    {
        return Mathf.Pow(speedScalePerDay, currentDay - 1);
    }
    
    public int GetExtraEnemiesForDay()
    {
        return (currentDay - 1) * additionalEnemiesPerDay;
    }
    
    void WinGame()
    {
        if (hasWon) return;
        
        hasWon = true;
        
        if (showDebugInfo)
        {
            Debug.Log("======================");
            Debug.Log("=== YOU WIN! ===");
            Debug.Log($"Survived {totalDaysToSurvive} nights!");
            Debug.Log("======================");
        }
        
        OnGameWon?.Invoke();
        
        // Pause game or show victory screen
        Time.timeScale = 0f;
    }
    
    public void LoseGame()
    {
        if (hasLost || hasWon) return;
        
        hasLost = true;
        
        if (showDebugInfo)
        {
            Debug.Log("======================");
            Debug.Log("=== GAME OVER ===");
            Debug.Log($"Survived {nightsSurvived}/{totalDaysToSurvive} nights");
            Debug.Log($"Died on Day {currentDay}");
            Debug.Log("======================");
        }
        
        OnGameLost?.Invoke();
        
        // Show game over screen
        Time.timeScale = 0f;
    }
    
    public bool HasWon() => hasWon;
    public bool HasLost() => hasLost;
    public int GetCurrentDay() => currentDay;
    public int GetNightsSurvived() => nightsSurvived;
    public bool IsNightTime() => isNightTime;
    
    // Call this when player dies
    public void OnPlayerDeath()
    {
        LoseGame();
    }
    
    void OnGUI()
    {
        if (!showDebugInfo) return;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        
        // Day counter
        GUI.Label(new Rect(10, 10, 300, 30), $"Day {currentDay} / {totalDaysToSurvive}", style);
        GUI.Label(new Rect(10, 35, 300, 30), $"Nights Survived: {nightsSurvived}", style);
        
        // Current time
        string timeOfDayStr = isNightTime ? "NIGHT" : "DAY";
        GUI.Label(new Rect(10, 60, 300, 30), $"Time: {timeOfDayStr}", style);
        
        // Difficulty info
        GUI.Label(new Rect(10, 90, 400, 30), $"Enemy HP: {GetHealthMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 115, 400, 30), $"Enemy Damage: {GetDamageMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 140, 400, 30), $"Enemy Speed: {GetSpeedMultiplier():F1}x", style);
    }
}