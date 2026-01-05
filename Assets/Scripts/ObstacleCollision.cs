using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObstacleCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag == "LevelPart")
        {
            if (PlayerMove.movingForward)
            {
                PlayerMove.canMoveFoward = false;
                PlayerMove.canMoveBackward = true;

            }

            if (PlayerMove.movingBackward)
            {
                PlayerMove.canMoveBackward = false;
                PlayerMove.canMoveFoward = true;
            }
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.tag == "LevelPart")
        {
            PlayerMove.canMoveFoward = true;
            PlayerMove.canMoveBackward = true;
        }
    }
}