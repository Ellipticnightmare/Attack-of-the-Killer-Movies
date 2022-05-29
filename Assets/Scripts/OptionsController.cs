using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class OptionsController : MonoBehaviour
{

    public Slider mouseSensitivitySlider;
    public Slider volumeSlider;
    // Start is called before the first frame update
    void Start()
    {
        if (PlayerPrefs.HasKey("Sensitivity"))
            mouseSensitivitySlider.value = PlayerPrefs.GetFloat("Sensitivity");
        else
            SetMouseSensitivity(.1f);
        volumeSlider.value = PlayerPrefs.GetFloat("Volume");
    }

    // Update is called once per frame
    public void SetMouseSensitivity(float val)
    {
        PlayerPrefs.SetFloat("Sensitivity", val);
        Debug.Log("Sensitivity " + val);
    }

    public void SetVolume(float volume)
    {
        PlayerPrefs.SetFloat("Volume", volume);
        AudioListener.volume = PlayerPrefs.GetFloat("Volume");
    }
}
