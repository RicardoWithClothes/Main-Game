using UnityEngine;
using System.Collections;

public static class FalloffGenerator {

    public static float[,] GenerateFalloffMap(int size) {
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++) {
            for (int j = 0; j < size; j++)
            {
                float x = i / (float)size* 2 - 1;
                float y = j / (float)size  * 2 - 1;
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                // Create a diagonal slope: high at (0,0), low at (1,1)
                // float value = (x + y) / 2f;
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    static float Evaluate(float value) {
        float a = 3;
        float b = 2.2f;
        // desmos shi
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
        // Simple linear falloff from 0 (high) to 1 (low)
        // return value;
    }
}