using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonHandler : MonoBehaviour
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Button is hovered");
        // Add your hover logic here
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Button is no longer hovered");
        // Add your exit hover logic here
    }
}
