using UnityEngine;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    public TMP_Dropdown difficulty;
    public TMP_InputField mouseSensitivity;

    public GameObject settingsPanel;

    void Start()
    {
        difficulty.value = SettingsData.difficulty;
        mouseSensitivity.text = SettingsData.mouseSensitivity.ToString();
    }

    public void OnDifficultyChanged(int index)
    {
        SettingsData.difficulty = index;
    }

    public void OnMouseSensitivityChanged(string value)
    {
        if (float.TryParse(value, out float result))
            SettingsData.mouseSensitivity = result;
    }

    public void ShowSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}
