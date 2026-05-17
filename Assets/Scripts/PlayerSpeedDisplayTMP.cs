using UnityEngine;
using TMPro;

public class PlayerSpeedDisplayTMP : MonoBehaviour {
    public Rigidbody playerRigidbody;
    public TMP_Text speedText;

    void Update() {
        if (playerRigidbody == null || speedText == null) return;

        float speed = playerRigidbody.linearVelocity.magnitude;

        // NEW: Zero memory allocation! TMP handles the formatting internally.
        // {0:F2} means "Take the first variable (speed) and format it to 2 decimal places"
        speedText.SetText("Speed: {0:F2}", speed);
    }
}