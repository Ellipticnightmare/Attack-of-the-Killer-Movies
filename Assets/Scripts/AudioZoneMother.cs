using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioZoneMother : MonoBehaviour
{
    private int sum;
    // Start is called before the first frame update
    void Start()
    {
        sum = 0;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Addtosum()
    {
        sum++;
    }
    public void Removefromsum()
    {
        sum--;
    }
    public int Getsum()
    {
        return sum;
    }
    
}
