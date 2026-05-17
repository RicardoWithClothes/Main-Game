using UnityEngine;
using UnityEngine.InputSystem; // Required for the New Input System

public class CameraShakeTrigger : MonoBehaviour {
    [Header("Input Setup")]
    public InputActionReference screamAction; // Drop your "Scream" (G key) action here

    [Header("References")]
    public ParticleSystem screamer;
    public CameraShake cameraShake;

    private void Update() {
        // Use WasPressedThisFrame() so it only triggers exactly ONCE per button press,
        // preventing the game from spawning 60 coroutines a second!
        if (screamAction != null && screamAction.action.WasPressedThisFrame()) {
            TriggerScream();
        }
    }

    private void TriggerScream() {
        // Good practice to null check external references before firing them!
        if (screamer != null) {
            screamer.Play();
        }

        if (cameraShake != null) {
            StartCoroutine(cameraShake.Shake(0.15f, 0.4f));
        }
    }
}