using System.Collections;
using Unity.Mathematics;
using UnityEngine;


// keep the noise down bro
public static class Noise
{

    public enum NormalizeMode { Local, Global };

    public static TerrainPoint[,] GenerateNoiseMap(int mapWidth, int mapHeight,
    int seed, float scale, int octaves, float persistance, float lacunarity,
    Vector2 offset, NormalizeMode normalizeMode, float3[] bakedSpline,
    float roadWidth, float valleyWidth, float pathDepression, float meshWorldScale = 2f)
    {
        float[,] noiseMap = new float[mapWidth, mapHeight];
        System.Random prng = new System.Random(seed);
        Vector2[] octaveOffsets = new Vector2[octaves];

        float maxPossibleHeight = 0;
        float amplitude = 1;
        float frequency = 1;

        for (int i = 0; i < octaves; i++)
        {
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


        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {

                amplitude = 1;
                frequency = 1;
                float noiseHeight = 0;

                for (int i = 0; i < octaves; i++)
                {
                    float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;

                    amplitude *= persistance;
                    frequency *= lacunarity;
                }

                if (noiseHeight > maxLocalNoiseHeight) maxLocalNoiseHeight = noiseHeight;
                if (noiseHeight < minLocalNoiseHeight) minLocalNoiseHeight = noiseHeight;

                noiseMap[x, y] = noiseHeight;
            }
        }
        TerrainPoint[,] terrainGrid = new TerrainPoint[mapWidth, mapHeight];
        bool hasSpline = bakedSpline != null && bakedSpline.Length >= 2;

        // World coordinate reconstruction to match MeshGenerator vertex positions.
        //
        // The bordered noise grid is (mapChunkSize + 2) = mapWidth wide.
        // MeshGenerator's "meshSizeUnsimplified" = borderedSize - 2 = mapWidth - 2.
        // Its topLeftX = (meshSizeUnsimplified - 1) / -2f, topLeftZ = -topLeftX.
        // A vertex at grid index [xi, yi] (0-indexed, LOD=0, meshSimplificationIncrement=1) has:
        //   percent.x = (xi - 1) / meshSizeUnsimplified
        //   local vertex X = topLeftX + percent.x * meshSizeUnsimplified
        //   local vertex Z = topLeftZ - percent.y * meshSizeUnsimplified
        // The chunk GameObject is positioned at (chunkCoord * chunkSize * scale) and has localScale = scale.
        // So world X = chunkWorldOriginX + localX * scale
        //            = (offset.x * (mapWidth - 3) * scale) + localX * scale
        // (offset = chunk grid coordinate, chunkSize = mapWidth - 3 local units = 238 at full res)
        float meshSizeUnsimplified = mapWidth - 2;
        float chunkSizeLocal = meshSizeUnsimplified - 1;

        float topLeftX = chunkSizeLocal / -2f;
        float topLeftZ = chunkSizeLocal / 2f;
        // 1. CALCULATE SPLINE BOUNDING BOX ONCE
        float minSplineX = float.MaxValue, maxSplineX = float.MinValue;
        float minSplineZ = float.MaxValue, maxSplineZ = float.MinValue;

        if (hasSpline) {
            for (int s = 0; s < bakedSpline.Length; s++) {
                if (bakedSpline[s].x < minSplineX) minSplineX = bakedSpline[s].x;
                if (bakedSpline[s].x > maxSplineX) maxSplineX = bakedSpline[s].x;
                if (bakedSpline[s].z < minSplineZ) minSplineZ = bakedSpline[s].z;
                if (bakedSpline[s].z > maxSplineZ) maxSplineZ = bakedSpline[s].z;
            }
        }

        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                float normalizedHeight = (normalizeMode == NormalizeMode.Local)
                    ? Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y])
                    : Mathf.Clamp((noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f), 0, int.MaxValue);

                // SPLINE PURE CHECK THIS TESTING
                float finalH = normalizedHeight;
                float pathInf = 0f;

                if (hasSpline)
                {
                    // Reconstruct this vertex's world XZ position matching MeshGenerator.
                    // Shift by -1 to skip the border row/col.
                    float percentX = (x - 1) / meshSizeUnsimplified;
                    float percentY = (y - 1) / meshSizeUnsimplified;
                    float localX = topLeftX + percentX * chunkSizeLocal;
                    float localZ = topLeftZ - percentY * chunkSizeLocal;
                    // offset is in chunk-grid coords; each chunk covers chunkSizeLocal local units.
                    // Multiply by meshWorldScale to get world space.
                    float worldX = (offset.x * chunkSizeLocal + localX) * meshWorldScale;
                    float worldZ = (offset.y * chunkSizeLocal + localZ) * meshWorldScale;


                    if (worldX < minSplineX - valleyWidth || worldX > maxSplineX + valleyWidth || 
                        worldZ < minSplineZ - valleyWidth || worldZ > maxSplineZ + valleyWidth) {
                        finalH = normalizedHeight;
                        pathInf = 0f;
                    }
                    else {
                        float2 queryPos2D = new float2(worldX, worldZ);
                    float minDistanceSq = float.MaxValue;

                    for (int s = 0; s < bakedSpline.Length - 1; s++)
                    {
                        // AI added this, human review left
                        float2 a = bakedSpline[s].xz;
                        float2 b = bakedSpline[s + 1].xz;
                        float2 ab = b - a;
                        float2 ap = queryPos2D - a;
                        float l2 = math.lengthsq(ab);
                        float distSq;
                        // I have no clue bro
                        if (l2 < 0.00001f)
                        {
                            // Degenerate segment (zero length) — treat as point
                            distSq = math.distancesq(queryPos2D, a);
                        }
                        else
                        {
                            // Project onto segment, clamp t to [0,1]
                            float tSeg = math.clamp(math.dot(ap, ab) / l2, 0f, 1f);
                            float2 proj = a + tSeg * ab;
                            distSq = math.distancesq(queryPos2D, proj);
                        }
                        if (distSq < minDistanceSq)
                        {
                            minDistanceSq = distSq;
                        }
                    }

                    float trueDistance = math.sqrt(minDistanceSq);

                    // Valley Mask Math
                    float linearMask = Mathf.InverseLerp(roadWidth, valleyWidth, trueDistance);
                    float smoothMask = Mathf.SmoothStep(0f, 1f, linearMask);

                    // Path is a shallow depression in the terrain — no absolute height target.
                    // smoothMask = 0 at path center (full depression), 1 in open terrain (no change).
                    finalH = normalizedHeight - pathDepression * (1f - smoothMask);
                    finalH = Mathf.Max(0f, finalH);
                    pathInf = 1f - smoothMask;

                    }
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