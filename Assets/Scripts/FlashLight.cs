using UnityEngine;
using UnityEngine.InputSystem;

public class FlashLightController : MonoBehaviour
{
    [Header("Input Setup")]
    public InputActionReference toggleAction;
    [Header("Settings")]
    public float lagSpeed = 5f;


    // maybe offset
    public Vector3 positionOffset = new Vector3(0.5f, -0.5f, 0f);

    [Header("References")]
    public Transform cameraTransform;

    private Light flashlight;

    private void Update() {
        if (toggleAction != null && toggleAction.action != null && toggleAction.action.WasPressedThisFrame()) {
            ToggleFlashlight();
        }
    }

    // THE FIX: LateUpdate runs AFTER the CameraController finishes moving
    private void LateUpdate() {
        if (cameraTransform != null) {
            // 1. Instantly snap to the camera's position (plus our shoulder offset)
            transform.position = cameraTransform.TransformPoint(positionOffset);

            // 2. Smoothly lag the rotation on ALL axes!
            transform.rotation = Quaternion.Lerp(transform.rotation, cameraTransform.rotation, Time.deltaTime * lagSpeed);
        }
    }
    private void ToggleFlashlight()
    {
        // Toggle the flashlight on/off
        if (flashlight != null) {
            flashlight.enabled = !flashlight.enabled;
        }
    }
    private void Awake() {
        // We grab the Light component once when the game starts, rather than every time you press F.
        flashlight = GetComponent<Light>();
        transform.SetParent(null);
    }
    private void OnEnable() {
        // Replace 'toggleAction' or 'lookAction' with whatever you named your variables!
        if (toggleAction != null) toggleAction.action.Enable();
    }

    private void OnDisable() {
        if (toggleAction != null) toggleAction.action.Disable();
    }
}
