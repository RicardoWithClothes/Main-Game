using UnityEngine;
using UnityEngine.Serialization;



// Damn
[CreateAssetMenu(fileName = "New Terrain Settings", menuName = "Procedural Terrain/Terrain Settings")]

public class TerrainSettings : ScriptableObject {
    [Header("Noise Shape")]
    public float noiseScale;
    public int octaves;
    [Range(0, 1)] public float persistence;
    public float lacunarity;
    public float slopeSteepness;

    [Header("Mesh Geometry")]
    public float meshHeightMultiplier;
    public AnimationCurve meshHeightCurve;

    [Header("Biomes & Textures")]
    public TerrainType[] regions;

    public event System.Action OnValuesUpdated;
    void OnValidate() {
        if (OnValuesUpdated != null) {
            OnValuesUpdated(); // "Shout" that the values have changed!
        }
    }

    public TerrainSettingsData GetThreadSafeData() {
        return new TerrainSettingsData(
            noiseScale,
            slopeSteepness,
            octaves,
            persistence,
            lacunarity,
            meshHeightMultiplier,
            meshHeightCurve.keys,
            regions
        );
    }
}
public struct TerrainSettingsData {
    public readonly float noiseScale;
    public readonly float slopeSteepness;
    public readonly int octaves;
    public readonly float persistence;
    public readonly float lacunarity;
    public readonly float meshHeightMultiplier;
    public readonly Keyframe[] meshHeightCurveKeys;
    public readonly TerrainType[] regions;

    public TerrainSettingsData(float noiseScale, float slopeSteepness, int octaves, float persistence, float lacunarity, float meshHeightMultiplier, Keyframe[] meshHeightCurveKeys, TerrainType[] regions) {
        this.noiseScale = noiseScale;
        this.slopeSteepness = slopeSteepness;
        this.octaves = octaves;
        this.persistence = persistence;
        this.lacunarity = lacunarity;
        this.meshHeightMultiplier = meshHeightMultiplier;
        this.meshHeightCurveKeys = meshHeightCurveKeys;
        this.regions = regions;
    }
}

[System.Serializable]
public struct TerrainType {
    public string name;
    public float height;
    public Color colour;
}