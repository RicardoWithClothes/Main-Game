using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateblock : MonoBehaviour
{
    public float xspeed = 0.5f;
    public float yspeed = 0.5f;
    public float zspeed = 0.5f;
    // Update is called once per frame
    void Update()
    {
        transform.Rotate(
         xspeed * Time.deltaTime,
         yspeed * Time.deltaTime,
         zspeed * Time.deltaTime);
    }
}
    
