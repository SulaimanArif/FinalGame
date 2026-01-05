using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public GameObject player; // holds the player gameobject
    Rigidbody rb; // stores the rigidbody component of the player
    float rotateSpeed = 100; // the rotation speed of the player
    float moveSpeed = 600; // the movement speed of the player

    public static bool canMoveFoward = true;
    public static bool canMoveBackward = true;

    public static bool movingForward = false;
    public static bool movingBackward = false;

    // Update is called once per frame
    void Update()
    {
        if (SceneManage.gameStarted) 
        {
            if (Input.GetKey(KeyCode.A))
            {
                player.transform.Rotate(Vector3.down * rotateSpeed * Time.deltaTime);
            }

            if (Input.GetKey(KeyCode.D))
            {
                player.transform.Rotate(Vector3.up * rotateSpeed * Time.deltaTime);
            }

            if (canMoveFoward)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    player.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime);
                    movingForward = true;
                    movingBackward = false;
                }
            }

            if (canMoveBackward)
            {
                if (Input.GetKey(KeyCode.S))
                {
                    player.transform.Translate(Vector3.forward * moveSpeed * Time.deltaTime * -1);
                    movingForward = false;
                    movingBackward = true;
                }
            }
        }
    }
}