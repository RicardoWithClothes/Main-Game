using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(SplineContainer))]
public class ProceduralSplineGenerator : MonoBehaviour {
    private SplineContainer splineContainer;

    void Start() {
        splineContainer = GetComponent<SplineContainer>();
        GeneratePath();
    }

    void GeneratePath() {
        // Get the primary spline from the container
        Spline spline = splineContainer.Spline;
        spline.Clear();

        // 1. Add starting point
        spline.Add(new BezierKnot(
            new float3(0f, 0f, 0f),               // Position
            new float3(0f, 0f, -2f),              // In-tangent
            new float3(0f, 0f, 2f),               // Out-tangent
            quaternion.identity                   // Rotation
        ));

        // 2. Add an intermediate curved point
        spline.Add(new BezierKnot(
            new float3(5f, 2f, 5f),
            new float3(-2f, 0f, 0f),
            new float3(2f, 0f, 0f),
            quaternion.identity
        ));

        // 3. Add an ending point
        spline.Add(new BezierKnot(
            new float3(10f, 0f, 10f),
            new float3(-2f, 0f, 0f),
            new float3(2f, 0f, 0f),
            quaternion.identity
        ));

        // Set the tangent mode to auto smooth the curves if preferred
        spline.SetTangentMode(0, TangentMode.AutoSmooth);
        spline.SetTangentMode(1, TangentMode.AutoSmooth);

        // Optional: Close the loop
        // spline.Closed = true;
    }
}