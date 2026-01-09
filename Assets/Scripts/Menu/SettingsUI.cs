using UnityEngine;
using TMPro;

public class SettingsUI : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown difficulty;
    [SerializeField] private TMP_InputField mouseSensitivity;

    public GameObject settingsPanel;

    void OnEnable()
    {

        if (difficulty == null)
            difficulty = settingsPanel.GetComponentInChildren<TMP_Dropdown>(true);

        if (mouseSensitivity == null)
            mouseSensitivity = settingsPanel.GetComponentInChildren<TMP_InputField>(true);

        if (difficulty == null || mouseSensitivity == null)
        {
            Debug.LogError("UI fields not found in SettingsUI!");
            return;
        }

        difficulty.value = SettingsData.difficulty;
        mouseSensitivity.text = SettingsData.mouseSensitivity.ToString();
    }

    public void OnDifficultyChanged()
    {
        int index = difficulty.value;
        SettingsData.difficulty = index;
        Debug.Log("New Difficulty of");
        Debug.Log(SettingsData.difficulty);
    }

    public void OnMouseSensitivityChanged()
    {
        string value = mouseSensitivity.text;
        if (float.TryParse(value, out float result))
            SettingsData.mouseSensitivity = result;
        Debug.Log("New Sensivity of");
        Debug.Log(SettingsData.mouseSensitivity);
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
