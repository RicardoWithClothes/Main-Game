using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Actions")]
    // Drag your Input Action References here in the Inspector
    public InputActionReference moveAction;
    public InputActionReference jumpAction;
    public InputActionReference sprintAction;
    public InputActionReference crouchAction;

    // Public properties for other scripts to read
    public float Horizontal { get; private set; }
    public float Vertical { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintHeld { get; private set; }
    public bool CrouchHeld { get; private set; }
    public bool CrouchTriggered { get; private set; }

    private void Update()
    {
        Vector2 moveInput = Vector2.zero;
        if (moveAction != null && moveAction.action != null) {
            moveInput = moveAction.action.ReadValue<Vector2>();
        }

        Horizontal = moveInput.x;
        Vertical = moveInput.y;

        JumpTriggered = jumpAction != null && jumpAction.action.WasPressedThisFrame();
        SprintHeld = sprintAction != null && sprintAction.action.IsPressed();

        if (crouchAction != null && crouchAction.action != null) {
            CrouchHeld = crouchAction.action.IsPressed();
            CrouchTriggered = crouchAction.action.WasPressedThisFrame();
        }
    }
    private void OnEnable() {
        if (moveAction != null) moveAction.action.Enable();
        if (jumpAction != null) jumpAction.action.Enable();
        if (sprintAction != null) sprintAction.action.Enable();
        if (crouchAction != null) crouchAction.action.Enable();
    }

    private void OnDisable() {
        if (moveAction != null) moveAction.action.Disable();
        if (jumpAction != null) jumpAction.action.Disable();
        if (sprintAction != null) sprintAction.action.Disable();
        if (crouchAction != null) crouchAction.action.Disable();
    }
}
