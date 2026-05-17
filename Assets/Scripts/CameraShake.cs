using System.Collections;
using UnityEngine;

public class CameraShake : MonoBehaviour {
    public IEnumerator Shake(float duration, float magnitude) {
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0.0f;

        // Grab a random starting point so the shake pattern is unique every time you trigger it
        float randomSeed = Random.Range(0f, 100f);

        while (elapsed < duration) {
            // Perlin noise returns 0 to 1. We multiply by 2 and subtract 1 to get a -1 to 1 range.
            // Multiplying Time.time by a speed multiplier (e.g., 25f) controls how "fast" the shake is.
            float x = (Mathf.PerlinNoise(randomSeed, Time.time * 25f) * 2f - 1f) * magnitude;
            float y = (Mathf.PerlinNoise(randomSeed + 10f, Time.time * 25f) * 2f - 1f) * magnitude;

            transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
    }

}