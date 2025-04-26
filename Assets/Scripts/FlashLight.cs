using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlashLightController : MonoBehaviour
{
    public CameraController cameraController; // Reference to the camera controller script
    public float flashlightOffset = 0f; // Offset if you want to adjust the flashlight's starting position
    public float lagSpeed = 5f; // Speed of the lag (higher value = less lag)

    private Quaternion targetRotation;

    void Update()
    {
        // Get the xRotation from the camera controller
        float flashlightYRotation = cameraController.xRotation + flashlightOffset;
        float flashlightXRotation = cameraController.yRotation + flashlightOffset;

        // Set the target rotation for the flashlight
        targetRotation = Quaternion.Euler(flashlightYRotation, flashlightXRotation, transform.localRotation.z);

        // Interpolate the flashlight rotation towards the target rotation
        transform.localRotation = Quaternion.Lerp(transform.localRotation, targetRotation, Time.deltaTime * lagSpeed);
    }
}
