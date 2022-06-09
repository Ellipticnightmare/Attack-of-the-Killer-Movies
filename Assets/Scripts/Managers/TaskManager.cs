using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour
{
    public static TaskManager instance;
    List<TaskObject> allTaskObjects = new List<TaskObject>();
    public TaskDatabase _TD;
    [HideInInspector]
    public List<Task> allTasks = new List<Task>();
    List<Task> activeTasks = new List<Task>();
    List<PlayerObject> activePlayers = new List<PlayerObject>();
    public GameObject taskObj, endingTaskObj;
    // Start is called before the first frame update
    void Start()
    {
        activePlayers.AddRange(FindObjectsOfType<PlayerObject>());
        instance = this;
        allTasks.AddRange(_TD.allTasks);
        TaskObject[] allObjects = FindObjectsOfType<TaskObject>();
        allTaskObjects.AddRange(allObjects);
        CreatePlayerLists();
    }
    public void PlayerDied(PlayerObject player) => StartCoroutine(RedelegateTasks(player));
    public void RemoveFromTasks(PlayerObject player, Task task)
    {
        activePlayers.AddRange(FindObjectsOfType<PlayerObject>());
        player.myTasks.Remove(task);
        bool canWin = true;
        foreach (var item in activePlayers)
        {
            if (item.myTasks.Count >= 1)
                canWin = false;
        }
        if (canWin)
            endingTaskObj.SetActive(true);
    }
    public void CreatePlayerLists() => StartCoroutine(CreateTaskLists());
    public void UpdateWorld() => SwapManager.singleton.SwapTo();
    IEnumerator CreateTaskLists()
    {
        float rCheck = Random.Range(allTaskObjects.Count / 3, allTasks.Count);
        allTasks = allTasks.OrderBy(x => Random.value).ToList();
        int y = 0;
        for (int i = 0; i < rCheck; i++)
        {
            activePlayers[y].gainedTask(allTasks[i]);
            y = WrapAround(y + 1, 3);
        }
        yield return new WaitForEndOfFrame();
        UpdateWorld();
        if (!GameManager.instance.storyMode)
            GameManager.instance.togglePause();
    }
    IEnumerator RedelegateTasks(PlayerObject player)
    {
        List<Task> newTasks = player.myTasks;
        int y = 0;
        List<PlayerObject> curPlayers = new List<PlayerObject>();
        curPlayers.AddRange(FindObjectsOfType<PlayerObject>());
        Destroy(player.gameObject);
        for (int i = 0; i < newTasks.Count; i++)
        {
            curPlayers[i].gainedTask(newTasks[i]);
            y = WrapAround(y + 1, curPlayers.Count);
        }
        yield return new WaitForEndOfFrame();
        UpdateWorld();
    }
    int WrapAround(int newCheck, int max)
    {
        if (newCheck <= max)
            return newCheck;
        else
            return 0;
    }
}
[System.Serializable]
public class TaskUI
{
    public Text taskName, taskDescription;
}