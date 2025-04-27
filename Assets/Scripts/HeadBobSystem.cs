using Cinemachine;
using UnityEngine;

public class CameraHeadBob : MonoBehaviour
{

    [Range(0.01f, 0.1f)]
    public float Amount = 0.07f;

    [Range(1f, 30f)]
    public float Frequency = 10f;

    [Range(10f, 100f)]
    public float Smooth = 10.0f;

    private Vector3 originalLocalPosition;
    private Vector3 velocity = Vector3.zero;
    void Start()
    {
        originalLocalPosition = transform.localPosition;
    }

    void Update()
    {
        CheckForHeadbobTrigger();
        StopHeadBob();
    }
    private void CheckForHeadbobTrigger()
    {
        float inputMagnitude = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical")).magnitude;

        if (inputMagnitude > 0) 
        { 
            StartHeadBob();
        }
    }
    private Vector3 StartHeadBob()
    {
        Vector3 pos = Vector3.zero;
        pos.y += Mathf.Lerp(pos.y, Mathf.Sin(Time.time * Frequency) * Amount * 1.4f, Smooth * Time.deltaTime);
        pos.x += Mathf.Lerp(pos.x, Mathf.Cos(Time.time * Frequency / 2f) * Amount * 1.6f, Smooth * Time.deltaTime);
        transform.localPosition += pos;

        return pos;
    }
    private void StopHeadBob() 
    {
        if (transform.localPosition == originalLocalPosition) return;
        transform.localPosition = Vector3.Lerp(transform.localPosition, originalLocalPosition, 1 * Time.deltaTime);
    }
}
