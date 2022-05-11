using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SFXManager : MonoBehaviour
{
    public static SFXManager instance;
    public AudioSource[] SFXsources;
    public void PlaySound(AudioClip x, Vector3 y) => StartCoroutine(FireSound(x, y));
    public IEnumerator FireSound(AudioClip effect, Vector3 worldSpace)
    {
        for (int i = 0; i < SFXsources.Length; i++)
        {
            if (!SFXsources[i].isPlaying)
            {
                SFXsources[i].transform.position = worldSpace;
                SFXsources[i].clip = effect;
                SFXsources[i].Play();
                yield return new WaitForEndOfFrame();
                break;
            }
        }
    }
}
[System.Serializable]
public class myAudio
{
    public AudioClip mySound;
    public Transform mySource;
    public bool hasSound;
}