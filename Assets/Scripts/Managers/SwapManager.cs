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
    public GameObject mainCam, topCam;
    #endregion
    private void Awake()
    {
        singleton = this;
        possibleActors.AddRange(FindObjectsOfType<PlayerObject>());
        if (Actor == null && possibleActors.Count >= 1)
            Actor = possibleActors[0];
    }
    private void Start()
    {
        mainCam.SetActive(false);
    }
    public void SwapTo()
    {
        Actor.enableControl();
        swapUI.SetActive(false);
        Actor = null;
    }
    public void StartSwap(PlayerObject oldActor)
    {
        oldActor.disableControl();
        swapUI.SetActive(true);
        possibleActors.Clear();
        possibleActors.AddRange(FindObjectsOfType<PlayerObject>());
        while (mapButtons.Count > possibleActors.Count)
            mapButtons.Remove(mapButtons[mapButtons.Count - 1]);
        for (int i = 0; i < possibleActors.Count; i++)
        {
            int x = i;
            mapButtons[i].onClick.AddListener(delegate { SetNewTargetActor(x); });
            mapButtons[i].GetComponent<Image>().sprite = possibleActors[i].actorFace;
            mapButtons[i].transform.position = RectTransformUtility.WorldToScreenPoint(topCam.GetComponent<Camera>(), possibleActors[i].transform.position);
        }
    }
    public void SetNewTargetActor(int i) => Actor = possibleActors[i];
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