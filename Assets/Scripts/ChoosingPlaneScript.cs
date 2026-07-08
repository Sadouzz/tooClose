using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChoosingPlaneScript : MonoBehaviour
{
    public int currentIndex = 0;
    public PlayerMovement playerMovement;
    public BuyPlaneScript buyScript;

    [Header("UI Synchronization")]
    public Transform uiImagesParent;
    public TextMeshProUGUI speedText, angleSpeedText, lifeText;

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
    }

    private void UpdateActivePlane()
    {
        if (transform.childCount == 0) return;

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

        if (uiImagesParent != null)
        {
            for (int i = 0; i < uiImagesParent.childCount; i++)
            {
                uiImagesParent.GetChild(i).gameObject.SetActive(i == currentIndex);
            }
        }

        if (PlaneUpgradeManager.instance != null)
        {
            PlaneUpgradeManager.instance.OnPlaneChanged(currentIndex);
        }
    }

    public void UpdateActivePlaneStatsOnly()
    {
        if (transform.childCount == 0) return;
        GameObject child = transform.GetChild(currentIndex).gameObject;
        PlaneData data = child.GetComponent<PlaneData>();
        SyncPlayerData(data);
    }

    private void SyncPlayerData(PlaneData data)
    {
        if (data != null && playerMovement != null)
        {
            playerMovement.speed = data.speed;
            playerMovement.rotationSpeed = data.rotationSpeed;
            playerMovement.maxTiltAngle = data.maxTiltAngle;
            playerMovement.tiltSpeed = data.tiltSpeed;
            playerMovement.life = data.life;
            playerMovement.rb = data.rb;
            playerMovement.sr = data.sr;
            playerMovement.bc = data.bc;
            if (data.smoke != null){
                playerMovement.smoke = data.smoke;
            }

            if (PlaneUpgradeManager.instance != null)
            {
                PlaneUpgradeManager.instance.ApplyUpgradesToPlane(currentIndex, playerMovement, data);
            }

            SyncUI(playerMovement.speed, playerMovement.rotationSpeed, playerMovement.life);
        }
    }

    void SyncUI(float speed, float angleSpeed, int life)
    {
        speedText.text = speed.ToString();
        angleSpeedText.text = angleSpeed.ToString();
        lifeText.text = life.ToString();
    }

    public int GetCurrentIndex() { return currentIndex; }
}
