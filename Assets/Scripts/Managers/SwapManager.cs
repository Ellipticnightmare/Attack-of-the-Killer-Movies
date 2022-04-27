using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwapManager : MonoBehaviour
{
    public PlayerObject Actor;
    public List<PlayerObject> possibleActors = new List<PlayerObject>();
    public static SwapManager singleton;

    #region UI
    public GameObject showProfileUI;
    public Image healthIndicator, heldItem;
    public Sprite[] healthTiers;
    public RawImage liveView;
    public List<Button> mapButtons = new List<Button>();
    public GameObject swapUI;
    public Camera mapRenderCam;
    #endregion
    private void Start()
    {
        singleton = this;
        possibleActors.AddRange(FindObjectsOfType<PlayerObject>());
        if (Actor == null && possibleActors.Count >= 1)
            Actor = possibleActors[0];
    }
    public void SwapTo()
    {
        Actor.enableControl();
        Actor = null;
    }
    public void StartSwap(PlayerObject oldActor)
    {
        oldActor.disableControl();
        swapUI.SetActive(true);
        while (mapButtons.Count > possibleActors.Count)
            mapButtons.Remove(mapButtons[mapButtons.Count - 1]);
        for (int i = 0; i < possibleActors.Count; i++)
        {
            mapButtons[i].GetComponent<Image>().sprite = possibleActors[i].actorFace;
            mapButtons[i].transform.position = RectTransformUtility.WorldToScreenPoint(mapRenderCam, possibleActors[i].transform.position);
            mapButtons[i].onClick.AddListener(delegate { SetNewTargetActor(possibleActors[i]); });
        }
    }
    public void SetNewTargetActor(PlayerObject newActor) => Actor = newActor;
    public void ConfirmSwapActor() => SwapTo(); //Set this in scene as a Confirm Selection button
    public void RejectSwapActor() => Actor = null;
    private void Update()
    {
        if(Actor != null)
        {
            showProfileUI.SetActive(true);
            liveView.texture = Actor.showUICam;
            switch (Actor.playerState)
            {
                case PlayerObject.PlayerState.Healthy:
                    healthIndicator.sprite = healthTiers[0];
                    break;
                case PlayerObject.PlayerState.Injured:
                    healthIndicator.sprite = healthTiers[1];
                    break;
                case PlayerObject.PlayerState.Crippled:
                    healthIndicator.sprite = healthTiers[2];
                    break;
                case PlayerObject.PlayerState.Dead:
                    healthIndicator.sprite = healthTiers[3];
                    break;
            }
        }
        else
        {
            showProfileUI.SetActive(false);
        }
    }
}