using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractBox : MonoBehaviour
{
    public string taskName;
    public float startTime = 0f;
    public float holdTime = 2.0f; // 5 seconds
    //public GameObject resultsScreen;

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (other.GetComponent<InputHandler>().inputActions.PlayerMovement.Interaction.IsPressed())
            {
                startTime = Time.time;
                
            }

            if (other.GetComponent<InputHandler>().inputActions.PlayerMovement.Interaction.IsPressed())
                // check if the start time plus [holdTime] is more or equal to the current time.
                // If so, we held the button for [holdTime] seconds.
                if ((startTime + holdTime) >= Time.time)
                {
                    TaskManager taskManager = GameObject.Find("Tasklist").GetComponent<TaskManager>();
                    foreach(Task activeTask in taskManager.activeTasks)
                    {
                        if (taskName == activeTask.title)
                        {
                            activeTask.goal.isReached();
                        }
                    }
                    
                    //other.gameObject.GetComponent<PlayerMasterController>().task.goal.TaskSectionComplete();
                    //other.gameObject.GetComponent<Task>().TaskComplete();

                    //resultsScreen.SetActive(true);
                        
                }
        }
    }

 

}

