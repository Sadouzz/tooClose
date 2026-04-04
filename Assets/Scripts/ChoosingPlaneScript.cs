using UnityEngine;
using UnityEngine.UI; // N'oublie pas d'ajouter ça pour manipuler l'UI

public class ChoosingPlaneScript : MonoBehaviour
{
    public int currentIndex = 0;
    public PlayerMovement playerMovement;
    public BuyPlaneScript buyScript;

    [Header("UI Synchronization")]
    // Glisse ici l'objet Parent qui contient toutes les IMAGES des avions en UI
    public Transform uiImagesParent;

    private const string PlaneSaveKey = "SelectedPlaneIndex";
    public static ChoosingPlaneScript instance;

    private void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    void Start()
    {
        currentIndex = PlayerPrefs.GetInt(PlaneSaveKey, 0);
        UpdateActivePlane();
    }

    public void NextPlane()
    {
        currentIndex = (currentIndex + 1) % transform.childCount;
        UpdateActivePlane();
    }

    public void PreviousPlane()
    {
        currentIndex = (currentIndex - 1 + transform.childCount) % transform.childCount;
        UpdateActivePlane();
    }

    public void SaveCurrentSelection()
    {
        PlayerPrefs.SetInt(PlaneSaveKey, currentIndex);
        PlayerPrefs.Save();
        Debug.Log("Avion sauvegardé : " + currentIndex);
    }

    private void UpdateActivePlane()
    {
        if (transform.childCount == 0) return;

        // --- 1. Mise à jour des objets physiques (3D/2D GameObjects) ---
        for (int i = 0; i < transform.childCount; i++)
        {
            GameObject child = transform.GetChild(i).gameObject;
            bool isActive = (i == currentIndex);
            child.SetActive(isActive);

            if (isActive)
            {
                PlaneData data = child.GetComponent<PlaneData>();
                SyncPlayerData(data);
                if (buyScript != null) buyScript.UpdateUI(currentIndex, data);
            }
        }

        // --- 2. Mise à jour des IMAGES UI (Le nouveau code) ---
        if (uiImagesParent != null)
        {
            for (int i = 0; i < uiImagesParent.childCount; i++)
            {
                // On active l'image correspondante à l'index, on cache les autres
                uiImagesParent.GetChild(i).gameObject.SetActive(i == currentIndex);
            }
        }
    }

    private void SyncPlayerData(PlaneData data)
    {
        if (data != null && playerMovement != null)
        {
            playerMovement.speed = data.speed;
            playerMovement.rotationSpeed = data.rotationSpeed;
            playerMovement.maxTiltAngle = data.maxTiltAngle;
            playerMovement.tiltSpeed = data.tiltSpeed;
            playerMovement.rb = data.rb;
            playerMovement.sr = data.sr;
            playerMovement.bc = data.bc;
        }
    }

    public int GetCurrentIndex() { return currentIndex; }
}