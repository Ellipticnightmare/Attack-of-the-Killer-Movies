using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InteractBox : MonoBehaviour
{
    float startTime = 0f;
    float holdTime = 5.0f; // 5 seconds

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                startTime = Time.time;
            }

            if (Input.GetKeyDown(KeyCode.E))
                // check if the start time plus [holdTime] is more or equal to the current time.
                // If so, we held the button for [holdTime] seconds.
                if ((startTime + holdTime) >= Time.time)
                {
                    // other.gameObject.TaskSectionComplete();
                }
        }
    }

 

}

