using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    public Text displayHighScore;
    public GameObject[] UIObjects;
    public bool allowCustom, allowSurvivor;
    public Button customStart, survivorStart;
    public Button[] customMonsterProfiles;
    List<string> selectedCustomMonsters = new List<string>();
    float difficulty;
    private void Awake()
    {
        displayHighScore.text = PlayerPrefs.HasKey("timeScore") ? (((int)PlayerPrefs.GetFloat("timeScore") / 60) + " : " + (PlayerPrefs.GetFloat("timeScore") % 60).ToString("f2")) : "No Highscore";
        if (PlayerPrefs.HasKey("survivedMonsters"))
            allowCustom = true;
        if (PlayerPrefs.GetInt("survivedCustomMonstersCheck") >= 10)
            allowSurvivor = true;
        UpdateGameModeUI();
    }
    public void UpdateGameModeUI()
    {
        customStart.enabled = allowCustom;
        survivorStart.enabled = allowSurvivor;
        string[] allowedCustomMonsters = PlayerPrefs.GetString("survivedMonsters").Split('|');
        List<string> allowedCustomMonstersCheck = new List<string>();
        allowedCustomMonstersCheck.AddRange(allowedCustomMonsters);
        foreach (var item in customMonsterProfiles)
        {
            item.onClick.AddListener(delegate { ToggleSelectMonster(item); });
            item.enabled = false;
            item.image.color = Color.black;
            if (allowedCustomMonstersCheck.Contains(item.name))
            {
                item.enabled = true;
                item.image.color = Color.white;
            }
        }
    }
    public void ToggleSelectMonster(Button inName)
    {
        if (!selectedCustomMonsters.Contains(inName.name) && selectedCustomMonsters.Count < 4)
        {
            inName.image.color = Color.green;
            selectedCustomMonsters.Remove(inName.name);
        }
        else if (selectedCustomMonsters.Contains(inName.name))
        {
            inName.image.color = Color.white;
            selectedCustomMonsters.Remove(inName.name);
        }
    }
    public void UpdateDifficulty(float newValue)
    {
        difficulty = newValue;
    }
    public void HitOpenUI(GameObject UIobj) //closes other UI windows, opens new UI window
    {
        GameObject UIObject = new GameObject();
        foreach (var obj in UIObjects)
        {
            obj.SetActive(false);
            if (obj == UIobj)
                UIobj.SetActive(true);
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
                UIobj.SetActive(true);
        }
    }
    public void HitLoadScene(string scene)
    {
        PlayerPrefs.SetString("gameMode", scene);
        if (scene == "Custom")
        {
            PlayerPrefs.SetString("enemy01", selectedCustomMonsters[0]);
            PlayerPrefs.SetString("enemy02", selectedCustomMonsters[1]);
            PlayerPrefs.SetString("enemy03", selectedCustomMonsters[2]);
            PlayerPrefs.SetFloat("difficultyCheck", difficulty);
        }
        if (scene != "Custom" || selectedCustomMonsters.Count >= 3)
            SceneManager.LoadScene(scene);
    }
    public void HitBackButton(GameObject UIobj) //closes current UI window
                                                //if no variable assigned, quits game
    {
        if (UIobj == null)
            Application.Quit();
        GameObject UIObject = new GameObject();
        foreach (var obj in UIObjects)
        {
            obj.SetActive(false);
            if (obj == UIobj)
                UIobj.SetActive(true);
        }
    }
}