using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableThisAfterTime : MonoBehaviour
{
    // Start is called before the first frame update
    public float time;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
    }
    public void Die()
    {
        Destroy(this.gameObject);
    }
}
