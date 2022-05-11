using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioZone : MonoBehaviour
{
    // Start is called before the first frame update
    public AudioSource buzzSound;
    public AudioZoneMother mother;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeSinceLevelLoad < 0.1f)
        {
            buzzSound.Stop();
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            mother.Addtosum();
        }
        if (other.tag == "Player" && !buzzSound.isPlaying)
        {
            buzzSound.Play();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        
        if (other.tag == "Player")
        {
            mother.Removefromsum();
        }
        if (other.tag == "Player" && buzzSound.isPlaying && mother.Getsum() < 1)
        {
            buzzSound.Stop();
        }
    }
}
