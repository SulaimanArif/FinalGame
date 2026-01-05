using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class DayNightCycle : MonoBehaviour
{
    [Header("Day/Night Settings")]
    public Light sunLight;
    public Light moonLight;
    public float dayDurationInSeconds = 120f;
    [Range(0f, 1f)]
    public float startTimeOfDay = 0.25f;
    
    [Header("Sun Light Settings (HDRP)")]
    public AnimationCurve lightIntensityCurve;
    public float maxSunIntensity = 100000f;
    public Gradient lightColorGradient;
    public AnimationCurve colorTemperatureCurve;
    public float minColorTemperature = 2000f;
    public float maxColorTemperature = 6500f;
    
    [Header("Moon Light Settings")]
    public float moonIntensity = 20000f;
    public Color moonColor = new Color(0.6f, 0.7f, 0.85f);
    public float moonColorTemperature = 7000f;
    
    [Header("Ambient Settings")]
    public Volume skyVolume;
    public AnimationCurve exposureCurve;
    public float dayExposure = 12f;
    public float nightExposure = 14f;
    
    private float timeOfDay = 0f;
    private HDAdditionalLightData hdSunLightData;
    private HDAdditionalLightData hdMoonLightData;
    private Exposure exposureOverride;
    
    void Start()
    {
        timeOfDay = startTimeOfDay;
        
        if (sunLight != null)
        {
            hdSunLightData = sunLight.GetComponent<HDAdditionalLightData>();
            if (hdSunLightData == null)
            {
                Debug.LogError("Sun Light is missing HDAdditionalLightData component!");
            }
        }
        
        if (moonLight != null)
        {
            hdMoonLightData = moonLight.GetComponent<HDAdditionalLightData>();
            if (hdMoonLightData == null)
            {
                Debug.LogError("Moon Light is missing HDAdditionalLightData component!");
            }
        }
        else
        {
            Debug.LogWarning("Moon Light is not assigned to DayNightCycle!");
        }
        
        if (skyVolume != null && skyVolume.profile.TryGet(out exposureOverride))
        {
            exposureOverride.mode.Override(ExposureMode.Fixed);
        }
        
        InitializeDefaultCurves();
    }
    
    void Update()
    {
        if (sunLight == null || hdSunLightData == null) return;
        
        timeOfDay += Time.deltaTime / dayDurationInSeconds;
        if (timeOfDay >= 1f) timeOfDay = 0f;
        
        float sunAngle = (timeOfDay * 360f) - 90f;
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        
        float intensityMultiplier = lightIntensityCurve.Evaluate(timeOfDay);
        hdSunLightData.SetIntensity(maxSunIntensity * intensityMultiplier);
        
        sunLight.color = lightColorGradient.Evaluate(timeOfDay);
        
        float colorTemp = Mathf.Lerp(minColorTemperature, maxColorTemperature, 
                                     colorTemperatureCurve.Evaluate(timeOfDay));
        hdSunLightData.SetColor(sunLight.color, colorTemp);
        
        if (moonLight != null && hdMoonLightData != null)
        {
            float moonAngle = sunAngle + 180f;
            moonLight.transform.rotation = Quaternion.Euler(moonAngle, 170f, 0f);
            
            // Moon is visible when sun is below horizon
            bool isNightTime = sunAngle < -90f || sunAngle > 90f;
            
            if (isNightTime)
            {
                // Keep moon at full brightness all night
                hdMoonLightData.SetIntensity(moonIntensity);
                moonLight.enabled = true;
                
                Debug.Log($"NIGHT - SunAngle:{sunAngle:F0} MoonAngle:{moonAngle:F0} MoonIntensity:{moonIntensity:F0}");
            }
            else
            {
                moonLight.enabled = false;
                Debug.Log($"DAY - SunAngle:{sunAngle:F0} Moon disabled");
            }
            
            hdMoonLightData.SetColor(moonColor, moonColorTemperature);
        }
        
        if (exposureOverride != null)
        {
            float exposure = Mathf.Lerp(nightExposure, dayExposure, 
                                       exposureCurve.Evaluate(timeOfDay));
            exposureOverride.fixedExposure.Override(exposure);
        }
    }
    
    void InitializeDefaultCurves()
    {
        if (lightIntensityCurve == null || lightIntensityCurve.keys.Length == 0)
        {
            lightIntensityCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.3f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.3f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (colorTemperatureCurve == null || colorTemperatureCurve.keys.Length == 0)
        {
            colorTemperatureCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.2f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.2f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (exposureCurve == null || exposureCurve.keys.Length == 0)
        {
            exposureCurve = new AnimationCurve(
                new Keyframe(0f, 0f),
                new Keyframe(0.25f, 0.5f),
                new Keyframe(0.5f, 1f),
                new Keyframe(0.75f, 0.5f),
                new Keyframe(1f, 0f)
            );
        }
        
        if (lightColorGradient == null || lightColorGradient.colorKeys.Length == 0)
        {
            lightColorGradient = new Gradient();
            GradientColorKey[] colorKeys = new GradientColorKey[5];
            colorKeys[0] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 0f);
            colorKeys[1] = new GradientColorKey(new Color(1f, 0.6f, 0.4f), 0.25f);
            colorKeys[2] = new GradientColorKey(new Color(1f, 1f, 0.95f), 0.5f);
            colorKeys[3] = new GradientColorKey(new Color(1f, 0.5f, 0.3f), 0.75f);
            colorKeys[4] = new GradientColorKey(new Color(0.05f, 0.05f, 0.2f), 1f);
            
            GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
            alphaKeys[0] = new GradientAlphaKey(1f, 0f);
            alphaKeys[1] = new GradientAlphaKey(1f, 1f);
            
            lightColorGradient.SetKeys(colorKeys, alphaKeys);
        }
    }
    
    public bool IsNight()
    {
        return timeOfDay >= 0.75f || timeOfDay <= 0.25f;
    }
    
    public float GetTimeOfDay()
    {
        return timeOfDay;
    }
    
    public void SetTimeOfDay(float time)
    {
        timeOfDay = Mathf.Clamp01(time);
    }
}