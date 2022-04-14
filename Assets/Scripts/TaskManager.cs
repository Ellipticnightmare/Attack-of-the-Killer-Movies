using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public Task[] allTasks; // an array to hold all tasks
    public Task[] activeTasks; // activetasks for the current game session

    public bool tasksComplete; // set to true if activeTasks is empty.

    public List<Transform> taskLocations; // set of points for each tasks, some will be static (can only appear in one place)
    
    public Task finalTask; 

    //public GameObject player; // this is to assign this task to a player

    public GameObject taskList; // reference to the task UI element
    public Text titleText; 
    public Text descriptionText;
    public Text numberToCompletionText;
    public Text numberCompletedText;

    private void Awake()
    {
        // This will be done in the inspector to save alot of time, these are just test tasks
        #region Tasks

        finalTask.title = "Escape the Cinema";
        finalTask.description = "Get the hell out of here";
        finalTask.numberCompleted = 0;
        finalTask.numToCompletion = 1;
        finalTask.goal.goalType = GoalType.Interact;
        finalTask.spawnType = SpawnType.Static;
        finalTask.spawnedTaskObjects = null;
        
        /*
        
        allTasks[0].title = "Buttery Popcorn";
        allTasks[0].description = "Yo";
        allTasks[0].numToCompletion = 1;
        allTasks[0].numberCompleted = 0;
        allTasks[0].goal.goalType = GoalType.Interact;
        allTasks[0].spawnType = SpawnType.Static;
        allTasks[0].spawnedTaskObjects = null; // this will spawn a prefab of the task object

        allTasks[1].title = "Testin this out";
        allTasks[1].description = "lets see if this works";
        allTasks[1].numToCompletion = 5;
        allTasks[1].numberCompleted = 0;
        allTasks[1].goal.goalType = GoalType.Interact;
        allTasks[1].spawnedTaskObjects = null; // this will spawn a prefab of the task object
        */
        #endregion
    }
    void Start()
    {
        //testTask.title = "Pull that Lever";
        //testTask.description = "Go ahead and pull it, it's just on that wall over there and watch out for that monster";
        //testTask.numberCompleted = 0;
        //testTask.numToCompletion = 1;
        //testTask.goal.goalType = GoalType.Interact;
       
        

        AssignTasks();
    }

    public void AssignTasks()
    {
        // This will be used to update the task list onscreen and choose from a pool of quests
        taskList.SetActive(true);

        for (int i = activeTasks.Length - 1; i >= 0; i--)
        {
            int randomInt = Random.Range(0, allTasks.Length);
            int randomTaskLocation = Random.Range(0, taskLocations.Count - 1);

            activeTasks[i].title = allTasks[randomInt].title;
            activeTasks[i].description = allTasks[randomInt].description;
            activeTasks[i].numberCompleted = allTasks[randomInt].numberCompleted;
            activeTasks[i].numToCompletion = allTasks[randomInt].numToCompletion;
            activeTasks[i].goal = allTasks[randomInt].goal;
            activeTasks[i].spawnType = allTasks[randomInt].spawnType;
            activeTasks[i].spawnedTaskObjects = allTasks[randomInt].spawnedTaskObjects;
            activeTasks[i].spawnLocation = allTasks[randomInt].spawnLocation;
            
            //instantiate task objects
            if (activeTasks[i].spawnType == SpawnType.Static)
            {
                for (int s = 0; s < activeTasks[i].spawnedTaskObjects.Length; s++)
                {

                    GameObject taskObject = Instantiate(activeTasks[i].spawnedTaskObjects[s], activeTasks[i].spawnLocation);
                    InteractBox box = taskObject.GetComponent<InteractBox>();
                    box.taskName = activeTasks[i].title;
                }
            }
            /*
            else {
                for (int j = 0; j > activeTasks[i].spawnedTaskObjects.Length; j++)
                {
                    Instantiate(activeTasks[i].spawnedTaskObjects[j], taskLocations[randomTaskLocation]);
                    taskLocations.RemoveAt(randomTaskLocation);
                }
            
            }
            */
        }


        //titleText.text = testTask.title;  // will use a find function to set titleText0 to activeTasks[0].title (a for loop will do this for all tasks)
        //descriptionText.text = testTask.description;
        //numberToCompletionText.text = testTask.numToCompletion.ToString();
        //numberCompletedText.text = testTask.numberCompleted.ToString();

        AssignUI();

    }

    public void AddTaskToPlayer()
    {
        //This is to assign these tasks to the player so that they can be completed
        //testTask.isActive = true;
        //player.GetComponent<PlayerMasterController>().task = testTask;

    }

    public void AssignUI() //adds active tasks to correct tasklist positions
    {
        
        for(int i =0; i< activeTasks.Length; i++)
        {
            Text temp = GameObject.Find("taskTitle" + i).GetComponent<Text>();
            temp.text = activeTasks[i].title;

            Text temp1 = GameObject.Find("taskDescription" + i).GetComponent<Text>();
            temp1.text = activeTasks[i].description;

            Text temp2 = GameObject.Find("taskNumRequired" + i).GetComponent<Text>();
            temp2.text = activeTasks[i].numToCompletion.ToString();

            Text temp3 = GameObject.Find("taskNumDone" + i).GetComponent<Text>();
            temp3.text = activeTasks[i].numberCompleted.ToString();
        }

    }

    public void UpdateUI(string taskName)
    {
        foreach(Task completedTask in activeTasks)
        {
            if (completedTask.title == taskName)
            {
                int indexPosition = System.Array.IndexOf(activeTasks,completedTask);
                GameObject taskInCanvas = GameObject.Find("Task" + indexPosition);
                taskInCanvas.SetActive(false);
                for(int index = indexPosition + 1; activeTasks.Length > index; index++ )
                {
                    GameObject otherTask = GameObject.Find("Task" + index);
                    RectTransform rect = otherTask.GetComponent<RectTransform>();
                    // trying to move the pos y value of the rectangle up in the list.
                }
                completedTask.isActive = false;
                
            }
        }
        //check for inactive tasks in activeTasks array
        //move all tasks up on the ui 
        if(CheckAllTask())
        {
            AssignFinalTask();
        }
    }
    
    public bool CheckAllTask()
    {
        int allTasksInactive = 0;
        foreach(Task activeTask in activeTasks)
        {
            if (activeTask.isActive)
            {
                allTasksInactive += 1;
            }

        }
        return (allTasksInactive <= 0);
    }

    public void AssignFinalTask()
    {
        activeTasks[0].title = finalTask.title;
        activeTasks[0].description = finalTask.description;
        activeTasks[0].numberCompleted = finalTask.numberCompleted;
        activeTasks[0].numToCompletion = finalTask.numToCompletion;
        activeTasks[0].goal = finalTask.goal;
        activeTasks[0].spawnType = finalTask.spawnType;
        activeTasks[0].spawnedTaskObjects = finalTask.spawnedTaskObjects;
        activeTasks[0].isActive = true;

        Instantiate(activeTasks[0].spawnedTaskObjects[0], activeTasks[0].spawnLocation);

        GameObject finalTaskUI = GameObject.Find("Task0");
        finalTaskUI.SetActive(true);

        Text temp = GameObject.Find("taskTitle" + 0).GetComponent<Text>();
        temp.text = activeTasks[0].title;

        Text temp1 = GameObject.Find("taskDescription" + 0).GetComponent<Text>();
        temp1.text = activeTasks[0].description;

        Text temp2 = GameObject.Find("taskNumRequired" + 0).GetComponent<Text>();
        temp2.text = activeTasks[0].numToCompletion.ToString();

        Text temp3 = GameObject.Find("taskNumDone" + 0).GetComponent<Text>();
        temp3.text = activeTasks[0].numberCompleted.ToString();
    }

}
