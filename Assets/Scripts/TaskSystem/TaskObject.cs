using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskObject : MonoBehaviour
{
    public Task thisTask;
    PlayerObject assignedKin;
    PlayerControls inputControls;
    private void Awake()
    {
        if (inputControls == null)
            inputControls = new PlayerControls();
    }
    public void RunInteract()
    {
        if (assignedKin.inputActions.PlayerMovement.Interact.IsPressed())
        {
            Debug.Log("Interacting");
            TaskManager.instance.RemoveFromTasks(assignedKin, thisTask);
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerObject>())
        {
            
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
            RunInteract();
        }
    }
}