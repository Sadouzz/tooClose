using UnityEngine;
using GoogleMobileAds.Api;
using GoogleMobileAds.Ump.Api; // <-- Le namespace crucial pour l'UMP
using System;

public class AdMobConsentManager : MonoBehaviour
{
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