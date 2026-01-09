using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackstoryUI : MonoBehaviour
{
    [SerializeField] private GameObject backstoryPanel;

    public void ShowBackstory()
    {
        if (backstoryPanel != null)
            backstoryPanel.SetActive(true);
    }

    public void CloseBackstory()
    {
        if (backstoryPanel != null)
            backstoryPanel.SetActive(false);
    }
}
