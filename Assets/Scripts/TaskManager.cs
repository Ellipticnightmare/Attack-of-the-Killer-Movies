using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public Task testTask; // will become an array to hold a number of tasks

    //public Player player; // this is to assign this task to a player

    public GameObject taskList; // reference to the task UI element
    public Text titleText;
    public Text descriptionText;
    public Text numberToCompletionText;
    public Text numberCompletedText;
    public void AssignTasks()
    {
        // This will be used to update the task list onscreen and choose from a pool of quests
        taskList.SetActive(true);
        titleText.text = testTask.title;
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

}
