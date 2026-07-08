using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlaneUpgradeManager : MonoBehaviour
{
    public static PlaneUpgradeManager instance;

    public enum UpgradeType { Speed, Handling, Armor }

    [System.Serializable]
    public class UpgradeData
    {
        public string upgradeName;
        public UpgradeType type;
        public int baseCost = 100;
        public float costMultiplier = 1.3f;
        public int maxLevel = 10;
        public float bonusPerLevel = 0.5f;
        [TextArea] public string description;
    }

    [System.Serializable]
    public class UpgradeTabUI
    {
        public UpgradeType type;
        public GameObject expandedView; 
        public LayoutElement layoutElement; 
        
        public float expandedFlexibleWidth = 1f;
        public float collapsedFlexibleWidth = 0f;
        
        [HideInInspector] public float targetFlexibleWidth;
        
        [Header("Textes & Boutons")]
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI descText;
        public TextMeshProUGUI costText;
        public Button buyButton;
    }

    [Header("Upgrade Definitions")]
    public List<UpgradeData> upgrades = new List<UpgradeData>()
    {
        new UpgradeData { upgradeName = "Vitesse", type = UpgradeType.Speed, baseCost = 100, costMultiplier = 1.3f, maxLevel = 10, bonusPerLevel = 0.5f, description = "Voler plus vite" },
        new UpgradeData { upgradeName = "Maniabilité", type = UpgradeType.Handling, baseCost = 100, costMultiplier = 1.3f, maxLevel = 10, bonusPerLevel = 1.0f, description = "Tourner plus sec" },
        new UpgradeData { upgradeName = "Blindage", type = UpgradeType.Armor, baseCost = 300, costMultiplier = 1.5f, maxLevel = 5, bonusPerLevel = 1.0f, description = "Plus de points de vie" }
    };

    [Header("UI Tabs (Les extensions de panels)")]
    public List<UpgradeTabUI> uiTabs;
    public float animationSpeed = 10f;

    [Header("Ad Offer Settings")]
    [Range(0, 100)] public int adOfferChance = 30; // 30% de chance
    public GameObject adOfferPopup; // Le panel Popup à afficher
    private bool isCurrentOfferAd = false;

    private UpgradeType currentSelectedType = UpgradeType.Speed;
    private int currentPlaneIndex = 0;

    private void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    void Start()
    {
        foreach (var tab in uiTabs)
        {
            if (tab.layoutElement != null)
                tab.layoutElement.flexibleWidth = tab.collapsedFlexibleWidth;
        }

        SelectUpgradeType((int)UpgradeType.Speed);
    }

    void Update()
    {
        foreach (var tab in uiTabs)
        {
            if (tab.layoutElement != null)
            {
                tab.layoutElement.flexibleWidth = Mathf.Lerp(tab.layoutElement.flexibleWidth, tab.targetFlexibleWidth, Time.deltaTime * animationSpeed);
            }
        }
    }

    public void OnPlaneChanged(int planeIndex)
    {
        currentPlaneIndex = planeIndex;
        RollAdOffer();
        UpdateAllTabsUI();
    }

    public void SelectUpgradeType(int typeIndex)
    {
        currentSelectedType = (UpgradeType)typeIndex;
        RollAdOffer();
        UpdateAllTabsUI();
    }

    private void RollAdOffer()
    {
        // On tire au sort si on offre une pub pour cette amélioration
        if (AdMob.instance != null && AdMob.instance.adReady)
        {
            isCurrentOfferAd = (Random.Range(0, 100) < adOfferChance);
        }
        else
        {
            isCurrentOfferAd = false;
        }
    }

    public void UpdateAllTabsUI()
    {
        int currentStars = PlayerPrefs.GetInt("stars", 0);

        foreach (var tab in uiTabs)
        {
            bool isSelected = (tab.type == currentSelectedType);
            
            tab.targetFlexibleWidth = isSelected ? tab.expandedFlexibleWidth : tab.collapsedFlexibleWidth;

            if (tab.expandedView != null) tab.expandedView.SetActive(isSelected);

            UpgradeData data = GetUpgradeData(tab.type);
            if (data == null) continue;

            int currentLevel = GetUpgradeLevel(currentPlaneIndex, tab.type);

            if (tab.nameText != null) tab.nameText.text = data.upgradeName + " (" + currentLevel + "/" + data.maxLevel + ")";
            if (tab.descText != null) tab.descText.text = data.description;

            if (currentLevel >= data.maxLevel)
            {
                if (tab.costText != null) tab.costText.text = "MAX";
                if (tab.buyButton != null) tab.buyButton.interactable = false;
            }
            else
            {
                if (isSelected && isCurrentOfferAd)
                {
                    // Affichage spécial pour l'offre pub
                    if (tab.costText != null) tab.costText.text = "PUB";
                    if (tab.buyButton != null) tab.buyButton.interactable = true; // Toujours cliquable
                }
                else
                {
                    // Achat normal avec des étoiles
                    int cost = CalculateCost(data, currentLevel);
                    if (tab.costText != null) tab.costText.text = cost.ToString();
                    if (tab.buyButton != null) tab.buyButton.interactable = (currentStars >= cost);
                }
            }
        }
    }

    public void BuyCurrentSelectedUpgrade()
    {
        if (isCurrentOfferAd)
        {
            // Ouvre le popup de pub
            if (adOfferPopup != null)
            {
                adOfferPopup.SetActive(true);
            }
            else 
            {
                // Fallback si on a oublié d'assigner le popup, lance direct la pub
                ConfirmWatchAdForUpgrade();
            }
        }
        else
        {
            BuyUpgrade(currentSelectedType);
        }
    }

    // A lier au bouton "OUI" du Popup
    public void ConfirmWatchAdForUpgrade()
    {
        if (adOfferPopup != null) adOfferPopup.SetActive(false);
        if (AdMob.instance != null && AdMob.instance.adReady)
        {
            AdMob.instance.ShowRewardedAd("PlaneUpgrade");
        }
    }

    // A lier au bouton "NON" du Popup
    public void CancelWatchAd()
    {
        if (adOfferPopup != null) adOfferPopup.SetActive(false);
        // Optionnel : Annuler l'offre si le joueur refuse pour qu'il puisse payer en étoiles
        isCurrentOfferAd = false;
        UpdateAllTabsUI();
    }

    public void GrantUpgradeFromAd()
    {
        UpgradeData data = GetUpgradeData(currentSelectedType);
        if (data == null) return;

        int currentLevel = GetUpgradeLevel(currentPlaneIndex, currentSelectedType);
        if (currentLevel >= data.maxLevel) return;

        // Gain du niveau gratuit
        PlayerPrefs.SetInt(GetSaveKey(currentPlaneIndex, currentSelectedType), currentLevel + 1);
        PlayerPrefs.Save();

        // Relance une chance pour le niveau suivant
        RollAdOffer();
        UpdateAllTabsUI();

        if (ChoosingPlaneScript.instance != null)
        {
            ChoosingPlaneScript.instance.UpdateActivePlaneStatsOnly();
        }
    }

    public void BuyUpgrade(UpgradeType type)
    {
        UpgradeData data = GetUpgradeData(type);
        if (data == null) return;

        int currentLevel = GetUpgradeLevel(currentPlaneIndex, type);
        if (currentLevel >= data.maxLevel) return;

        int cost = CalculateCost(data, currentLevel);
        int currentStars = PlayerPrefs.GetInt("stars", 0);

        if (currentStars >= cost)
        {
            PlayerPrefs.SetInt("stars", currentStars - cost);
            PlayerPrefs.SetInt(GetSaveKey(currentPlaneIndex, type), currentLevel + 1);
            PlayerPrefs.Save();

            RollAdOffer(); // Peut-être une pub dispo pour le niveau suivant ?
            UpdateAllTabsUI();

            if (ChoosingPlaneScript.instance != null)
            {
                ChoosingPlaneScript.instance.UpdateActivePlaneStatsOnly();
            }
        }
    }

    public int GetUpgradeLevel(int planeIndex, UpgradeType type)
    {
        return PlayerPrefs.GetInt(GetSaveKey(planeIndex, type), 0);
    }

    private string GetSaveKey(int planeIndex, UpgradeType type)
    {
        return "Plane_" + planeIndex + "_Upgrade_" + type.ToString();
    }

    private int CalculateCost(UpgradeData data, int level)
    {
        return Mathf.RoundToInt(data.baseCost * Mathf.Pow(data.costMultiplier, level));
    }

    private UpgradeData GetUpgradeData(UpgradeType type)
    {
        foreach (var data in upgrades)
        {
            if (data.type == type) return data;
        }
        return null;
    }

    public void ApplyUpgradesToPlane(int planeIndex, PlayerMovement playerMovement, PlaneData baseData)
    {
        int speedLevel = GetUpgradeLevel(planeIndex, UpgradeType.Speed);
        int handlingLevel = GetUpgradeLevel(planeIndex, UpgradeType.Handling);
        int armorLevel = GetUpgradeLevel(planeIndex, UpgradeType.Armor);

        UpgradeData speedData = GetUpgradeData(UpgradeType.Speed);
        UpgradeData handlingData = GetUpgradeData(UpgradeType.Handling);
        UpgradeData armorData = GetUpgradeData(UpgradeType.Armor);

        playerMovement.speed = baseData.speed + (speedLevel * (speedData != null ? speedData.bonusPerLevel : 0f));
        playerMovement.rotationSpeed = baseData.rotationSpeed + (handlingLevel * (handlingData != null ? handlingData.bonusPerLevel : 0f));
        playerMovement.life = baseData.life + (int)(armorLevel * (armorData != null ? armorData.bonusPerLevel : 0f));
    }
}
