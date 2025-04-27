using Cinemachine;
using System;
using UnityEngine;
using UnityEngine.Events;
using static PlayerMovementAdvanced;

public class CameraHeadBob : MonoBehaviour
{

    [Range(0.01f, 0.1f)]
    public float baseAmount = 0.07f;

    [Range(1f, 30f)]
    public float baseFrequency = 10f;

    [Range(10f, 100f)]
    public float Smooth = 10.0f;

    private Vector3 originalLocalPosition;
    private Vector3 velocity = Vector3.zero;
    private PlayerMovementAdvanced playerMovement;

    public UnityEvent onFootStep;
    float Sin;
    bool isTriggered = false;

    void Start()
    {
        originalLocalPosition = transform.localPosition;
        playerMovement = FindObjectOfType<PlayerMovementAdvanced>();
    }

    void Update()
    {
        CheckForHeadbobTrigger();
        StopHeadBob();
    }
    private void CheckForHeadbobTrigger()
    {
        if (playerMovement == null) return;

        float inputMagnitude = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude;

        // Fix: Obtain a reference to the PlayerMovementAdvanced instance
        if (inputMagnitude > 0 && !playerMovement.IsCrouching && playerMovement.grounded)
        {
            StartHeadBob();
        }
    }
    private Vector3 StartHeadBob()
    {
        float moveSpeed = playerMovement.moveSpeed;
        float dynamicFrequency = baseFrequency * (moveSpeed / playerMovement.walkSpeed);
        float dynamicAmount = baseAmount * (moveSpeed / playerMovement.walkSpeed);
 

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
