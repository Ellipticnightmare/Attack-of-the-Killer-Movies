using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Balloon : MonoBehaviour
{
    public MonsterController Clown;
    public float clownTimer;
    public AudioClip soundEffect;
    public myAudio MyAudio;
    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<PlayerObject>())
        {
            Vector3 targetDir = other.transform.position;
            Vector3 myPosition = transform.position;
            if(!Physics.Linecast(myPosition, targetDir))
            {
                EnemyManager.instance.Balloons.balloonCount--;
                Clown.startHuntTrigger(this.transform.position);
                Clown.clownTimer = clownTimer;
                Clown.targPlayer = other.GetComponent<PlayerObject>();
                SFXManager.instance.PlaySound(MyAudio.mySound, transform.position);
                Destroy(this.gameObject);
            }
        }
    }
}