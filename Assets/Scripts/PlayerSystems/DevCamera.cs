using UnityEngine;
using UnityEngine.InputSystem;

public class DevCamera : MonoBehaviour {
    [Header("Camera Toggling")]
    public Camera playerCamera; // The camera attached to your player
    public Camera devCamera;    // A separate camera placed anywhere in the scene

    [Header("Player Scripts")]
    public PlayerController playerMovement;
    public CameraController mouseLook;

    [Header("Settings")]
    public float flySpeed = 20f;
    public float sprintMultiplier = 3f;

    [Header("Input Actions")]
    public InputActionReference toggleAction;  // Tab key
    public InputActionReference moveAction;    // Vector2 (WASD)
    public InputActionReference elevateAction; // 1D Axis (Q/E)
    public InputActionReference lookAction;    // Vector2 (Mouse Delta)
    public InputActionReference sprintAction;  // Button (Left Shift)

    private bool isDevMode = false;
    private float yaw = 0f;
    private float pitch = 0f;

    private void Start() {
        // Start game with DevCam off and PlayerCam on
        devCamera.enabled = false;
        playerCamera.enabled = true;

        yaw = devCamera.transform.eulerAngles.y;
        pitch = devCamera.transform.eulerAngles.x;
    }

    private void Update() {
        // 1. Toggle Mode
        if (toggleAction != null && toggleAction.action.WasPressedThisFrame()) {
            ToggleDevMode();
        }

        // 2. Handle Flying Logic (Only if active)
        if (isDevMode) {
            MoveCamera();
            RotateCamera();
        }
    }

    private void ToggleDevMode() {
        isDevMode = !isDevMode;

        // Turn scripts on/off
        if (playerMovement != null) playerMovement.enabled = !isDevMode;
        if (mouseLook != null) mouseLook.enabled = !isDevMode;

        // Swap Cameras!
        playerCamera.enabled = !isDevMode;
        devCamera.enabled = isDevMode;

        if (isDevMode) {
            // Optional: Snap the dev camera to the player's head when activated
            devCamera.transform.position = playerCamera.transform.position;
            devCamera.transform.rotation = playerCamera.transform.rotation;

            yaw = devCamera.transform.eulerAngles.y;
            pitch = devCamera.transform.eulerAngles.x;
        }
    }

    private void MoveCamera() {
        // Read Inputs
        Vector2 moveInput = moveAction.action.ReadValue<Vector2>();
        float elevateInput = elevateAction.action.ReadValue<float>();
        bool isSprinting = sprintAction.action.IsPressed();

        float speed = flySpeed * (isSprinting ? sprintMultiplier : 1f);

        // Calculate direction relative to where the dev camera is looking
        Vector3 moveDir = (devCamera.transform.forward * moveInput.y) +
                          (devCamera.transform.right * moveInput.x) +
                          (Vector3.up * elevateInput);

        devCamera.transform.position += moveDir * speed * Time.deltaTime;
    }

    private void RotateCamera() {
        Vector2 lookInput = lookAction.action.ReadValue<Vector2>();

        yaw += lookInput.x;
        pitch -= lookInput.y;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        devCamera.transform.eulerAngles = new Vector3(pitch, yaw, 0f);
    }
    private void OnEnable() {
        if (toggleAction != null) toggleAction.action.Enable();
        if (moveAction != null) moveAction.action.Enable();
        if (elevateAction != null) elevateAction.action.Enable();
        if (lookAction != null) lookAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
    }
    private void OnDisable() {
        if (toggleAction != null) toggleAction.action.Disable();
        if (moveAction != null) moveAction.action.Disable();
        if (elevateAction != null) elevateAction.action.Disable();
        if (lookAction != null) lookAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
    }
}