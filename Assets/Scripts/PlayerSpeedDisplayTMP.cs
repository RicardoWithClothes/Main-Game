using Palmmedia.ReportGenerator.Core.Parser.Analysis;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerSpeedDisplayTMP : MonoBehaviour
{
    public Rigidbody playerRigidbody; // Reference to the player's Rigidbody
    public TMP_Text speedText; // Reference to the TextMeshPro UI Text component

    void Update()
    {
        // Calculate the speed
        float speed = playerRigidbody.velocity.magnitude;

        // Update the TMP text with the speed
        speedText.text = "Speed: " + speed.ToString("F2");
    }
}