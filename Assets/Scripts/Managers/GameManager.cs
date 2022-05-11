using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    public Text timerText;
    float timeSpent;
    public static float tMod;
    static bool isPaused = true;
    string minutes, seconds;
    public Color timerPauseColor, timerRunColor;
    public GameObject endGameScreenUI;
    GameObject s_endGameScreenUI;
    public GameObject SoundPoint, startInstructions;
    public bool storyMode = false;
    private void Awake()
    {
        instance = this;
        s_endGameScreenUI = endGameScreenUI;
        tMod = 0;
        timeSpent = Time.time;
        if (PlayerPrefs.GetString("gameMode") == "Story")
        {
            startInstructions.SetActive(true);
            storyMode = true;
        }
    }
    private void FixedUpdate()
    {
        timeSpent += Time.deltaTime * tMod;
        minutes = ((int)timeSpent / 60).ToString();
        seconds = (timeSpent % 60).ToString("f2");

        timerText.text = minutes + ":" + seconds;
        timerText.color = isPaused ? timerPauseColor : timerRunColor;
    }
    public void ReturnToMainMenu() => SceneManager.LoadScene("MainMenu");
    public void ReloadScene() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    public void QuitGame() => Application.Quit();
    public void FinishedGame()
    {
        if (PlayerPrefs.GetFloat("timeScore") >= timeSpent)
            PlayerPrefs.SetFloat("timeScore", timeSpent);
        tMod = 0;
        string[] checkEnemyNames = new string[0];
        switch (EnemyManager.instance.gameMode)
        {
            case "Story":
                checkEnemyNames = PlayerPrefs.GetString("survivedMonsters").Split('|');
                break;
            case "Custom":
                checkEnemyNames = PlayerPrefs.GetString("survivedCustomMonsters").Split('|');
                break;
        }
        List<string> survivedCheck = new List<string>();
        survivedCheck.AddRange(checkEnemyNames);
        if (!survivedCheck.Contains(PlayerPrefs.GetString("enemy01")))
            survivedCheck.Add(PlayerPrefs.GetString("enemy01"));
        if (!survivedCheck.Contains(PlayerPrefs.GetString("enemy02")))
            survivedCheck.Add(PlayerPrefs.GetString("enemy02"));
        if (!survivedCheck.Contains(PlayerPrefs.GetString("enemy03")))
            survivedCheck.Add(PlayerPrefs.GetString("enemy03"));
        PlayerPrefs.SetInt("survivedCustomMonstersCheck", survivedCheck.Count + PlayerPrefs.GetInt("difficultyCheck"));
        string[] combinerArray = survivedCheck.ToArray();
        string combinedString = "";
        foreach (var item in combinerArray)
        {
            combinedString = "|" + combinedString + item;
        }
        combinedString.Remove(0, 1);
        switch (EnemyManager.instance.gameMode)
        {
            case "Story":
                PlayerPrefs.SetString("survivedMonsters", combinedString);
                break;
            case "Custom":
                PlayerPrefs.SetString("survivedCustomMonsters", combinedString);
                break;
        }
        s_endGameScreenUI.SetActive(true);
    }
    public void PlayerDied(PlayerObject player)
    {
        Debug.Log("PlayerDied");
        TaskManager.instance.PlayerDied(player);
    }
    public static void togglePause()
    {
        isPaused = !isPaused;
        tMod = isPaused ? 0 : 1;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }

    public void CloseStartUI()
    {
        isPaused = false;
        tMod = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        startInstructions.SetActive(false);
        storyMode = false;
        Debug.Log(storyMode);
    }
}