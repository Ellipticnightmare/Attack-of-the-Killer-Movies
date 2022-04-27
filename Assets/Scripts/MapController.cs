using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapController : MonoBehaviour
{
    InputHandler inputHandler;
    public GameObject gameMap;
    public List<GameObject> taskList;
    public PlayerSwap playerSwap;

    // Start is called before the first frame update
    void Start()
    {
        inputHandler = GetComponent<InputHandler>();
        playerSwap = GetComponent<PlayerSwap>();

    }

    // Update is called once per frame
    void Update()
    {
        float delta = Time.deltaTime;
        inputHandler.TickInput(delta);

        if (inputHandler.mapIsOpen == false)
        {
            OpenMap();
        }else if(inputHandler.mapIsOpen)
        {
            CloseMap();
        }
    }

    public void OpenMap()
    {
        inputHandler.mapIsOpen = true;
        playerSwap.player.GetComponent<PlayerMovement>().enabled = false;
        foreach(GameObject list in taskList)
        {
            list.GetComponent<Renderer>().enabled = false;
        }
        gameMap.SetActive(true);

        

    }

    public void CloseMap()
    {
        playerSwap.player.GetComponent<PlayerMovement>().enabled = true;
        foreach (GameObject list in taskList)
        {
            list.GetComponent<Renderer>().enabled = true;
        }
        gameMap.SetActive(false);
        inputHandler.mapIsOpen = false;
    }
}
