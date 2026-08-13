using System.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SocialPlatforms;


// keep the noise down bro
public static class Noise {

    public enum NormalizeMode { Local, Global };

    public static TerrainPoint[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, float3[] bakedSpline, float roadWidth, float valleyWidth, float heightMultiplier) {

        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++) {
            float offsetX = prng.Next(-100000, 100000) + offset.x;
            float offsetY = prng.Next(-100000, 100000) - offset.y;
            octaveOffsets[i] = new Vector2(offsetX, offsetY);

            maxPossibleHeight += amplitude;
            amplitude *= persistance;
        }

        if (scale <= 0) scale = 0.0001f;

        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        float halfWidth = mapWidth / 2f;
        float halfHeight = mapHeight / 2f;


        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++) {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                else if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }
        // normalization noise map. + spline calc CHECK THIS
        TerrainPoint[,] terrainGrid = new TerrainPoint[mapWidth, mapHeight];
        bool hasSpline = bakedSpline != null && bakedSpline.Length > 0;

        for (int y = 0; y < mapHeight; y++) {
            for (int x = 0; x < mapWidth; x++) {

                float normalizedHeight = (normalizeMode == NormalizeMode.Local)
                    ? Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y])
                    : Mathf.Clamp((noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f), 0, int.MaxValue);

                // SPLINE PURE CHECK THIS TESTING
                float finalH = normalizedHeight;
                float pathInf = 0f;

                if (hasSpline) {
                    float worldX = offset.x + (x - halfWidth);
                    float worldZ = offset.y + (halfHeight - y);
                    float worldY = normalizedHeight * heightMultiplier;

                    float3 queryPos = new float3(worldX, worldY, worldZ);

                    // FIX 2: Fast Polyline Stick Distance Check
                    float minDistanceSq = float.MaxValue;
                    for (int s = 0; s < bakedSpline.Length; s++) {
                        float distSq = math.distancesq(queryPos, bakedSpline[s]);
                        if (distSq < minDistanceSq) {
                            minDistanceSq = distSq;
                        }
                    }

                    float trueDistance = math.sqrt(minDistanceSq);

                    // Valley Mask Math
                    float linearMask = Mathf.InverseLerp(roadWidth, valleyWidth, trueDistance);
                    float smoothMask = Mathf.SmoothStep(0f, 1f, linearMask);


                    finalH *= smoothMask;
                    pathInf = 1f - smoothMask;
                }


                TerrainPoint point = new TerrainPoint();
                point.baseNoiseHeight = normalizedHeight; // Store the raw mountain height
                point.finalHeight = finalH;
                point.pathInfluence = pathInf;

                terrainGrid[x, y] = point;
            }
        }

        return terrainGrid;
    }

}