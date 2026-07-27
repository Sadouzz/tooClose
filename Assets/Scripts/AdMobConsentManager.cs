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
}