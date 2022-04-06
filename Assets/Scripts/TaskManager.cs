using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public Task[] allTasks; // an array to hold all tasks
    public Task[] activeTasks; // activetasks for the current game session

    public Task testTask; 

    public GameObject player; // this is to assign this task to a player

    public GameObject taskList; // reference to the task UI element
    public Text titleText; 
    public Text descriptionText;
    public Text numberToCompletionText;
    public Text numberCompletedText;

    
    void Start()
    {
        testTask.title = "Buttery Popcorn";
        testTask.description = "Interact with the butter lever to add butter to your popcorn";
        testTask.numberCompleted = 0;
        testTask.numToCompletion = 1;
        testTask.goal.goalType = GoalType.Interact;
        #region Tasks
        allTasks[0].title = "Buttery Popcorn";
        allTasks[0].description = "Yo";


    #endregion

        //AssignTasks();
    }

    public void AssignTasks()
    {
        // This will be used to update the task list onscreen and choose from a pool of quests
        taskList.SetActive(true);

        //AssignUI();
        titleText.text = testTask.title;  // will use a find function to set titleText0 to activeTasks[0].title (a for loop will do this for all tasks)
        descriptionText.text = testTask.description;
        numberToCompletionText.text = testTask.numToCompletion.ToString();
        numberCompletedText.text = testTask.numberCompleted.ToString();

       

    }

    public void AddTaskToPlayer()
    {
        //This is to assign these tasks to the player so that they can be completed
        testTask.isActive = true;
        //player.task = testTask; 

    }

    public void AssignUI() //adds active tasks to correct tasklist positions
    {
        
        for(int i =0; i< activeTasks.Length - 1; i++)
        {
            Text temp = GameObject.Find("titleText"+i).GetComponent<Text>();
            temp.text = activeTasks[i].title;

            Text temp1 = GameObject.Find("descriptionText"+i).GetComponent<Text>();
            temp1.text = activeTasks[i].description;

            Text temp2 = GameObject.Find("numberToCompletionText"+i).GetComponent<Text>();
            temp2.text = activeTasks[i].numToCompletion.ToString();

            Text temp3 = GameObject.Find("numberCompletedText" + i).GetComponent<Text>();
            temp3.text = activeTasks[i].numberCompleted.ToString();
        }

    }


}
