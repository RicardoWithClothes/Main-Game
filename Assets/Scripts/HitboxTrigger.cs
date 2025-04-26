using UnityEngine;

public class HitboxTrigger : MonoBehaviour
{
    public CameraShake cameraShake;
    public float shakeDuration = 0.5f;
    public float shakeMagnitude = 0.3f;

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (other.CompareTag("Player"))
        {
            if (cameraShake != null)
            {
                StartCoroutine(cameraShake.Shake(shakeDuration, shakeMagnitude));
            }
            else
            {
                Debug.LogError("CameraShake reference is not set.");
            }
        }
    }
}