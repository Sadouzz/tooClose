using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds;
using GoogleMobileAds.Api;
using System;
using UnityEngine.UI;
using TMPro;
//using GooglePlayGames.BasicApi;
using UnityEngine.SceneManagement;

public class AdMob : MonoBehaviour
{
    public Button adButton, adButtonDiePanel, adButtonMission;
    bool adReady;
    public int watchedCount;
    Scene currentScene;

    public GameObject[] objectsToMove;

    public static AdMob instance;
    private void Awake()
    {
        currentScene = SceneManager.GetActiveScene();
        if (instance != null)
        {
            return;
        }
        instance = this;
    }

    void StatusAdButtons(bool status)
    {
        adButton.interactable = status;
        adButtonDiePanel.interactable = status;
    }

    void Start()
    {
        /*PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder().Build();
        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        // Optional: Automatically sign in the user
        SignInToGooglePlayServices();*/
        watchedCount = PlayerPrefs.GetInt("watchedAdsCount", 0);
        StatusAdButtons(false);
        //adButton.interactable = false;
        /*
        if (Inventory.instance.menu)
        {
            adButtonMission.interactable = false;
        }
        */

        InitializeAdMob();
    }

    // Start is called before the first frame update
    void InitializeAdMob()
    {
        MobileAds.RaiseAdEventsOnUnityMainThread = true;

        // Initialize the Google Mobile Ads SDK.
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            RequestConfiguration requestConfiguration = new RequestConfiguration
            {
                TagForChildDirectedTreatment = TagForChildDirectedTreatment.True,
                MaxAdContentRating = MaxAdContentRating.G
            };

            MobileAds.SetRequestConfiguration(requestConfiguration);
            //tr.text = "Ini";
            if(PlayerPrefs.GetString("noBannerAdsPaid", "no") == "no")
            {
                LoadAd();
            }
            LoadRewardedAd();
            
            // This callback is called once the MobileAds SDK is initialized.
        });

        
    }


    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private string _adUnitId = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
  private string _adUnitId = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _adUnitId = "unused";
#endif

    private RewardedAd _rewardedAd;


    // These ad units are configured to always serve test ads.
#if UNITY_ANDROID
    private string _bannerAdUnitId = "ca-app-pub-3940256099942544/6300978111";
#elif UNITY_IPHONE
  private string _bannerAdUnitId = "ca-app-pub-3940256099942544/2934735716";
#else
    private string _bannerAdUnitId = "unused";
#endif

    BannerView _bannerView;

    /// <summary>
    /// Creates a 320x50 banner view at top of the screen.
    /// </summary>
    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        // If we already have a banner, destroy the old one.
        if (_bannerView != null)
        {
            DestroyAd();
        }

        // Create a 320x50 banner at top of the screen
        _bannerView = new BannerView(_bannerAdUnitId, AdSize.Banner, AdPosition.Top);
        float bannerHeightInPixels = _bannerView.GetHeightInPixels();

        // Convertir la taille de la bannière de pixels en unités Unity
        float bannerHeightInUnits = bannerHeightInPixels / Screen.dpi * 2.54f; // Conversion en cm puis en Unity units

        // Décaler le RectTransform de l'élément UI en fonction de la taille de la bannière
        /*Vector2 offset = uiElementToAdjust.offsetMax;
        offset.y = -bannerHeightInUnits;  // Décaler vers le bas
        uiElementToAdjust.offsetMax = offset;*/

        /*for (int i = 0; i < objectsToMove.Length; i++)
        {
            Vector2 rectOffsetMax = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
            rectOffsetMax.y = -bannerHeightInUnits;
            Vector2 rectOffsetMin = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
            rectOffsetMin.y = -bannerHeightInUnits;

            objectsToMove[i].GetComponent<RectTransform>().offsetMax = rectOffsetMax;
            objectsToMove[i].GetComponent<RectTransform>().offsetMin = rectOffsetMin;
        }*/
        _bannerView.OnBannerAdLoaded += HandleOnAdLoaded;
    }

    private void HandleOnAdLoaded()
    {
        Debug.Log("HandleOnAdLoaded");
        Debug.Log("Height/currentResolution: " + Screen.height / (float)Screen.currentResolution.height);
        Debug.Log("Height: " + Screen.height);
        Debug.Log("currentResolution: " + Screen.currentResolution.height);
        Debug.Log("dpi: " + Screen.dpi);

        // Obtenir la hauteur de la bannière en pixels
        float bannerHeightInPixels = _bannerView.GetHeightInPixels();
        //Debug.Log(bannerHeightInPixels);
        // Convertir la taille de la bannière de pixels en unités Unity
        float bannerHeightInUnits = bannerHeightInPixels * Screen.height / (float)Screen.currentResolution.height; // Conversion en cm puis en Unity units
        //Debug.Log("Height in Unity: " + Screen.height / (float)Screen.currentResolution.height);
        // Décaler le RectTransform de l'élément UI en fonction de la taille de la bannière
        /*Vector2 offset = uiElementToAdjust.offsetMax;
        offset.y = -bannerHeightInUnits;  // Décaler vers le bas
        uiElementToAdjust.offsetMax = offset;*/
        float height = 0;
        if (Screen.currentResolution.height > Screen.height)
        {
            height = (1 - 0.94875f) * Screen.currentResolution.height;
        }
        else
        {
            height = (1 - 0.94875f) * Screen.height + 10;
        }

        height = 50 * (1440/0.692f) / 800;

        Debug.Log("Décalage avec 50 height  50 * (1440/0.692f) / 800:" + height);
        Debug.Log("BannerHeightsInPixels: " + bannerHeightInPixels);

        //float bannerHeightInUnity = bannerHeightInPixels * Screen.height / (float)Screen.currentResolution.height;

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            Vector2 rectOffsetMax = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
            rectOffsetMax.y = -height;
            Vector2 rectOffsetMin = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
            rectOffsetMin.y = -height;

            objectsToMove[i].GetComponent<RectTransform>().offsetMax = rectOffsetMax;
            objectsToMove[i].GetComponent<RectTransform>().offsetMin = rectOffsetMin;

            //objectsToMove[i].GetComponent<RectTransform>().anchoredPosition = new Vector2(objectsToMove[i].GetComponent<RectTransform>().anchoredPosition.x, objectsToMove[i].GetComponent<RectTransform>().anchoredPosition.y - height);
        }
    

    }

    /// <summary>
    /// Creates the banner view and loads a banner ad.
    /// </summary>
    public void LoadAd()
    {
        // create an instance of a banner view first.
        if (_bannerView == null)
        {
            CreateBannerView();
        }

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // send the request to load the ad.
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
    }

    /// <summary>
    /// Destroys the banner view.
    /// </summary>
    public void DestroyAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;


            for (int i = 0; i < objectsToMove.Length; i++)
            {
                Vector2 rectOffsetMax = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
                rectOffsetMax.y = 0;
                Vector2 rectOffsetMin = objectsToMove[i].GetComponent<RectTransform>().offsetMax;
                rectOffsetMin.y = 0;

                objectsToMove[i].GetComponent<RectTransform>().offsetMax = rectOffsetMax;
                objectsToMove[i].GetComponent<RectTransform>().offsetMin = rectOffsetMin;
            }
        }
    }

    /// <summary>
    /// Loads the rewarded ad.
    /// </summary>
    public void LoadRewardedAd()
    {
        // Clean up the old ad before loading a new one.
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");

        // create our request used to load the ad.
        var adRequest = new AdRequest();

        // Ajoutez un paramètre pour désactiver les annonces personnalisées
        adRequest.Extras = new Dictionary<string, string> {
        { "npa", "1" }  // "npa" signifie "Non-Personalized Ads"
    };

        // send the request to load the ad.
        RewardedAd.Load(_adUnitId, adRequest,
            (RewardedAd ad, LoadAdError error) =>
            {
                // if error is not null, the load request failed.
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad " +
                                   "with error : " + error);
                    StatusAdButtons(false);
                    /*
                    adButton.interactable = false;
                    if (currentScene.name == "Menu")
                    {
                        adButtonMission.interactable = false;
                    }
                    */
                    
                    return;
                }

                Debug.Log("Rewarded ad loaded with response : "
                          + ad.GetResponseInfo());

                _rewardedAd = ad;
                adReady = true;
                
                
            });
    }

    void Update()
    {
        if (adReady)
        {
            var buttComp = adButton.gameObject.GetComponent<TimeManagerFreePackWithAd>();
            if (buttComp != null)
            {
                if (buttComp.finished)
                {
                    StatusAdButtons(true);
                    //adButton.interactable = true;
                }
            }
            else
            {
                StatusAdButtons(true);
                //adButton.interactable = true;
                /*
                if (currentScene.name == "Parking")
                {
                    adButton.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = "Regarder une Pub pour continuer";
                }
                */
            }

            if(Inventory.instance.menu)
            {
                StatusAdButtons(true);
                //adButtonMission.interactable = true;
            }
            
        }
        else
        {
            if (Inventory.instance.menu)
            {
                StatusAdButtons(false);
                adButton.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Pas de Pub";
            }
            
        }
    }

    public void ShowRewardedAd(string _reward)
    {
        const string rewardMsg =
            "Rewarded ad rewarded the user. Type: {0}, amount: {1}.";

        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                // TODO: Reward the user.
                Debug.Log(String.Format(rewardMsg, reward.Type, reward.Amount));
                if(_reward == "250stars")
                {
                    Debug.Log("250stars");
                    PlayerPrefs.SetInt("stars", PlayerPrefs.GetInt("stars", 0) + 250);
                    PlayerPrefs.Save();
                    //UIScript.instance.ClaimRewardOnPack("blue");
                }
                if (_reward == "LifeRegen")
                {
                    Inventory.instance.AdsReward();
                }
                //adButton.interactable = false;
                StatusAdButtons(false);
                if (currentScene.name == "Menu")
                {
                    adButtonMission.interactable = false;
                }
                adReady = false;
                watchedCount++;
                PlayerPrefs.SetInt("watchedAdsCount", watchedCount);
                LoadRewardedAd();
            });
        }
    }
}