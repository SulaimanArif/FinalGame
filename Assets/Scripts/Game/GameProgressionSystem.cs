using UnityEngine;
using UnityEngine.Events;

public class GameProgressionSystem : MonoBehaviour
{
    [Header("Game Settings")]
    public int totalDaysToSurvive = 7;
    public int currentDay = 1;
    
    [Header("Player Reference")]
    public PlayerStats playerStats;
    
    [Header("Day/Night Cycle")]
    public DayNightCycle dayNightCycle;
    public float nightStartTime = 0.75f; 
    public float dayStartTime = 0.25f; 
    
    [Header("Difficulty Scaling")]
    [Tooltip("Enemy health multiplier per day")]
    public float healthScalePerDay = 1.3f; 
    
    [Tooltip("Enemy damage multiplier per day")]
    public float damageScalePerDay = 1.25f; 
    
    [Tooltip("Enemy speed multiplier per day")]
    public float speedScalePerDay = 1.1f; 
    
    [Tooltip("More enemies spawn each day")]
    public int additionalEnemiesPerDay = 2;

    private float difficultyMultiplier = 1f;
    
    [Header("Events")]
    public UnityEvent<int> OnDayChanged; 
    public UnityEvent<int> OnNightStarted; 
    public UnityEvent OnGameWon;
    public UnityEvent OnGameLost;
    
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
        
        if (playerStats != null)
        {
            playerStats.OnPlayerDeath.AddListener(OnPlayerDeath);
        }
        
        if (dayNightCycle != null)
        {
            dayNightCycle.SetTimeOfDay(0.3f);
        }
        
        OnDayChanged?.Invoke(currentDay);
    }
    
    void Update()
    {
        if (hasWon || hasLost) return;
        
        CheckDayNightTransition();
        ApplyDifficultySettings();
    }
    
    void CheckDayNightTransition()
    {
        if (dayNightCycle == null) return;
        
        float timeOfDay = dayNightCycle.GetTimeOfDay();
        bool isCurrentlyNight = timeOfDay >= nightStartTime || timeOfDay < dayStartTime;
        
        if (isCurrentlyNight && !isNightTime)
        {
            isNightTime = true;
            OnNightStart();
        }
        else if (!isCurrentlyNight && isNightTime)
        {
            isNightTime = false;
            OnDayStart();
        }
    }
    
    void OnNightStart()
    {
        OnNightStarted?.Invoke(currentDay);
        ApplyDifficultyToEnemies();
    }
    
    void OnDayStart()
    {
        nightsSurvived++;
        
        if (nightsSurvived >= totalDaysToSurvive)
        {
            WinGame();
            return;
        }
        
        currentDay++;
        OnDayChanged?.Invoke(currentDay);
    }
    
    void ApplyDifficultyToEnemies()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        
        foreach (GameObject enemy in enemies)
        {
            ApplyScalingToEnemy(enemy);
        }
    }
    
    public void ApplyScalingToEnemy(GameObject enemy)
    {
        if (enemy == null) return;
        
        Health health = enemy.GetComponent<Health>();
        if (health != null)
        {
            float healthMultiplier = GetHealthMultiplier();
            float newMaxHealth = health.GetMaxHealth() * healthMultiplier;
            
            System.Reflection.FieldInfo maxHealthField = typeof(Health).GetField("maxHealth", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (maxHealthField != null)
            {
                maxHealthField.SetValue(health, newMaxHealth);
                
                System.Reflection.FieldInfo currentHealthField = typeof(Health).GetField("currentHealth", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
                if (currentHealthField != null)
                {
                    currentHealthField.SetValue(health, newMaxHealth);
                }
            }
        }
        
        AIBehavior ai = enemy.GetComponent<AIBehavior>();
        if (ai != null)
        {
            float damageMultiplier = GetDamageMultiplier();
            ai.attackDamage *= damageMultiplier;
            
            float speedMultiplier = GetSpeedMultiplier();
            ai.moveSpeed *= speedMultiplier;
            ai.fleeSpeed *= speedMultiplier;
        }
    }

    void ApplyDifficultySettings()
    {
        switch (SettingsData.difficulty)
        {
            case 0: difficultyMultiplier = 0.75f; break;
            case 1: difficultyMultiplier = 1f;    break;
            case 2: difficultyMultiplier = 1.25f; break;
            default: difficultyMultiplier = 1f;   break;
        }
    }
    
    public float GetHealthMultiplier()
    {
        return Mathf.Pow(healthScalePerDay, currentDay - 1) * difficultyMultiplier;
    }
    
    public float GetDamageMultiplier()
    {
        return Mathf.Pow(damageScalePerDay, currentDay - 1) * difficultyMultiplier;
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
        
        OnGameWon?.Invoke();
        
        Time.timeScale = 0f;
    }
    
    public void LoseGame()
    {
        if (hasLost || hasWon) return;
        
        hasLost = true;
        
        OnGameLost?.Invoke();
        
        Time.timeScale = 0f;
    }
    
    public bool HasWon() => hasWon;
    public bool HasLost() => hasLost;
    public int GetCurrentDay() => currentDay;
    public int GetNightsSurvived() => nightsSurvived;
    public bool IsNightTime() => isNightTime;
    
    public void OnPlayerDeath()
    {
        LoseGame();
    }
    
    void OnGUI()
    {   
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 20;
        style.fontStyle = FontStyle.Bold;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 300, 30), $"Day {currentDay} / {totalDaysToSurvive}", style);
        GUI.Label(new Rect(10, 35, 300, 30), $"Nights Survived: {nightsSurvived}", style);
        
        string timeOfDayStr = isNightTime ? "NIGHT" : "DAY";
        GUI.Label(new Rect(10, 60, 300, 30), $"Time: {timeOfDayStr}", style);
        
        GUI.Label(new Rect(10, 90, 400, 30), $"Enemy HP: {GetHealthMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 115, 400, 30), $"Enemy Damage: {GetDamageMultiplier():F1}x", style);
        GUI.Label(new Rect(10, 140, 400, 30), $"Enemy Speed: {GetSpeedMultiplier():F1}x", style);
    }
}