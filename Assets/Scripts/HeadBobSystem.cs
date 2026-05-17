using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class CameraHeadBob : MonoBehaviour
{
    [Range(0.01f, 0.1f)]
    public float baseAmount = 0.07f;

    [Range(1f, 30f)]
    public float baseFrequency = 10f;

    [Range(10f, 100f)]
    public float Smooth = 10.0f;

    [Header("Input Settings")]
    public InputActionReference moveAction;

    private Vector3 originalLocalPosition;
    private PlayerController playerController;

    public UnityEvent onFootStep;
    float Sin;
    bool isTriggered = false;

    void Start()
    {
        originalLocalPosition = transform.localPosition;

        // CHANGE 2: Find the new script
        playerController = UnityEngine.Object.FindFirstObjectByType<PlayerController>();
    }

    void Update()
    {
        CheckForHeadbobTrigger();
        StopHeadBob();
    }

    private void CheckForHeadbobTrigger()
    {
        if (playerController == null) return;

        Vector2 moveInput = Vector2.zero;
        if (moveAction != null && moveAction.action != null) {
            moveInput = moveAction.action.ReadValue<Vector2>();
        }
        float inputMagnitude = moveInput.magnitude;
        if (inputMagnitude > 0 && !playerController.IsCrouching && playerController.isGrounded) {
            StartHeadBob();
        }
    }

    private Vector3 StartHeadBob()
    {
        // CHANGE 4: Get real velocity and stats from the ScriptableObject
        float actualSpeed = playerController.CurrentSpeed;
        float baseWalkSpeed = playerController.stats.walkSpeed;

        float dynamicFrequency = baseFrequency * (actualSpeed / baseWalkSpeed);
        float dynamicAmount = baseAmount * (actualSpeed / baseWalkSpeed);

        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * dynamicFrequency) * dynamicAmount * 1.4f, Smooth * Time.deltaTime);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * dynamicFrequency / 2f) * dynamicAmount * 1.6f, Smooth * Time.deltaTime);

        transform.localPosition += pos;

        Sin = Mathf.Sin(Time.time * dynamicFrequency);

        if (Sin > 0.97f && isTriggered == false)
        {
            isTriggered = true;
            onFootStep.Invoke();
        }
        else if (isTriggered == true && Sin < -0.97f)
        {
            isTriggered = false;
        }
        return pos;
    }

    private void StopHeadBob()
    {
        if (transform.localPosition == originalLocalPosition) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, 1 * Time.deltaTime);
    }
}