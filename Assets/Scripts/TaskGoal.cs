using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TaskGoal
{
    public GoalType goalType;

    public int requiredAmount;
    public int currentAmount;

    public bool isReached()
    {
        return (currentAmount >= requiredAmount);
    }

    public void TaskSectionComplete()//Tasks can be tagged and that tagged used as an argument here
    {
        // For tasks that require more than one objective 
        if (goalType == GoalType.Interact)
        {
            currentAmount++;
        }
    }

    public void ItemFetched()
    {
        if (goalType == GoalType.Fetch)
        {
            currentAmount++;
        }
    }

}

public enum GoalType
{
    Fetch,
    Interact
}