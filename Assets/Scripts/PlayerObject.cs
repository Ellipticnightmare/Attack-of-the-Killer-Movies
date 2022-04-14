using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerObject : MonoBehaviour
{
    public GameObject myCanvas, myTaskListHolder;
    [HideInInspector]
    public List<TaskUI> myTasks = new List<TaskUI>();
    public PlayerState playerState = PlayerState.Healthy;

    public enum PlayerState
    {
        Healthy,
        Injured,
        Crippled,
        Dead
    };
}