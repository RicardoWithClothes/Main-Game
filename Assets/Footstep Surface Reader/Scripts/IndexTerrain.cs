using UnityEngine;

/// <summary>
/// Returns the index of the terrain texture where the player is
/// </summary>
/// 
namespace FSR
{
    public class IndexTerrain
    {
        private int surfaceIndex = 0;

        private Terrain terrain;
        private TerrainData terrainData;
        private Vector3 terrainPos;

        void OnGUI()
        {
            GUI.Box(new Rect(100, 100, 200, 25), "index: " + surfaceIndex.ToString() + ", name: " + terrainData.terrainLayers[surfaceIndex].diffuseTexture.name);
        }

        public string GetMainTextureName(Vector3 WorldPos)
        {
            surfaceIndex = GetMainTexture(WorldPos);
            return terrainData.terrainLayers[surfaceIndex].diffuseTexture.name;
        }

        private float[] GetTextureMix(Vector3 WorldPos)
        {
            terrain = Terrain.activeTerrain;
            terrainData = terrain.terrainData;
            terrainPos = terrain.transform.position;

            int mapX = (int)(((WorldPos.x - terrainPos.x) / terrainData.size.x) * terrainData.alphamapWidth);
            int mapZ = (int)(((WorldPos.z - terrainPos.z) / terrainData.size.z) * terrainData.alphamapHeight);

            float[,,] splatmapData = terrainData.GetAlphamaps(mapX, mapZ, 1, 1);

            float[] cellMix = new float[splatmapData.GetUpperBound(2) + 1];

            for (int n = 0; n < cellMix.Length; n++)
            {
                cellMix[n] = splatmapData[0, 0, n];
            }
            return cellMix;
        }

        public int GetMainTexture(Vector3 WorldPos)
        {
            float[] mix = GetTextureMix(WorldPos);

            float maxMix = 0;
            int maxIndex = 0;

            for (int n = 0; n < mix.Length; n++)
            {
                if (mix[n] > maxMix)
                {
                    maxIndex = n;
                    maxMix = mix[n];
                }
            }
            return maxIndex;
        }
    }
}