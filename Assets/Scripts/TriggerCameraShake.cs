using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShakeTrigger : MonoBehaviour
{
    public KeyCode screamKey = KeyCode.G;
    public ParticleSystem screamer;
    public CameraShake cameraShake;
    void Update()
    {
        if (Input.GetKey(screamKey))
        {
            screamer.Play();
            StartCoroutine(cameraShake.Shake(0.15f, 0.4f));
        }    
    }
}
