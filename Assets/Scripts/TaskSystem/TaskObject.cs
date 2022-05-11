using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskObject : MonoBehaviour
{
    public Task thisTask;
    PlayerObject assignedKin;
    public void RunInteract(PlayerObject newInteractor)
    {
        bool b_Input = new PlayerControls().PlayerMovement.Interact.phase == UnityEngine.InputSystem.InputActionPhase.Performed;
        if (b_Input && assignedKin != null)
        {
            TaskManager.instance.RemoveFromTasks(assignedKin, thisTask);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerObject>())
        {
            RunInteract(other.GetComponent<PlayerObject>());
            foreach (var obj in other.GetComponent<PlayerObject>().myTasks)
            {
                if (obj == thisTask)
                    assignedKin = other.GetComponent<PlayerObject>();
            }
        }
    }
}