using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskObject : MonoBehaviour
{
    public Task thisTask;
    PlayerObject assignedKin;
    public void RunInteract()
    {
        bool b_Input = new PlayerControls().PlayerMovement.Interact.phase == InputActionPhase.Performed;
        if (b_Input && assignedKin != null)
        {
            TaskManager.instance.RemoveFromTasks(assignedKin, thisTask);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerObject>())
        {
            RunInteract();
            if (assignedKin == null)
            {
                foreach (var obj in other.GetComponent<PlayerObject>().myTasks)
                {
                    if (obj == thisTask)
                    {
                        assignedKin = other.GetComponent<PlayerObject>();
                        Debug.Log("I've found my Kinnie uwu");
                    }
                }
            }
        }
    }
}