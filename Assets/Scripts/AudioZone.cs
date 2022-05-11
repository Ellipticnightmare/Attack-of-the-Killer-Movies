using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioZone : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource buzzSound;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player" && !buzzSound.isPlaying)
        {
            buzzSound.Play();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player" && buzzSound.isPlaying)
        {
            buzzSound.Stop();
        }
    }
}
