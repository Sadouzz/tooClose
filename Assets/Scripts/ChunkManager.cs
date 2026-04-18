using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public float maxViewDist = 12f;
    public float chunkSize = 20;

    public Transform player;
    public GameObject[] chunksAvailable;

    public static Vector2 playerPos;

    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk2D> chunkDictionary = new();
    List<TerrainChunk2D> visibleChunksLastUpdate = new();

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    void Update()
    {
        playerPos = player.position;
        UpdateVisibleChunks();
    }

    void UpdateVisibleChunks()
    {
        foreach (var chunk in visibleChunksLastUpdate)
            chunk.SetVisible(false);

        visibleChunksLastUpdate.Clear();

        int currentChunkX = Mathf.RoundToInt(playerPos.x / chunkSize);
        int currentChunkY = Mathf.RoundToInt(playerPos.y / chunkSize);

        for (int y = -chunksVisibleInViewDist; y <= chunksVisibleInViewDist; y++)
        {
            for (int x = -chunksVisibleInViewDist; x <= chunksVisibleInViewDist; x++)
            {
                Vector2 coord = new(currentChunkX + x, currentChunkY + y);

                if (chunkDictionary.ContainsKey(coord))
                {
                    chunkDictionary[coord].UpdateChunk(maxViewDist); // Pass the variable here
                    if (chunkDictionary[coord].IsVisible())
                        visibleChunksLastUpdate.Add(chunkDictionary[coord]);
                }
                else
                {
                    chunkDictionary.Add(coord,
                        new TerrainChunk2D(coord, chunkSize, transform, chunksAvailable));
                }
            }
        }
    }

    // ================= CHUNK =================

    public class TerrainChunk2D
    {
        GameObject chunkObject;
        Vector2 position;
        Bounds bounds;

        public TerrainChunk2D(Vector2 coord, float size, Transform parent, GameObject[] prefabs)
        {
            position = coord * size;
            bounds = new Bounds(position, Vector2.one * size);

            int r = Random.Range(0, prefabs.Length);
            chunkObject = Object.Instantiate(prefabs[r], position, Quaternion.identity);

            chunkObject.transform.parent = parent;
            chunkObject.transform.position = new Vector3(position.x, position.y, 0);
            chunkObject.transform.localScale = Vector3.one;

            SetVisible(false);
        }

        public void UpdateChunk(float viewDistance) // Add parameter
        {
            float dist = Vector2.Distance(playerPos, position);
            SetVisible(dist <= viewDistance);
        }

        public void SetVisible(bool visible)
        {
            chunkObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return chunkObject.activeSelf;
        }
    }
}
