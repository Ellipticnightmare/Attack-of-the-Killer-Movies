using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public Text timerText;
    static float timeSpent;
    public static float tMod;
    static bool isPaused = true;
    string minutes, seconds;
    public Color timerPauseColor, timerRunColor;
    public GameObject endGameScreenUI;
    static GameObject s_endGameScreenUI;
    private void Awake()
    {
        s_endGameScreenUI = endGameScreenUI;
        tMod = 0;
        timeSpent = Time.time;
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
    public static void FinishedGame()
    {
        if (PlayerPrefs.GetFloat("timeScore") >= timeSpent)
            PlayerPrefs.SetFloat("timeScore", timeSpent);
        tMod = 0;
        s_endGameScreenUI.SetActive(true);
    }
    public static void PlayerDied()
    {
        TaskManager.PlayerDied();
    }
    public static void togglePause()
    {
        isPaused = !isPaused;
        tMod = isPaused ? 0 : 1;
        Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isPaused;
    }
}