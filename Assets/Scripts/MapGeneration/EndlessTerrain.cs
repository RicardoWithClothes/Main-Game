using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// Takes players coordinates, and updates visible chunks
public class EndlessTerrain : MonoBehaviour
{
    [Header("Biome Profiles")]
    public TerrainSettings EdgeTerrainSettings;
    public TerrainSettings MaainTerrainSettings;

    TerrainSettingsData edgeSettingsData;
    TerrainSettingsData mainSettingsData;


    public const float scale = 2f;

    const float viewerMoveThresholdForChunkUpdate = 25f;
    const float sqrViewerMoveThresholdForChunkUpdate = viewerMoveThresholdForChunkUpdate * viewerMoveThresholdForChunkUpdate;

    public LODInfo[] detailLevels;
    public static float maxViewDst;

    public Transform viewer;
    public Material mapMaterial;

    public static Vector2 viewerPosition;
    Vector2 viewerPositionOld;
    static MapGenerator mapGenerator;
    int chunkSize;
    int chunksVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();


    void Start(){
        mapGenerator = FindAnyObjectByType<MapGenerator>();

        if (EdgeTerrainSettings != null) {
            edgeSettingsData = EdgeTerrainSettings.GetThreadSafeData();
        }
        if (MaainTerrainSettings != null) {
            mainSettingsData = MaainTerrainSettings.GetThreadSafeData();
        }

        maxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksVisibleInViewDst = Mathf.RoundToInt(maxViewDst / chunkSize);

        UpdateVisibleChunks();
    }

    void Update(){
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        // only update if player moved enough
        if ((viewerPositionOld - viewerPosition).sqrMagnitude > sqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = viewerPosition;
            UpdateVisibleChunks();
        }
    }

    void UpdateVisibleChunks(){

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++){
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        // player coords
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        // int currentChunkCoordY = Mathf.RoundToInt(viewerPosition.y / chunkSize);
        // loop through all chunk in view distance and update or create them.
        for (int z = -1; z <= 1; z++){
            for (int xOffset = -chunksVisibleInViewDst; xOffset <= chunksVisibleInViewDst; xOffset++){
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, z);
                // if chunk already exists, update it, else create it
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord)){
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }else{
                    // create
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial, z == 0 ? mainSettingsData : edgeSettingsData));
                }

            }
        }
    }

    // Each chunk getting its own data and mesh, updating itself when player moves. 
    public class TerrainChunk
    {

        GameObject meshObject;
        Vector2 position;
        Bounds bounds;

        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        MeshCollider meshCollider;

        LODInfo[] detailLevels;
        LODMesh[] lodMeshes;
        LODMesh colliderLODMesh;

        MapData mapData;
        bool mapDataReceived;
        int previousLODIndex = -1;

        TerrainSettingsData terrainSettingsData;

        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material, TerrainSettingsData terrainSettingsData)
        {
            this.detailLevels = detailLevels;
            this.terrainSettingsData = terrainSettingsData;

            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);
            // CHECK THIS 
            Vector3 positionV3 = new Vector3(position.x, 0, position.y);

            meshObject = new GameObject("Terrain Chunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshFilter = meshObject.AddComponent<MeshFilter>();
            meshCollider = meshObject.AddComponent<MeshCollider>();
            meshRenderer.material = material;

            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            SetVisible(false);

            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                // Pass UpdateTerrainChunk as a callback action
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
                if (detailLevels[i].useForCollider)
                {
                    colliderLODMesh = lodMeshes[i];
                }
            }
            // do the math in bg
            mapGenerator.RequestMapData(position, terrainSettingsData, OnMapDataReceived);
        }

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;

            // texture thing
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.mainTexture = texture;
            // run visibility and LOD checks
            UpdateTerrainChunk();
        }



        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDstFromNearestEdge <= maxViewDst;
                if (visible)
                {
                    int lodIndex = 0;
                    // visibility settings
                    for (int i = 0; i < detailLevels.Length - 1; i++)
                    {
                        if (viewerDstFromNearestEdge > detailLevels[i].visibleDstThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }
                    // only update if settings changes 
                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex];
                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData, terrainSettingsData);
                        }
                    }
                    // collider only if close enough 
                    if (lodIndex == 0)
                    {
                        if (colliderLODMesh.hasMesh)
                        {
                            meshCollider.sharedMesh = colliderLODMesh.mesh;
                        }
                        else if (!colliderLODMesh.hasRequestedMesh)
                        {
                            colliderLODMesh.RequestMesh(mapData, terrainSettingsData);
                        }
                    }

                    terrainChunksVisibleLastUpdate.Add(this);
                }
                // activate or deactivate chunk
                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }

    }

    class LODMesh
    {

        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        int lod;
        System.Action updateCallback;
        

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        // callback when mesh data received
        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;

            updateCallback();
        }

        // async request
        public void RequestMesh(MapData mapData, TerrainSettingsData terrainSettingsData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, terrainSettingsData, OnMeshDataReceived);
        }

    }

    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDstThreshold;
        public bool useForCollider;
    }

}