using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public Text displayHighScore;
    public GameObject[] UIObjects;
    private void Awake()
    {
        displayHighScore.text = PlayerPrefs.HasKey("timeScore") ? (((int)PlayerPrefs.GetFloat("timeScore") / 60) + " : " + (PlayerPrefs.GetFloat("timeScore") % 60).ToString("f2")) : "No Highscore";
    }
    public void HitOpenUI(GameObject UIobj) //closes other UI windows, opens new UI window
    {
        GameObject UIObject = new GameObject();
        foreach (var obj in UIObjects)
        {
            obj.SetActive(false);
            if (obj == UIobj)
                UIobj.SetActive(false);
        }
    }
    public void HitOpenUIWindow(GameObject UIobj) //opens new UI window over the existing windows
                                                  //Use this for Settings sub-menus, or pop-ups that
                                                  //add quality-of-life

                                                  //Also use this for the Play and Quit submenus
    {
        GameObject UIObject = new GameObject();
        foreach (var obj in UIObjects)
        {
            if (obj == UIobj)
                UIobj.SetActive(false);
        }
    }
    public void HitLoadScene(string scene) => SceneManager.LoadScene(scene);
    public void HitBackButton(GameObject UIobj) //closes current UI window
                                                //if no variable assigned, quits game
    {
        if (UIobj == null)
            Application.Quit();
        GameObject UIObject = new GameObject();
        foreach (var obj in UIObjects)
        {
            if (obj == UIobj)
                UIobj.SetActive(false);
        }
    }
}