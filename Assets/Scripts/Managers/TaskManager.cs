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
    List<PlayerObject> activePlayers = new List<PlayerObject>();

    public GameObject taskObject; //reference to prefab that will show all task information
    public int maxDisplayTasks = 6;
    void Start()
    {
        activePlayers.AddRange(FindObjectsOfType<PlayerObject>());
        instance = this;
        allTasks.AddRange(myTaskDatabase.allTasks);
        TaskObject[] allObjects = FindObjectsOfType<TaskObject>();
        allTaskObjects.AddRange(allObjects);
        CreatePlayerLists();
    }
    public void PlayerDied(PlayerObject player) => StartCoroutine(RedelegateTasks(player));
    public void RemoveFromTasks(PlayerObject player, Task task)
    {
        Debug.Log("Removing");
        activePlayers.AddRange(FindObjectsOfType<PlayerObject>());
        for (int i = 0; i < player.myTasks.Count; i++)
        {
            if (player.myTasks[i].title == task.title)
                player.myTasks.Remove(player.myTasks[i]);
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
    public void CreatePlayerLists() => 
        StartCoroutine(CreateTaskLists());
    public void UpdateWorld() =>
        SwapManager.singleton.SwapTo();
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
        yield return new WaitForEndOfFrame();
        UpdateWorld();
        if(!GameManager.instance.storyMode)
        GameManager.instance.togglePause();
    }
    IEnumerator RedelegateTasks(PlayerObject player)
    {
        List<PlayerObject> curLivingPlayers = new List<PlayerObject>();
        curLivingPlayers.AddRange(FindObjectsOfType<PlayerObject>());
        yield return new WaitForEndOfFrame();
        PlayerObject diedPlayer = player;
        
        yield return new WaitForEndOfFrame();
        activePlayers.Remove(diedPlayer);
        yield return new WaitForEndOfFrame();
        List<Task> newTasks = diedPlayer.myTasks;
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
            activePlayers[i].myTasks.AddRange(listOfLists[i]);
        }
        yield return new WaitForEndOfFrame();
        Destroy(player.gameObject);
        FindObjectOfType<SwapManager>().StartSwap(player);
        UpdateWorld();
    }
}
[System.Serializable]
public class TaskUI
{
    public Text taskName, taskDescription;
}