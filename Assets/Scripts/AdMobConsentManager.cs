using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api; // <-- Le namespace crucial pour l'UMP
using System;

public class AdMobConsentManager : MonoBehaviour
{
    public static AdMobConsentManager instance;

    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 1. On lance la vérification du consentement dès le démarrage
        RequestConsent();
    }

    void RequestConsent()
    {
        // Créer les paramètres de requête
        ConsentRequestParameters request = new ConsentRequestParameters();

        // (Optionnel) Pour tester sur votre propre appareil, vous pouvez forcer la localisation en Europe
        // ConsentDebugSettings debugSettings = new ConsentDebugSettings
        // {
        //     DebugGeography = DebugGeography.EEA,
        //     TestDeviceHashedIds = new List<string> { "VOTRE_HASH_DEVICE_ID" }
        // };
        // request.ConsentDebugSettings = debugSettings;

        // 2. Vérifier le statut actuel auprès des serveurs Google
        ConsentInformation.Update(request, OnConsentInfoUpdated);
    }

    void OnConsentInfoUpdated(FormError consentError)
    {
        if (consentError != null)
        {
            Debug.LogError("Erreur UMP: " + consentError.Message);
            // S'il y a une erreur réseau, on tente quand même de lancer AdMob
            StartAdMob();
            return;
        }

        // 3. Si la mise à jour a réussi, on charge et affiche le formulaire si nécessaire
        ConsentForm.LoadAndShowConsentFormIfRequired((FormError formError) =>
        {
            if (formError != null)
            {
                Debug.LogError("Erreur d'affichage du formulaire: " + formError.Message);
            }

            // 4. Une fois que le joueur a répondu (ou si le formulaire n'était pas requis)
            // On vérifie si on a le droit de demander des pubs
            if (ConsentInformation.CanRequestAds())
            {
                StartAdMob();
            }
        });
    }

    [Header("Fallback UI")]
    [Tooltip("Panel affiché quand les préférences pub ne sont pas disponibles (hors Europe).")]
    public GameObject feedbackPanel;
    private Coroutine _feedbackCoroutine;

    public void OpenPrivacySettings()
    {
        if (ConsentInformation.PrivacyOptionsRequirementStatus == PrivacyOptionsRequirementStatus.Required)
        {
            ConsentForm.ShowPrivacyOptionsForm((FormError formError) =>
            {
                if (formError != null)
                {
                    Debug.LogError("Erreur d'affichage des paramètres : " + formError.Message);
                }
            });
        }
        else
        {
            if (_feedbackCoroutine != null)
            {
                StopCoroutine(_feedbackCoroutine);
            }
            _feedbackCoroutine = StartCoroutine(ShowTemporaryFeedback());
        }
    }

    private System.Collections.IEnumerator ShowTemporaryFeedback()
    {
        if (feedbackPanel == null) yield break;

        feedbackPanel.SetActive(true);
        
        CanvasGroup cg = feedbackPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = feedbackPanel.AddComponent<CanvasGroup>();

        float animDuration = 0.3f;
        float elapsed = 0f;
        Vector3 targetScale = Vector3.one; // Ou la scale initiale si différente

        // Animation d'entrée (Fade + Zoom)
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;
            float easeT = 1f - (1f - t) * (1f - t); // Ease out quad

            cg.alpha = t;
            feedbackPanel.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, easeT);
            
            yield return null;
        }

        cg.alpha = 1f;
        feedbackPanel.transform.localScale = targetScale;

        // Attendre 1.5 secondes
        yield return new WaitForSeconds(1.5f);

        // Animation de sortie (Fade + Zoom Inversé)
        elapsed = 0f;
        while (elapsed < animDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animDuration;

            cg.alpha = 1f - t;
            feedbackPanel.transform.localScale = Vector3.Lerp(targetScale, Vector3.zero, t);
            
            yield return null;
        }

        feedbackPanel.SetActive(false);
    }

    void StartAdMob()
    {
        Debug.Log("Initialisation d'AdMob...");

        // Transmettre les consentements GDPR et CCPA à Unity Ads avant d'initialiser AdMob
        PassConsentToUnityAds();

        if (AdMob.instance == null)
        {
            AdMob.instance = FindFirstObjectByType<AdMob>();
        }

        if (AdMob.instance != null)
        {
            AdMob.instance.InitializeAdMob();
        }
        else
        {
            Debug.LogError("AdMob instance non trouvée dans la scène.");
        }
    }

    void PassConsentToUnityAds()
    {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        try
        {
            bool gdprConsent = IsGdprConsentGranted();
            bool privacyConsent = IsPrivacyConsentGranted();

            GoogleMobileAds.Api.Mediation.UnityAds.UnityAds.SetConsentMetaData("gdpr.consent", gdprConsent);
            GoogleMobileAds.Api.Mediation.UnityAds.UnityAds.SetConsentMetaData("privacy.consent", privacyConsent);

            Debug.Log($"[AdMobConsent] Passed consent to Unity Ads: gdpr.consent={gdprConsent}, privacy.consent={privacyConsent}");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[AdMobConsent] Failed to pass consent to Unity Ads: " + e.Message);
        }
#else
        Debug.Log("[AdMobConsent] Consent passing to Unity Ads skipped (Editor/Unsupported Platform).");
#endif
    }

    bool IsGdprConsentGranted()
    {
        // Si le consentement n'est pas requis (ex: Hors Europe), on accorde le consentement par défaut
        if (ConsentInformation.ConsentStatus == ConsentStatus.NotRequired)
        {
            return true;
        }

        if (ConsentInformation.ConsentStatus == ConsentStatus.Obtained)
        {
            // Lire la chaîne de consentement IAB TCF v2
            string purposeConsents = ApplicationPreferences.GetString("IABTCF_PurposeConsents");
            if (!string.IsNullOrEmpty(purposeConsents) && purposeConsents.Length > 0)
            {
                // Caractère 0 = Purpose 1 (Stockage local/Accès cookies)
                return purposeConsents[0] == '1';
            }
        }

        return false;
    }

    bool IsPrivacyConsentGranted()
    {
        // Lire la chaîne de consentement IAB US Privacy (CCPA)
        string usPrivacy = ApplicationPreferences.GetString("IABUSPrivacy_String");
        if (!string.IsNullOrEmpty(usPrivacy) && usPrivacy.Length >= 3)
        {
            // Caractère 2 = Choix d'Opt-Out (Y = Refus du partage/vente de données, N = Acceptation)
            if (usPrivacy[2] == 'Y')
            {
                return false;
            }
        }
        return true; // Consentement accordé par défaut (l'utilisateur ne s'est pas opposé)
    }
}