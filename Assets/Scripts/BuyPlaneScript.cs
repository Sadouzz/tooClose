using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class BuyPlaneScript : MonoBehaviour
{
    public ChoosingPlaneScript selectionScript;
    public GameObject[] playButtons;
    public GameObject buyPanel;
    public GameObject infoPanel; // Panel qui dit "Stars insuffisantes"
    public TextMeshProUGUI priceText, infoPanelText;

    // Simule ton stock de monnaie (à lier à ton vrai système de monnaie plus tard)
    private int playerStars;

    void Start()
    {
        // On récupère les stars au démarrage
        playerStars = PlayerPrefs.GetInt("stars", 0);
        infoPanel.SetActive(false);
    }

    // Cette fonction sera appelée par ChoosingPlaneScript à chaque changement
    public void UpdateUI(int index, PlaneData data)
    {
        // Vérifie si l'avion est déjà acheté (L'avion 0 est gratuit par défaut)
        bool isUnlocked = (index == 0) || PlayerPrefs.GetInt("Unlocked_" + index, 0) == 1;

        if (isUnlocked)
        {
            foreach (var item in playButtons)
            {
                item.SetActive(true);
            }
            selectionScript.SaveCurrentSelection();
            buyPanel.SetActive(false);
        }
        else
        {
            foreach (var item in playButtons)
            {
                item.SetActive(false);
            }
            buyPanel.SetActive(true);
            priceText.text = data.price.ToString();
        }
    }

    /*public void BuyCurrentPlane()
    {
        int currentIndex = selectionScript.GetCurrentIndex();
        PlaneData data = selectionScript.GetCurrentPlaneData();

        if (playerStars >= data.price)
        {
            // Achat réussi
            playerStars -= data.price;
            PlayerPrefs.SetInt("TotalStars", playerStars);

            // On enregistre le déblocage
            PlayerPrefs.SetInt("Unlocked_" + currentIndex, 1);
            PlayerPrefs.Save();

            // Refresh l'UI
            UpdateUI(currentIndex, data);
        }
        else
        {
            // Pas assez de stars
            ShowInfoPanel();
        }
    }*/

    public void BuyCurrentPlane()
    {
        int currentIndex = selectionScript.GetCurrentIndex();
        PlaneData data = selectionScript.transform.GetChild(currentIndex).GetComponent<PlaneData>();

        // Simulation de monnaie (TotalStars)
        int stars = PlayerPrefs.GetInt("stars", 0);

        if (stars >= data.price)
        {
            // 1. Déduire le prix
            PlayerPrefs.SetInt("stars", stars - data.price);

            // 2. Marquer comme débloqué
            PlayerPrefs.SetInt("Unlocked_" + currentIndex, 1);

            // 3. PUISQUE L'ACHAT EST FAIT, ON SAUVEGARDE CET INDEX COMME ÉTANT LE CHOIX ACTUEL
            selectionScript.SaveCurrentSelection();

            UpdateUI(currentIndex, data);
        }
        else
        {
            ShowInfoPanel();
        }
    }

    void ShowInfoPanel()
    {
        infoPanel.SetActive(true);
        infoPanelText.text = "Pas assez d'etoiles necessaires";
    }
}