using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    static List<TaskObject> allTaskObjects = new List<TaskObject>();
    public TaskDatabase myTaskDatabase;
    [HideInInspector]
    public List<Task> allTasks = new List<Task>();
    static List<Task> activeTasks = new List<Task>();
    static List<Task> completedTasks = new List<Task>();
    static List<TaskUI> SelectedTaskUIs = new List<TaskUI>();
    static List<taskController> activePlayers = new List<taskController>();

    public GameObject taskObject; //reference to prefab that will show all task information
    public int maxDisplayTasks = 6;
    void Start()
    {
        allTasks.AddRange(myTaskDatabase.allTasks);
        TaskObject[] allObjects = FindObjectsOfType<TaskObject>();
        allTaskObjects.AddRange(allObjects);
        CreatePlayerLists();
    }
    public static void PlayerDied() //called from GameManager when a player dies, unlocks all tasks
                                    //in case surviving players have already completed a task there,
                                    //thus preventing hard locks where players would be unable to re-do
                                    //a task
    {
        foreach (var item in allTaskObjects)
        {
            item.doesDiscriminateBetweenPlayers = false;
        }
        FindObjectOfType<TaskManager>().ReDelegateTasks();
    }
    public void ReDelegateTasks() => StartCoroutine(RedelegateTasks());
    public static void UpdatedTask(TaskUI goal)
    {
        goal.UpdateTaskUI();
    }
    public static void RemoveFromTasks(TaskUI goal, TaskObject taskGoal)
    {
        foreach (var obj in activePlayers)
        {
            if (obj.myPlayer == goal.myPlayer)
            {
                foreach (var item in obj.myTasks)
                {
                    if (item == goal)
                    {
                        activeTasks.Remove(item.myTask);
                        obj.myTasks.Remove(item);
                        obj.myTaskChecks.Remove(item.myTask);
                        obj.UpdateMyUI();
                        return;
                    }
                }
            }
        }
        List<Task> taskChecks = new List<Task>();
        foreach (var item in activeTasks)
        {
            if (item == goal.myTask)
                taskChecks.Add(item);
        }
        if (taskChecks.Count <= 1)
        {
            taskGoal.enabled = false;
        }
        bool canWin = true;
        foreach (var item in activePlayers)
        {
            if (item.myTasks.Count >= 1)
                canWin = false;
        }
        if (canWin)
            GameManager.FinishedGame();
    }
    public void CreatePlayerLists()
    {
        PlayerObject[] players = FindObjectsOfType<PlayerObject>();
        foreach (var item in players)
        {
            taskController newController = new taskController();
            newController.myPlayer = item;
            newController.myTaskHolderList = item.myTaskListHolder;
            newController.newTaskObject = taskObject;
            activePlayers.Add(newController);
        }
        StartCoroutine(CreateTaskLists());
    }
    public static void UpdateWorld()
    {
        foreach (var item in allTaskObjects)
        {
            if (!activeTasks.Contains(item.thisTask))
                Destroy(item); //This can be changed to make the task objects static by changing to [item.enabled = false;]
        }
    }
    IEnumerator CreateTaskLists()
    {
        float rCheck = Random.Range(allTasks.Count / 3, allTasks.Count - 2);
        rCheck = (int)(rCheck / 4);
        allTasks = allTasks.OrderBy(x => Random.value).ToList();
        for (int i = 0; i < rCheck; i++)
        {
            activeTasks.Add(allTasks[i]);
        }
        yield return new WaitForEndOfFrame();
        List<Task> masterA = new List<Task>();
        List<Task> masterB = new List<Task>();
        for (int i = 0; i < activeTasks.Count; i++)
        {
            if (i % 2 == 0)
                masterB.Add(activeTasks[i]);
            else
                masterA.Add(activeTasks[i]);
        }
        yield return new WaitForEndOfFrame();
        if (masterA.Count % 2 != 0)
            masterA.Add(allTasks[(int)rCheck + 1]);
        yield return new WaitForEndOfFrame();
        if (masterB.Count < masterA.Count)
        {
            masterB.Add(allTasks[(int)rCheck + 2]);
        }
        if (masterB.Count < masterA.Count)
        {
            masterB.Add(allTasks[(int)rCheck + 3]);
        }
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < masterA.Count; i++)
        {
            if (i % 2 == 0)
                activePlayers[0].myTaskChecks.Add(masterA[i]);
            else
                activePlayers[1].myTaskChecks.Add(masterA[i]);
        }
        for (int i = 0; i < masterB.Count; i++)
        {
            if (i % 2 == 0)
            {
                activePlayers[2].myTaskChecks.Add(masterB[i]);
            }
            else
                activePlayers[3].myTaskChecks.Add(masterB[i]);
        }
        yield return new WaitForEndOfFrame();
        foreach (var item in activePlayers)
        {
            item.BuildMyTaskList();
        }
        yield return new WaitForEndOfFrame();
        UpdateWorld();
        GameManager.togglePause();
    }
    static IEnumerator RedelegateTasks()
    {
        List<PlayerObject> curLivingPlayers = new List<PlayerObject>();
        curLivingPlayers.AddRange(FindObjectsOfType<PlayerObject>());
        yield return new WaitForEndOfFrame();
        taskController diedPlayer = new taskController();
        foreach (var item in activePlayers)
        {
            if (!curLivingPlayers.Contains(item.myPlayer))
                diedPlayer = item;
        }
        yield return new WaitForEndOfFrame();
        activePlayers.Remove(diedPlayer);
        yield return new WaitForEndOfFrame();
        List<Task> newTasks = diedPlayer.myTaskChecks;
        IEnumerable<Task> subTaskList = newTasks;
        List<IEnumerable<Task>> listOfLists = new List<IEnumerable<Task>>();
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < newTasks.Count(); i += curLivingPlayers.Count)
        {
            listOfLists.Add(subTaskList.Skip(i).Take(curLivingPlayers.Count));
        }
        yield return new WaitForEndOfFrame();
        for (int i = 0; i < activePlayers.Count; i++)
        {
            activePlayers[i].myTaskChecks.AddRange(listOfLists[i]);
        }
        yield return new WaitForEndOfFrame();
        foreach (var item in activePlayers)
        {
            item.BuildMyTaskList();
        }
        UpdateWorld();
    }
}
//this is a smol change
#region Serialized Classes
[System.Serializable]
public class taskController : MonoBehaviour
{
    public List<Task> myTaskChecks = new List<Task>();
    public List<TaskUI> myTasks = new List<TaskUI>();
    public List<GameObject> myTaskObjects = new List<GameObject>();
    public PlayerObject myPlayer;
    public GameObject myTaskHolderList;
    public GameObject newTaskObject;
    public void BuildMyTaskList()
    {
        foreach (var obj in myTaskObjects)
        {
            Destroy(obj);
        }
        foreach (var item in myTaskChecks)
        {
            TaskUI newTask = new TaskUI();
            newTask.myTask = item;
            if (canTakeNewTask(newTask))
                myTasks.Add(newTask);
        }
        myTaskChecks = myTaskChecks.Distinct().ToList();
        myTasks = myTasks.Distinct().ToList();
        UpdateMyUI();
    }
    public void UpdateMyUI()
    {
        foreach (var obj in myTaskObjects)
        {
            Destroy(obj);
        }
        foreach (var data in myTasks)
        {
            if (canTakeNewTask(data))
            {
                GameObject newTaskUIObj = Instantiate(newTaskObject, myTaskHolderList.transform);
                TaskUI newVals = newTaskUIObj.GetComponent<TaskUI>();
                newVals.taskTitle = data.taskTitle;
                newVals.taskDescription = data.taskDescription;
                newVals.numberCompleted = data.numberCompleted;
                newVals.taskNumberCompleted = data.taskNumberCompleted;
                newVals.taskNumToCompletion = data.taskNumToCompletion;
                newVals.myPlayer = myPlayer;
                myTaskObjects.Add(newTaskUIObj);
            }
        }
        myPlayer.myTasks = myTasks;
        foreach (var item in myTasks)
        {
            item.GenerateTaskUI();
        }
    }
    public bool canTakeNewTask(TaskUI newTask)
    {
        bool output = (!myTaskChecks.Contains(newTask.myTask));
        if (!output)
        {
            myTaskChecks.Remove(newTask.myTask);
        }
        return output;
    }
}


#endregion