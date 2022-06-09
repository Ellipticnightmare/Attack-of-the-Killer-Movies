using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndDoorCol : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        GameManager.instance.FinishedGame();
    }
}