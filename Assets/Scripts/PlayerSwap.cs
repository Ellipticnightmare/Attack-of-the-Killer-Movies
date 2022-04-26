using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSwap : MonoBehaviour
{
    public Transform player;
    public List<Transform> possiblePlayers;
    public int whichPlayer;
    InputHandler inputHandler;
    // Start is called before the first frame update
    void Start()
    {
        inputHandler = GetComponent<InputHandler>();

        if (player == null && possiblePlayers.Count >= 1)
        {
            player = possiblePlayers[0];
        }
        Swap();
        whichPlayer = 0;
    }

    // Update is called once per frame
    void Update()
    {
        float delta = Time.deltaTime;
        inputHandler.TickInput(delta);
        

        if (inputHandler.isSwap)
        {
            if(whichPlayer < possiblePlayers.Count - 1)
            {
                whichPlayer += 1;
            }
            else if(whichPlayer == possiblePlayers.Count - 1)
            {
                whichPlayer = 0;
            }
            Swap();
        }
    }

    public void Swap()
    {
        player = possiblePlayers[whichPlayer];
        player.GetComponent<PlayerMovement>().enabled = true;
        for (int i = 0; i < possiblePlayers.Count; i++)
        {
            if(possiblePlayers[i] != player)
            {
                possiblePlayers[i].GetComponent<PlayerMovement>().enabled = false;
            }
        }
        inputHandler.isSwap = false;
    }
}
