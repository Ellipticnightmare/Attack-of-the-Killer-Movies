using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundPoint : MonoBehaviour
{
    float lifeTime;
    public void Initialize(float _lifeTime)
    {
        lifeTime = _lifeTime;
    }
    void Update()
    {
        if (lifeTime > 0)
            lifeTime -= Time.deltaTime;
        else
            Destroy(this.gameObject);
    }
}