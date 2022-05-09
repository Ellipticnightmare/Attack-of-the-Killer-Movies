using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager instance;
    List<TaskObject> allTaskObjects = new List<TaskObject>();
    public TaskDatabase myTaskDatabase;
    [HideInInspector]
    public List<Task> allTasks = new List<Task>();
    List<Task> activeTasks = new List<Task>();
    List<Task> completedTasks = new List<Task>();
    List<TaskUI> SelectedTaskUIs = new List<TaskUI>();
    public List<PlayerObject> ns_activePlayers = new List<PlayerObject>();
    List<PlayerObject> activePlayers = new List<PlayerObject>();

    public GameObject taskObject; //reference to prefab that will show all task information
    public int maxDisplayTasks = 6;
    void Start()
    {
        instance = this;
        allTasks.AddRange(myTaskDatabase.allTasks);
        TaskObject[] allObjects = FindObjectsOfType<TaskObject>();
        allTaskObjects.AddRange(allObjects);
        CreatePlayerLists();
    }
    public void PlayerDied(PlayerObject player) //called from GameManager when a player dies, unlocks all tasks
                                    //in case surviving players have already completed a task there,
                                    //thus preventing hard locks where players would be unable to re-do
                                    //a task
    {
        foreach (var item in allTaskObjects)
        {
            item.doesDiscriminateBetweenPlayers = false;
        }
        FindObjectOfType<TaskManager>().ReDelegateTasks(player);
    }
    public void ReDelegateTasks(PlayerObject player) => StartCoroutine(RedelegateTasks(player));
    public static void UpdatedTask(TaskUI goal)
    {
        goal.UpdateTaskUI();
    }
    public void RemoveFromTasks(TaskUI goal, TaskObject taskGoal)
    {
        foreach (var obj in activePlayers)
        {
            if (obj == goal.myPlayer)
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
            GameManager.instance.FinishedGame();
    }
    public void CreatePlayerLists()
    {
        PlayerObject[] players = FindObjectsOfType<PlayerObject>();
        foreach (var item in players)
        {
            PlayerObject newController = item.GetComponent<PlayerObject>();
            newController.newTaskObject = taskObject;
            ns_activePlayers.Add(newController);
        }
        activePlayers = ns_activePlayers;
        StartCoroutine(CreateTaskLists());
    }
    public void UpdateWorld()
    {
        /*foreach (var item in allTaskObjects)
        {
            if (activeTasks.Contains(item.thisTask))
                Destroy(item); //This can be changed to make the task objects static by changing to [item.enabled = false;]
        }*/
        SwapManager.singleton.SwapTo();
    }
    IEnumerator CreateTaskLists()
    {
        float rCheck = Random.Range(allTasks.Count / 3, allTasks.Count - 2);
        rCheck = (int)(rCheck / 4);
        rCheck = rCheck * 4;
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
            if (i % 2 == 0 || i == 0)
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
            if (i % 2 == 0 || i == 0)
                activePlayers[0].gainedTask(masterA[i]);
            else
                activePlayers[1].gainedTask(masterA[i]);
        }
        for (int i = 0; i < masterB.Count; i++)
        {
            if (i % 2 == 0 || i == 0)
            {
                activePlayers[2].gainedTask(masterB[i]);
            }
            else
                activePlayers[3].gainedTask(masterB[i]);
        }
        yield return new WaitForSeconds(1);
        yield return new WaitForEndOfFrame();
        foreach (var item in activePlayers)
        {
            item.BuildMyTaskList();
        }
        yield return new WaitForEndOfFrame();
        UpdateWorld();
        GameManager.togglePause();
    }
    IEnumerator RedelegateTasks(PlayerObject player)
    {
        List<PlayerObject> curLivingPlayers = new List<PlayerObject>();
        curLivingPlayers.AddRange(FindObjectsOfType<PlayerObject>());
        yield return new WaitForEndOfFrame();
        PlayerObject diedPlayer = null;
        foreach (var item in activePlayers)
        {
            if (!curLivingPlayers.Contains(item))
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
        Destroy(player.gameObject);
        FindObjectOfType<SwapManager>().StartSwap(player);
        UpdateWorld();
    }
}