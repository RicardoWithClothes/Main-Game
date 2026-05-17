using UnityEngine;
using UnityEngine.InputSystem;


public class CameraController : MonoBehaviour
{
    [Header("Input Setup")]
    public InputActionReference lookAction; // Drag your 'Look' action here

    [Header("Settings")]
    public float sensX = 0.5f; // Note: You usually need to lower your sensitivity values
    public float sensY = 0.5f; // when removing Time.deltaTime!

    [Header("References")]
    public Transform orientation;

    private float xRotation;
    private float yRotation;
    private void Start()
    {
        Cursor.lockState=CursorLockMode.Locked;
        Cursor.visible=false;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 lookInput = Vector2.zero;
        if (lookAction != null && lookAction.action != null) {
            lookInput = lookAction.action.ReadValue<Vector2>();
        }

        float mouseX = lookInput.x * sensX;
        float mouseY = lookInput.y * sensY;

        yRotation += mouseX;
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.rotation = Quaternion.Euler(xRotation, yRotation, 0);
        orientation.rotation = Quaternion.Euler(0, yRotation, 0);
    }
    private void OnEnable() {
        if (lookAction != null) lookAction.action.Enable();
    }
    private void OnDisable() {
        if (lookAction != null) lookAction.action.Disable();
    }
}
