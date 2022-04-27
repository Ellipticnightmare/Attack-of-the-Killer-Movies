using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskObject : MonoBehaviour
{
    public bool doesDiscriminateBetweenPlayers = true;
    List<PlayerObject> hasInteracted = new List<PlayerObject>();
    public Task thisTask;
    float startTime = 0f;
    TaskUI curHeldTask;
    PlayerControls inputActions;

    private void Start()
    {

    }
    public void RunInteract(PlayerObject newInteractor)
    {
        bool canIInteract = true;
        if (doesDiscriminateBetweenPlayers)
        {
            if (hasInteracted.Contains(newInteractor))
                canIInteract = false;
        }
        if (canIInteract)
        {
            //newInteractor.GetComponent<PlayerObject>().inputActions.PlayerMovement.Interact.performed += InteractWithObject;
          
            if (newInteractor.GetComponent<PlayerObject>().inputActions.PlayerMovement.Interact.IsPressed())//Input.GetKeyDown(KeyCode.E)) 
            {
                startTime = Time.time;
            }
            if (newInteractor.GetComponent<PlayerObject>().inputActions.PlayerMovement.Interact.IsPressed())//Input.GetKey(KeyCode.E))
            {
                if ((startTime + thisTask.numToCompletion) >= Time.time)
                    curHeldTask.UpdateTaskData(thisTask.numToCompletion, this);
            }
          
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.GetComponent<PlayerObject>() && thisTask.goalType == Task.GoalType.Interact)
        {
            RunInteract(other.GetComponent<PlayerObject>());
            foreach(var obj in other.GetComponent<PlayerObject>().myTasks)
            {
                if (obj.myTask == thisTask)
                    curHeldTask = obj;
            }
        }
    }
    public void InteractWithObject(InputAction.CallbackContext context)
    {
        startTime = Time.time;
        if ((startTime + thisTask.numToCompletion) >= Time.time)
            curHeldTask.UpdateTaskData(thisTask.numToCompletion, this);

    }
}