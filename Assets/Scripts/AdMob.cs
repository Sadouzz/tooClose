using System;
using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Common;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AdMob : MonoBehaviour
{
    private BannerView _bannerView;
    private RewardedAd _rewardedAd;

#if UNITY_ANDROID
    private string _adUnitIdBanner = "ca-app-pub-3940256099942544/6300978111";
    private string _adUnitId       = "ca-app-pub-3940256099942544/5224354917";
#elif UNITY_IPHONE
    private string _adUnitIdBanner = "ca-app-pub-3940256099942544/2934735716";
    private string _adUnitId       = "ca-app-pub-3940256099942544/1712485313";
#else
    private string _adUnitIdBanner = "unused";
    private string _adUnitId       = "unused";
#endif

    public static AdMob instance;
    private Scene currentScene;

    public int watchedCount = 0;

    public Button adButton;
    public Button adButtonMission;
    public bool adReady;

    public GameObject[] objectsToMove;
    private Dictionary<RectTransform, float> originalYPositions = new Dictionary<RectTransform, float>();

    // Flag pour éviter un double-ajustement si OnBannerAdLoaded est appelé plusieurs fois
    private bool _bannerAdjusted = false;

    private bool isInitialized = false;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    void Start()
    {
        currentScene = SceneManager.GetActiveScene();
        watchedCount = PlayerPrefs.GetInt("watchedAdsCount", 0);
    }

    public void InitializeAdMob()
    {
        if (isInitialized) return;
        isInitialized = true;

        Debug.Log("AdMob: Initializing MobileAds SDK.");
        MobileAds.Initialize((InitializationStatus initStatus) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("AdMob: MobileAds SDK initialized. Loading ads.");
                LoadRewardedAd();
                LoadAd();
            });
        });
    }

    void StatusAdButtons(bool status)
    {
        if (adButton != null) adButton.interactable = status;
        if (adButtonMission != null) adButtonMission.interactable = status;
    }

    public void CreateBannerView()
    {
        Debug.Log("Creating banner view");

        if (_bannerView != null) DestroyAd();

        _bannerAdjusted = false;
        _bannerView = new BannerView(_adUnitIdBanner, AdSize.Banner, AdPosition.Bottom);
        ListenToAdEvents();
    }

    private void ListenToAdEvents()
    {
        // ⚠️ Tous les callbacks AdMob arrivent sur un thread background.
        //    On dispatch systématiquement sur le main thread via MobileAdsEventExecutor.
        _bannerView.OnBannerAdLoaded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view loaded an ad with response : " + _bannerView.GetResponseInfo());
                //AdjustUIForBanner();
            });
        };

        _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.LogError("Banner view failed to load an ad with error : " + error);
            });
        };

        _bannerView.OnAdPaid += (AdValue adValue) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view paid " + adValue.Value + " " + adValue.CurrencyCode);
            });
        };

        _bannerView.OnAdImpressionRecorded += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view recorded an impression.");
            });
        };

        _bannerView.OnAdClicked += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view was clicked.");
            });
        };

        _bannerView.OnAdFullScreenContentOpened += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view opened full screen content.");
            });
        };

        _bannerView.OnAdFullScreenContentClosed += () =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                Debug.Log("Banner view closed full screen content.");
            });
        };
    }

    // -------------------------------------------------------
    // Décale les éléments UI sous la bannière
    // -------------------------------------------------------
    private void AdjustUIForBanner()
    {
        // Evite un double-décalage si le callback est déclenché plusieurs fois
        if (_bannerAdjusted) return;

        float height = GetBannerHeightInUIUnits();
        Debug.Log("[AdMob] Banner UI height (unités Canvas) : " + height);

        if (height <= 0f)
        {
            Debug.LogWarning("[AdMob] Hauteur calculée nulle ou négative — décalage annulé.");
            return;
        }

        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] == null) continue;
            RectTransform rt = objectsToMove[i].GetComponent<RectTransform>();
            if (rt == null) continue;

            // Sauvegarde de la position Y originale (une seule fois par session)
            if (!originalYPositions.ContainsKey(rt))
                originalYPositions[rt] = rt.anchoredPosition.y;

            // Bannière en haut → les éléments ancrés en HAUT descendent (Y diminue)
            //                   → les éléments ancrés en BAS remontent  (Y augmente)
            bool anchorIsTop = rt.anchorMin.y >= 0.5f;
            Vector2 newPos = rt.anchoredPosition;
            newPos.y = anchorIsTop
                ? originalYPositions[rt] - height
                : originalYPositions[rt] + height;
            rt.anchoredPosition = newPos;
        }

        _bannerAdjusted = true;
    }

    // -------------------------------------------------------
    // Calcul de la hauteur de la bannière en unités Canvas
    // -------------------------------------------------------
    private float GetBannerHeightInUIUnits()
    {
        // Récupération du Canvas racine
        Canvas canvas = null;
        for (int i = 0; i < objectsToMove.Length; i++)
        {
            if (objectsToMove[i] != null)
            {
                canvas = objectsToMove[i].GetComponentInParent<Canvas>();
                if (canvas != null)
                {
                    while (canvas.transform.parent != null)
                    {
                        Canvas parentCanvas = canvas.transform.parent.GetComponentInParent<Canvas>();
                        if (parentCanvas != null) canvas = parentCanvas;
                        else break;
                    }
                    break;
                }
            }
        }

        if (canvas == null)
        {
            Debug.LogError("[AdMob] Aucun Canvas trouvé — décalage impossible.");
            return 0f;
        }

        float canvasHeightUnits = canvas.GetComponent<RectTransform>().rect.height;

        // La vraie densité de l'écran via le pont Java Android
        float density = GetAndroidDensity();

        // GetHeightInPixels() retourne bien des pixels physiques sur l'appareil !
        float bannerPhysicalPx = _bannerView != null ? _bannerView.GetHeightInPixels() : 0f;

        // Fallback : si la hauteur est 0 (ex: dans l'éditeur), on utilise 50 dp convertis en pixels
        if (bannerPhysicalPx <= 0f) 
        {
            bannerPhysicalPx = 50f * density;
        }

        // dp → pixels physiques → unités Canvas (formule proportionnelle)
        float result = (bannerPhysicalPx / Screen.height) * canvasHeightUnits;

        Debug.Log("[AdMob] bannerPhysicalPx=" + bannerPhysicalPx
                  + "  density=" + density
                  + "  Screen.height=" + Screen.height
                  + "  canvasHeightUnits=" + canvasHeightUnits
                  + "  → offset=" + result + " unités Canvas");

        return result;
    }

    // -------------------------------------------------------
    // Lit la densité réelle de l'écran via DisplayMetrics Android
    // (Screen.dpi retourne souvent 96 par défaut sur Android Unity)
    // -------------------------------------------------------
    private float GetAndroidDensity()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity   = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var resources  = activity.Call<AndroidJavaObject>("getResources"))
            using (var metrics    = resources.Call<AndroidJavaObject>("getDisplayMetrics"))
            {
                float d = metrics.Get<float>("density");
                Debug.Log("[AdMob] Android DisplayMetrics.density = " + d);
                return d;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[AdMob] Impossible de lire DisplayMetrics.density : " + e.Message
                             + " — fallback Screen.dpi");
        }
#endif
        // Fallback éditeur / iOS / erreur
        float fallback = Screen.dpi > 0f ? Screen.dpi / 160f : 2f;
        Debug.Log("[AdMob] density fallback = " + fallback + " (Screen.dpi=" + Screen.dpi + ")");
        return fallback;
    }

    public void LoadAd()
    {
        if (_bannerView == null) CreateBannerView();
        var adRequest = new AdRequest();
        Debug.Log("Loading banner ad.");
        _bannerView.LoadAd(adRequest);
    }

    public void DestroyAd()
    {
        if (_bannerView != null)
        {
            Debug.Log("Destroying banner view.");
            _bannerView.Destroy();
            _bannerView = null;
            _bannerAdjusted = false;

            // Restauration des positions originales
            foreach (var kv in originalYPositions)
            {
                if (kv.Key == null) continue;
                Vector2 pos = kv.Key.anchoredPosition;
                pos.y = kv.Value;
                kv.Key.anchoredPosition = pos;
            }
            originalYPositions.Clear();
        }
    }

    public void LoadRewardedAd()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("Loading the rewarded ad.");
        var adRequest = new AdRequest();
        adRequest.Extras = new Dictionary<string, string> { { "npa", "1" } };

        RewardedAd.Load(_adUnitId, adRequest, (RewardedAd ad, LoadAdError error) =>
        {
            MobileAdsEventExecutor.ExecuteInUpdate(() =>
            {
                if (error != null || ad == null)
                {
                    Debug.LogError("Rewarded ad failed to load an ad with error : " + error);
                    StatusAdButtons(false);
                    return;
                }
                Debug.Log("Rewarded ad loaded");
                _rewardedAd = ad;
                adReady = true;
            });
        });
    }

    void Update()
    {
        if (adReady)
        {
            var buttComp = adButton.gameObject.GetComponent<TimeManagerFreePackWithAd>();
            if (buttComp != null)
            {
                if (buttComp.finished) StatusAdButtons(true);
            }
            else
            {
                StatusAdButtons(true);
            }

            if (Inventory.instance != null && Inventory.instance.menu)
                StatusAdButtons(true);
        }
        else
        {
            if (Inventory.instance != null && Inventory.instance.menu)
            {
                StatusAdButtons(false);
                if (adButton != null && adButton.transform.childCount > 1)
                    adButton.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = "Pas de Pub";
            }
        }
    }

    public void ShowRewardedAd(string _reward)
    {
        if (_rewardedAd != null && _rewardedAd.CanShowAd())
        {
            _rewardedAd.Show((Reward reward) =>
            {
                MobileAdsEventExecutor.ExecuteInUpdate(() =>
                {
                    if (_reward == "250stars")
                    {
                        PlayerPrefs.SetInt("stars", PlayerPrefs.GetInt("stars", 0) + 250);
                        PlayerPrefs.Save();
                    }
                    if (_reward == "LifeRegen")
                    {
                        Inventory.instance.AdsReward();
                    }

                    StatusAdButtons(false);
                    if (currentScene.name == "Menu")
                        adButtonMission.interactable = false;

                    adReady = false;
                    watchedCount++;
                    PlayerPrefs.SetInt("watchedAdsCount", watchedCount);
                    LoadRewardedAd();
                });
            });
        }
    }
}
