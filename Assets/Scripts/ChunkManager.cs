using System.Collections.Generic;
using UnityEngine;

public class ChunkManager : MonoBehaviour
{
    public float maxViewDist = 20, initMaxViewDist;
    public float chunkSize = 20;

    public static ChunkManager instance;

    void Awake()
    {
        instance = this;
    }

    public Transform player;
    
    [UnityEngine.Serialization.FormerlySerializedAs("chunksAvailable")]
    public GameObject[] chunksAvailableEasy;
    public GameObject[] chunksAvailableHard;

    public static Vector2 playerPos;

    int chunksVisibleInViewDist;

    Dictionary<Vector2, TerrainChunk2D> chunkDictionary = new();
    List<TerrainChunk2D> visibleChunksLastUpdate = new();

    void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
        initMaxViewDist = maxViewDist;
    }

    void Update()
    {
        playerPos = player.position;
        UpdateVisibleChunks();

        if (PlayerPowerUpManager.instance != null && PlayerPowerUpManager.instance.isZoomActive)
        {
            maxViewDist = 35;
        }
        else if (PlayerMovement.instance != null && PlayerMovement.instance.currentPhase == PlayerMovement.GamePhase.Shooting)
        {
            maxViewDist = 30; // Augmenté pour la phase de tir afin d'éviter les vides sur les bords !
        }
        else if (PlayerMovement.instance != null && PlayerMovement.instance.currentPhase == PlayerMovement.GamePhase.Shooting && PlayerPowerUpManager.instance.isZoomActive)
        {
            maxViewDist = 40; // Augmenté pour la phase de tir afin d'éviter les vides sur les bords !
        }
        else
        {
            maxViewDist = initMaxViewDist;
        }

        chunksVisibleInViewDist = Mathf.RoundToInt(maxViewDist / chunkSize);
    }

    public void ForceUpdateChunks()
    {
        if (player != null)
        {
            playerPos = player.position;
            UpdateVisibleChunks();
        }
    }

    public void UpdateDifficulty()
    {
        foreach (var chunk in chunkDictionary.Values)
        {
            chunk.DestroyChunk();
        }
        chunkDictionary.Clear();
        visibleChunksLastUpdate.Clear();
        ForceUpdateChunks();
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
                    string difficulty = PlayerPrefs.GetString("Difficulty", "Easy");
                    GameObject[] currentChunks = (difficulty == "Hard" && chunksAvailableHard != null && chunksAvailableHard.Length > 0) ? chunksAvailableHard : chunksAvailableEasy;

                    chunkDictionary.Add(coord,
                        new TerrainChunk2D(coord, chunkSize, transform, currentChunks));
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

        public void DestroyChunk()
        {
            if (chunkObject != null)
                Object.Destroy(chunkObject);
        }
    }
}
