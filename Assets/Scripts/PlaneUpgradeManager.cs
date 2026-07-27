using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

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
        new UpgradeData { upgradeName = "VITESSE", type = UpgradeType.Speed, baseCost = 100, costMultiplier = 1.3f, maxLevel = 10, bonusPerLevel = 0.5f, description = "Voler plus vite" },
        new UpgradeData { upgradeName = "CONTROLE", type = UpgradeType.Handling, baseCost = 100, costMultiplier = 1.3f, maxLevel = 10, bonusPerLevel = 1.0f, description = "Tourner plus sec" },
        new UpgradeData { upgradeName = "BLINDAGE", type = UpgradeType.Armor, baseCost = 300, costMultiplier = 1.5f, maxLevel = 5, bonusPerLevel = 1.0f, description = "Plus de points de vie" }
    };

    [Header("UI Tabs (Les extensions de panels)")]
    public List<UpgradeTabUI> uiTabs;
    public float animationSpeed = 10f;

    [Header("Localization")]
    public string stringTableName = "UITexts"; // Modifiez ceci dans l'Inspecteur avec le VRAI nom de votre Table

    [Header("Ad Offer Settings")]
    [Range(0, 100)] public int adOfferChance = 30; // 30% de chance
    public GameObject adOfferPopup; // Le panel Popup complet
    public TextMeshProUGUI popupCostText; // Le texte du prix sur le bouton "Achat avec Etoiles"
    public Button popupBuyWithStarsButton; // Le bouton "Achat avec Etoiles" dans le popup

    [Header("Animation & Sound")]
    public AudioClip upgradeSound;
    public AudioSource audioSource;
    public TMP_FontAsset upgradeFont; // Optionnel, si on veut utiliser une font précise (sinon on utilise celle par défaut)

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

            int maxLvl = GetMaxLevel(tab.type);

            // On demande la traduction à partir des chaînes actuelles (qui servent de clés)
            string translatedName = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, data.upgradeName);
            string translatedDesc = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, data.description);

            // Si la traduction est vide (clé introuvable), on affiche le texte par défaut
            if (string.IsNullOrEmpty(translatedName) || translatedName.Contains("No translation")) translatedName = data.upgradeName;
            if (string.IsNullOrEmpty(translatedDesc) || translatedDesc.Contains("No translation")) translatedDesc = data.description;

            if (tab.nameText != null) tab.nameText.text = translatedName + " (" + currentLevel + "/" + maxLvl + ")";
            if (tab.descText != null) tab.descText.text = translatedDesc;

            if (currentLevel >= maxLvl)
            {
                if (tab.costText != null) tab.costText.text = "MAX";
                if (tab.buyButton != null) tab.buyButton.interactable = false;
            }
            else
            {
                int cost = CalculateCost(data, currentLevel);
                if (tab.costText != null) tab.costText.text = cost.ToString();
                
                // Le bouton est cliquable si on a assez d'étoiles OU si une pub est dispo
                if (tab.buyButton != null) tab.buyButton.interactable = (currentStars >= cost || isCurrentOfferAd);
            }
        }
    }

    public void BuyCurrentSelectedUpgrade()
    {
        if (isCurrentOfferAd)
        {
            // Ouvre le popup de choix
            if (adOfferPopup != null)
            {
                UpgradeData data = GetUpgradeData(currentSelectedType);
                int currentLevel = GetUpgradeLevel(currentPlaneIndex, currentSelectedType);
                int cost = CalculateCost(data, currentLevel);
                int currentStars = PlayerPrefs.GetInt("stars", 0);

                if (popupCostText != null) popupCostText.text = cost.ToString();
                
                // Si on n'a pas assez d'étoiles, on grise le bouton d'achat en étoiles du popup
                if (popupBuyWithStarsButton != null) popupBuyWithStarsButton.interactable = (currentStars >= cost);

                adOfferPopup.SetActive(true);
            }
            else 
            {
                // Fallback si on a oublié d'assigner le popup
                ConfirmWatchAdForUpgrade();
            }
        }
        else
        {
            BuyUpgrade(currentSelectedType); // Achat direct si pas d'offre pub
        }
    }

    // A lier au bouton "OUI / Regarder la pub" du Popup
    public void ConfirmWatchAdForUpgrade()
    {
        if (adOfferPopup != null) adOfferPopup.SetActive(false);
        if (AdMob.instance != null && AdMob.instance.adReady)
        {
            AdMob.instance.ShowRewardedAd("PlaneUpgrade");
        }
    }

    // A lier au bouton "Achat avec Etoiles" du Popup
    public void BuyFromPopupWithStars()
    {
        if (adOfferPopup != null) adOfferPopup.SetActive(false);
        BuyUpgrade(currentSelectedType);
    }

    // A lier au bouton "X" (Fermer) du Popup
    public void ClosePopup()
    {
        if (adOfferPopup != null) adOfferPopup.SetActive(false);
        isCurrentOfferAd = false; // Annule l'offre de pub
        UpdateAllTabsUI();
    }

    public void GrantUpgradeFromAd()
    {
        UpgradeData data = GetUpgradeData(currentSelectedType);
        if (data == null) return;

        int currentLevel = GetUpgradeLevel(currentPlaneIndex, currentSelectedType);
        int maxLvl = GetMaxLevel(currentSelectedType);
        if (currentLevel >= maxLvl) return;

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

        TriggerUpgradeAnimationAndSound(currentSelectedType);
    }

    public void BuyUpgrade(UpgradeType type)
    {
        UpgradeData data = GetUpgradeData(type);
        if (data == null) return;

        int currentLevel = GetUpgradeLevel(currentPlaneIndex, type);
        int maxLvl = GetMaxLevel(type);
        if (currentLevel >= maxLvl) return;

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
            
            TriggerUpgradeAnimationAndSound(type);
        }
    }

    private void TriggerUpgradeAnimationAndSound(UpgradeType type)
    {
        if (upgradeSound != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(upgradeSound);
            }
            else
            {
                // Fallback si pas d'AudioSource assigné
                AudioSource.PlayClipAtPoint(upgradeSound, Camera.main != null ? Camera.main.transform.position : Vector3.zero);
            }
        }
        StartCoroutine(PlayUpgradeAnimation(type));
    }

    private System.Collections.IEnumerator PlayUpgradeAnimation(UpgradeType type)
    {
        Vector3 startPos = Vector3.zero;
        Transform animParent = this.transform;
        
        foreach(var tab in uiTabs) {
            if(tab.type == type && tab.buyButton != null) {
                startPos = tab.buyButton.transform.position;
                if (tab.buyButton.transform.parent != null && tab.buyButton.transform.parent.parent != null)
                {
                    animParent = tab.buyButton.transform.parent.parent;
                }
                break;
            }
        }

        Vector3 endPos = Vector3.zero;
        Transform targetStatText = null;

        if (ChoosingPlaneScript.instance != null) {
            switch (type) {
                case UpgradeType.Speed: 
                    targetStatText = ChoosingPlaneScript.instance.speedText.transform; 
                    break;
                case UpgradeType.Handling: 
                    targetStatText = ChoosingPlaneScript.instance.angleSpeedText.transform; 
                    break;
                case UpgradeType.Armor: 
                    targetStatText = ChoosingPlaneScript.instance.lifeText.transform; 
                    break;
            }
            if (targetStatText != null) endPos = targetStatText.position;
        }

        if (startPos == Vector3.zero || endPos == Vector3.zero) yield break;

        GameObject animObj = new GameObject("UpgradePlusOne");
        
        // On cherche le Canvas le plus haut pour être sûr d'être au premier plan
        Canvas rootCanvas = animParent.GetComponentInParent<Canvas>();
        if (rootCanvas != null) {
            animObj.transform.SetParent(rootCanvas.rootCanvas.transform, false);
        } else {
            animObj.transform.SetParent(animParent, false);
        }
        
        animObj.transform.position = startPos;
        animObj.transform.SetAsLastSibling();

        TextMeshProUGUI tmp = animObj.AddComponent<TextMeshProUGUI>();
        
        // Récupérer la vraie valeur d'amélioration (bonus par niveau)
        float bonusValue = GetBonusPerLevel(type);
        tmp.text = "+" + bonusValue.ToString("0.##"); // Affiche +0.5, +1, etc. sans trop de décimales
        
        tmp.fontSize = 70;
        tmp.color = new Color(1f, 0.85f, 0f, 1f); // Jaune/Or
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        if (upgradeFont != null) tmp.font = upgradeFont;

        float duration = 0.8f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float easeT = 1f - (1f - t) * (1f - t); // Ease out quad
            
            animObj.transform.position = Vector3.Lerp(startPos, endPos, easeT);
            
            // Fading à la fin
            if (t > 0.5f) {
                float alphaT = (t - 0.5f) * 2f;
                tmp.color = new Color(tmp.color.r, tmp.color.g, tmp.color.b, 1f - alphaT);
            }
            
            yield return null;
        }

        Destroy(animObj);

        // Animation de scale sur le texte des stats
        if (targetStatText != null)
        {
            StartCoroutine(PulseStatText(targetStatText));
        }
    }

    private System.Collections.IEnumerator PulseStatText(Transform textTransform)
    {
        float t = 0;
        Vector3 startScale = Vector3.one; // On assume que la scale de base est 1

        while (t < 1)
        {
            t += Time.deltaTime * 6f; // Vitesse de la pulsation
            float scale = Mathf.Lerp(1f, 1.5f, Mathf.Sin(t * Mathf.PI));
            textTransform.localScale = startScale * scale;
            yield return null;
        }
        textTransform.localScale = startScale;
    }

    public int GetUpgradeLevel(int planeIndex, UpgradeType type)
    {
        return PlayerPrefs.GetInt(GetSaveKey(planeIndex, type), 1);
    }

    private string GetSaveKey(int planeIndex, UpgradeType type)
    {
        return "Plane_" + planeIndex + "_Upgrade_" + type.ToString();
    }

    private int CalculateCost(UpgradeData data, int level)
    {
        // level - 1 car le niveau de base est 1 (le premier achat est l'amélioration vers le niveau 2)
        return Mathf.RoundToInt(data.baseCost * Mathf.Pow(data.costMultiplier, level - 1));
    }

    private UpgradeData GetUpgradeData(UpgradeType type)
    {
        foreach (var data in upgrades)
        {
            if (data.type == type) return data;
        }
        return null;
    }

    public int GetMaxLevel(UpgradeType type)
    {
        if (ChoosingPlaneScript.instance == null) return GetUpgradeData(type).maxLevel;
        
        PlaneData planeData = ChoosingPlaneScript.instance.GetCurrentPlaneData();
        if (planeData == null) return GetUpgradeData(type).maxLevel;

        switch (type)
        {
            case UpgradeType.Speed: return planeData.maxSpeedLevel;
            case UpgradeType.Handling: return planeData.maxHandlingLevel;
            case UpgradeType.Armor: return planeData.maxArmorLevel;
        }
        return GetUpgradeData(type).maxLevel;
    }

    public float GetBonusPerLevel(UpgradeType type)
    {
        if (ChoosingPlaneScript.instance == null) return GetUpgradeData(type).bonusPerLevel;
        
        PlaneData planeData = ChoosingPlaneScript.instance.GetCurrentPlaneData();
        if (planeData == null) return GetUpgradeData(type).bonusPerLevel;

        switch (type)
        {
            case UpgradeType.Speed: return planeData.bonusSpeedPerLevel;
            case UpgradeType.Handling: return planeData.bonusHandlingPerLevel;
            case UpgradeType.Armor: return planeData.bonusArmorPerLevel;
        }
        return GetUpgradeData(type).bonusPerLevel;
    }

    public void ApplyUpgradesToPlane(int planeIndex, PlayerMovement playerMovement, PlaneData baseData)
    {
        int speedLevel = GetUpgradeLevel(planeIndex, UpgradeType.Speed);
        int handlingLevel = GetUpgradeLevel(planeIndex, UpgradeType.Handling);
        int armorLevel = GetUpgradeLevel(planeIndex, UpgradeType.Armor);

        float speedBonus = GetBonusPerLevel(UpgradeType.Speed);
        float handlingBonus = GetBonusPerLevel(UpgradeType.Handling);
        float armorBonus = GetBonusPerLevel(UpgradeType.Armor);

        // On fait (Level - 1) car le niveau 1 représente les statistiques de base sans bonus
        playerMovement.speed = baseData.speed + ((speedLevel - 1) * speedBonus);
        playerMovement.rotationSpeed = baseData.rotationSpeed + ((handlingLevel - 1) * handlingBonus);
        playerMovement.life = baseData.life + (int)((armorLevel - 1) * armorBonus);
    }
}
