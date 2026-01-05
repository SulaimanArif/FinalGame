using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    public GameObject modeButtons;
    public GameObject mainButtons;

    void OnMouseEnter()
    {
        gameObject.GetComponent<Animator>().Play("selected");
        Debug.Log("Mouse");

    }

    void OnMouseExit()
    {
        gameObject.GetComponent<Animator>().Play("deselected");

    }

    void OnMouseDown()
    {
        gameObject.GetComponent<Animator>().Play("press");

        if (gameObject.tag == "Play Button") 
        {
            modeButtons.SetActive(true);
            mainButtons.SetActive(false);
        }
    }
}