using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AchievementsManager : MonoBehaviour
{
    // -------------------------------------------------------
    // Glisse l'objet "Content" ici — les boutons sont collectés automatiquement
    // -------------------------------------------------------
    public Transform content;

    // Textes de progression affichés dans l'UI
    public TextMeshProUGUI textMission2;  // missiles détruits total
    public TextMeshProUGUI textMission4;  // missiles détruits total
    public TextMeshProUGUI textMission7;  // pubs regardées
    public TextMeshProUGUI textMission11; // ennemis détruits total
    public TextMeshProUGUI textMission12; // ennemis détruits total
    public TextMeshProUGUI textMission13; // ennemis détruits total

    [Header("Récompense Mission 13")]
    [Tooltip("Index de l'avion à débloquer (position dans ChoosingPlaneScript, 0 = avion de base)")]
    public int mission13PlaneIndex = 7;
    public Sprite mission13PlaneSprite, starsSprite;
    public string mission13PlaneName = "JET";

    // Boutons collectés dynamiquement — buttons[0] = Mission 1, etc.
    private Button[] buttons;
    private const int MISSION_COUNT = 14;

    // -------------------------------------------------------
    void Awake()
    {
        CollectButtonsDynamically();
    }

    // -------------------------------------------------------
    // Parcourt Content → chaque enfant Mission → trouve "Button"
    // -------------------------------------------------------
    void CollectButtonsDynamically()
    {
        if (content == null)
        {
            Debug.LogError("[AchievementsManager] Content non assigné !");
            buttons = new Button[0];
            return;
        }

        var found = new List<Button>();
        for (int i = 0; i < content.childCount; i++)
        {
            Transform mission = content.GetChild(i);
            // Cherche l'enfant direct nommé "Button"
            Transform btnTransform = mission.Find("Button");
            if (btnTransform != null)
            {
                Button btn = btnTransform.GetComponent<Button>();
                if (btn != null)
                    found.Add(btn);
                else
                    Debug.LogWarning("[AchievementsManager] Pas de Button sur " + mission.name + "/Button");
            }
            else
            {
                Debug.LogWarning("[AchievementsManager] Pas d'enfant 'Button' dans " + mission.name);
            }
        }

        buttons = found.ToArray();
        Debug.Log("[AchievementsManager] " + buttons.Length + " boutons collectés dynamiquement.");
    }

    // -------------------------------------------------------
    void Update()
    {
        RefreshProgressTexts();
        RefreshAllButtons();
    }

    // -------------------------------------------------------
    // Met à jour les textes de progression
    // -------------------------------------------------------
    void RefreshProgressTexts()
    {
        int missiles = PlayerPrefs.GetInt("totalDestroyedMissiles", 0);
        int enemies  = PlayerPrefs.GetInt("totalDestroyedEnemies",  0);
        int ads      = PlayerPrefs.GetInt("watchedAdsCount",        0);

        if (textMission2  != null) textMission2.text  = missiles.ToString();
        if (textMission4  != null) textMission4.text  = missiles.ToString();
        if (textMission7  != null) textMission7.text  = ads.ToString();
        if (textMission11 != null) textMission11.text = enemies.ToString();
        if (textMission12 != null) textMission12.text = enemies.ToString();
        if (textMission13 != null) textMission13.text = enemies.ToString();
    }

    // -------------------------------------------------------
    // Rafraîchit l'état interactable de tous les boutons
    // -------------------------------------------------------
    void RefreshAllButtons()
    {
        for (int i = 1; i <= buttons.Length; i++)
        {
            if (buttons[i - 1] == null) continue;

            bool completed = PlayerPrefs.GetString("mission" + i,               "no") == "yes";
            bool collected = PlayerPrefs.GetString("mission" + i + "Collected", "no") == "yes";

            if (completed && !collected)
            {
                buttons[i - 1].interactable = true;
            }
            else
            {
                if (collected)
                {
                    var label = buttons[i - 1].transform.GetChild(0).GetComponent<TextMeshProUGUI>();
                    if (label != null) label.text = "DEJA COLLECTE";
                }
                buttons[i - 1].interactable = false;
            }
        }
    }

    // -------------------------------------------------------
    // Appelé par chaque bouton (passer l'index 1-based)
    // -------------------------------------------------------
    public void CollectRewardFromMission(int _index)
    {
        if (_index < 1 || _index - 1 >= buttons.Length) return;

        buttons[_index - 1].interactable = false;
        PlayerPrefs.SetString("mission" + _index + "Collected", "yes");
        PlayerPrefs.Save();

        GrantReward(_index);
    }

    // -------------------------------------------------------
    // Attribution des récompenses par mission
    // -------------------------------------------------------
    void GrantReward(int index)
    {
        switch (index)
        {
            // ── MISSIONS TEMPS ──────────────────────────────
            case 1:  GiveStars(200,  "VOUS RECEVEZ 200 ETOILES");  break;
            case 5:  GiveStars(350,  "VOUS RECEVEZ 350 ETOILES");  break;
            case 8:  GiveStars(600,  "VOUS RECEVEZ 600 ETOILES");  break;

            // ── MISSIONS MISSILES ────────────────────────────
            case 2:  GiveStars(500,  "VOUS RECEVEZ 500 ETOILES");  break;
            case 4:  GiveStars(1000, "VOUS RECEVEZ 1000 ETOILES"); break;
            case 6:  GiveStars(400,  "VOUS RECEVEZ 400 ETOILES");  break;

            // ── MISSIONS POWER-UPS & PUBS ────────────────────
            case 3:  GiveStars(300,  "VOUS RECEVEZ 300 ETOILES");  break;
            case 7:  GiveStars(700,  "VOUS RECEVEZ 700 ETOILES");  break;

            // ── MISSIONS ENNEMIS ─────────────────────────────
            case 9:  GiveStars(150,  "VOUS RECEVEZ 150 ETOILES");  break;
            case 10: GiveStars(300,  "VOUS RECEVEZ 300 ETOILES");  break;
            case 11: GiveStars(500,  "VOUS RECEVEZ 500 ETOILES");  break;
            case 12: GiveStars(800,  "VOUS RECEVEZ 800 ETOILES");  break;

            // Mission 13 — Débloquer un avion (500 ennemis total)
            case 13:
                GivePlane(mission13PlaneIndex, mission13PlaneSprite, "VOUS RECEVEZ " + mission13PlaneName);
                break;

            case 14: GiveStars(750, "VOUS RECEVEZ 750 ETOILES"); break;
        }
    }

    // -------------------------------------------------------
    // Helpers récompenses
    // -------------------------------------------------------
    void GiveStars(int amount, string message)
    {
        PlayerPrefs.SetInt("stars", PlayerPrefs.GetInt("stars", 0) + amount);
        PlayerPrefs.Save();
        if (NotificationScript.instance != null)
            NotificationScript.instance.CallNotif(message, starsSprite);
    }

    // Débloque un avion exactement comme BuyPlaneScript.BuyCurrentPlane
    void GivePlane(int planeIndex, Sprite planeSprite, string message)
    {
        if (PlayerPrefs.GetInt("Unlocked_" + planeIndex, 0) == 1) return;

        PlayerPrefs.SetInt("Unlocked_" + planeIndex, 1);
        PlayerPrefs.Save();

        // Rafraîchir l'UI du sélecteur si on est dans la bonne scène
        if (ChoosingPlaneScript.instance != null && BuyPlaneScript.instance != null)
        {
            int savedIndex = ChoosingPlaneScript.instance.GetCurrentIndex();
            PlaneData data = ChoosingPlaneScript.instance.transform
                .GetChild(savedIndex).GetComponent<PlaneData>();
            BuyPlaneScript.instance.UpdateUI(savedIndex, data);
        }

        if (NotificationScript.instance != null)
            NotificationScript.instance.CallNotif(message, planeSprite);
    }
}
