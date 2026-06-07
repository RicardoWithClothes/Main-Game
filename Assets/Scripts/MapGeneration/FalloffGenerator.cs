using UnityEngine;
using System.Collections;

public static class FalloffGenerator {

    public static float[,] GenerateFalloffMap(int size) {
        float flatRoadWidth = 0.001f;   
        float totalValleyWidth = 0.2f; 

        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++)
            {
                
                float x = i / (float)size * 2 - 1;

                float distanceToPath = Mathf.Abs(x);
                float mask = CalculateValleyMask(distanceToPath, flatRoadWidth, totalValleyWidth);

                // Absolute value creates a V-shape valley centered at x = 0
                map[i, j] = 0.9f - mask;
            }
        }

        return map;
    }
    public static float CalculateValleyMask(float distanceToPath, float pathRadius, float valleyRadius) {
        // 1. Calculate the linear falloff
        // If distance < pathRadius, returns 0 (Flat Road)
        // If distance > valleyRadius, returns 1 (Wild Mountains)
        // If it's in between, it returns a decimal percentage.
        float linearFalloff = Mathf.InverseLerp(pathRadius, valleyRadius, distanceToPath);

        // 2. Apply S-Curve Smoothing
        // This bends the straight ramp into a natural, curved valley wall
        float smoothFalloff = Mathf.SmoothStep(0f, 1f, linearFalloff);

        return smoothFalloff;
    }
}