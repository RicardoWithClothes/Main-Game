using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Splines;


// The mesh generator, takes the noise map, and makes mesh. Also multithreads.
public class MapGenerator : MonoBehaviour
{

    public enum DrawMode { NoiseMap, ColourMap, Mesh, FalloffMap };
    public DrawMode drawMode;

    [Header("Imports")]
    public TerrainSettings DefaultterrainSettings;
    public SplineContainer testSpline;

    [Header("Spline / Path Settings")]
    public float roadWidth = 15f;
    public float valleyWidth = 50f;
    [Tooltip("How much lower the path is relative to the surrounding terrain (0 = flat, 0.05 = subtle dip, 0.2 = valley)")]

    [Range(0f, 0.5f)]
    public float pathDepression = 0.04f;

    [Header("Global Settings")]
    public Noise.NormalizeMode normalizeMode;
    public int seed;
    public Vector2 offset;
    float3[] cachedSpline;


    // 239 for not flatshading, 95 for flatshading (+ 1 for the border thing)
    public bool useFlatShading;
    [Range(0, 6)]
    public int editorPreviewLOD;

    public bool useFalloff;
    public bool useSlope;
    public bool autoUpdate;

    static MapGenerator instance;
    float[,] falloffMap;

    [Header("References")]
    public MapDisplay mapDisplay;

    // multithreading stuff
    Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
    Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

    void Awake()
    {
        falloffMap = FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2);
        cachedSpline = ExtractWorldSpaceSpline();

    }
    public static int mapChunkSize
    {
        get
        {
            if (instance == null) {
                instance = FindAnyObjectByType<MapGenerator>();
            }
            return instance.useFlatShading ? 95 : 239;
        }
    }

    float3[] ExtractWorldSpaceSpline()
    {
        if (testSpline == null) return null;

        float3[] sticks = new float3[100];
        for (int i = 0; i < 100; i++)
        {
            float t = i / 99f;
            // Evaluates the curve and converts it to World Space
            Vector3 worldPos = testSpline.transform.TransformPoint(testSpline.Spline.EvaluatePosition(t));
            sticks[i] = worldPos;
        }
        return sticks;
    }


    public void DrawMapInEditor()
    {
        if (DefaultterrainSettings == null) return;

        TerrainSettingsData settingsData = DefaultterrainSettings.GetThreadSafeData();
        float3[] safeSpline = ExtractWorldSpaceSpline();

        MapData mapData = GenerateMapData(Vector2.zero, settingsData, safeSpline);

        if (drawMode == DrawMode.NoiseMap)
        {

            float[,] previewNoise = new float[mapChunkSize, mapChunkSize];
            for (int y = 0; y < mapChunkSize; y++)
            {
                for (int x = 0; x < mapChunkSize; x++)
                {
                    previewNoise[x, y] = mapData.heightMap[x + 1, y + 1].finalHeight;
                }
            }
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(previewNoise));
        }
        else if (drawMode == DrawMode.ColourMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.Mesh)
        {
            AnimationCurve curve = new AnimationCurve(settingsData.meshHeightCurveKeys);
            mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, settingsData.meshHeightMultiplier, curve, editorPreviewLOD, useFlatShading, useSlope, settingsData.slopeSteepness, mapData.centre), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
        }
        else if (drawMode == DrawMode.FalloffMap)
        {
            mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(FalloffGenerator.GenerateFalloffMap(mapChunkSize + 2)));
        }
    }

    // the async map data threading
    public void RequestMapData(Vector2 centre, TerrainSettingsData terrainSettingsData, Action<MapData> callback)
    {
        float3[] safeSpline = cachedSpline;

        ThreadStart threadStart = delegate
        {
            MapDataThread(centre, terrainSettingsData, safeSpline, callback);
        };

        new Thread(threadStart).Start();
    }

    void MapDataThread(Vector2 centre, TerrainSettingsData terrainSettingsData, float3[] safeSpline, Action<MapData> callback)
    {

        MapData mapData = GenerateMapData(centre, terrainSettingsData, safeSpline);
        lock (mapDataThreadInfoQueue)
        {
            mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
        }
    }

    public void RequestMeshData(MapData mapData, int lod, TerrainSettingsData terrainSettingsData, Action<MeshData> callback)
    {
        ThreadStart threadStart = delegate
        {
            MeshDataThread(mapData, lod, terrainSettingsData, callback);
        };
        new Thread(threadStart).Start();
    }

    void MeshDataThread(MapData mapData, int lod, TerrainSettingsData settingsData, Action<MeshData> callback)
    {
        AnimationCurve curve = new AnimationCurve(settingsData.meshHeightCurveKeys);
        MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, settingsData.meshHeightMultiplier, curve, lod, useFlatShading, useSlope, settingsData.slopeSteepness, mapData.centre);

        lock (meshDataThreadInfoQueue)
        {
            meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
        }
    }

    // MAIN standard Unity Game thread
    void Update()
    {
        int mapCount = mapDataThreadInfoQueue.Count;
        for (int i = 0; i < mapCount; i++)
        {
            MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }
        int meshCount = meshDataThreadInfoQueue.Count;
        for (int i = 0; i < meshCount; i++)
        {
            MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
        }
    }

    // The math
    MapData GenerateMapData(Vector2 centre, TerrainSettingsData settings, float3[] safeSpline)
    {
        int currentChunkSize = mapChunkSize;
        TerrainPoint[,] terrainGrid = Noise.GenerateNoiseMap(
            currentChunkSize + 2, currentChunkSize + 2, seed, settings, centre + offset, normalizeMode,
            safeSpline, roadWidth, valleyWidth, pathDepression
        );

        // falloff only
        if (useFalloff)
        {
            for (int y = 0; y < currentChunkSize + 2; y++)
            {
                for (int x = 0; x < currentChunkSize + 2; x++)
                {
                    TerrainPoint currentPoint = terrainGrid[x, y];
                    currentPoint.finalHeight = Mathf.Max(0f, currentPoint.finalHeight - falloffMap[x, y]);
                    currentPoint.pathInfluence = Mathf.Max(currentPoint.pathInfluence, falloffMap[x, y]);
                    terrainGrid[x, y] = currentPoint;
                }
            }
        }

        Color[] colourMap = new Color[currentChunkSize * currentChunkSize];
        for (int y = 0; y < currentChunkSize; y++)
        {
            for (int x = 0; x < currentChunkSize; x++)
            {
                float currentHeight = terrainGrid[x + 1, y + 1].finalHeight;
                for (int i = 0; i < settings.regions.Length; i++)
                {
                    if (currentHeight >= settings.regions[i].height)
                    {
                        colourMap[y * currentChunkSize + x] = settings.regions[i].colour;
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        return new MapData(terrainGrid, colourMap, centre);
    }

    void OnTerrainSettingsUpdated()
    {
        // Only auto-update in the editor, prevent massive lag spikes while playing
        if (!Application.isPlaying && autoUpdate)
        {
            DrawMapInEditor();
        }
    }

    void OnValidate()
    {
        // 4. Subscribe the listener to the TerrainSettings event
        if (DefaultterrainSettings != null)
        {
            // Unsubscribe first to ensure we don't accidentally subscribe multiple times
            DefaultterrainSettings.OnValuesUpdated -= OnTerrainSettingsUpdated;
            DefaultterrainSettings.OnValuesUpdated += OnTerrainSettingsUpdated;
        }

    }

    struct MapThreadInfo<T>
    {
        public readonly Action<T> callback;
        public readonly T parameter;
        public MapThreadInfo(Action<T> callback, T parameter)
        {
            this.callback = callback;
            this.parameter = parameter;
        }

    }

}

[System.Serializable]
public struct TerrainPoint
{
    public float baseNoiseHeight; // The raw mountain
    public float finalHeight;     // The height after slopes and valleys
    public float pathInfluence;   // 0 = Mountains, 1 = Center of the road
    // You can add 'Color vertexColor' or 'int biomeIndex' here later!
}

public struct MapData
{
    public readonly TerrainPoint[,] heightMap;
    public readonly Color[] colourMap;
    public readonly Vector2 centre;

    public MapData(TerrainPoint[,] heightMap, Color[] colourMap, Vector2 centre)
    {
        this.heightMap = heightMap;
        this.colourMap = colourMap;
        this.centre = centre;
    }
}