using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class AchievementsManager : MonoBehaviour
{
    // -------------------------------------------------------
    // Glisse l'objet "Content" ici â€” les boutons sont collectÃ©s automatiquement
    // -------------------------------------------------------
    public Transform content;

    // Textes de progression affichÃ©s dans l'UI
    public TextMeshProUGUI textMission2;  // missiles dÃ©truits total
    public TextMeshProUGUI textMission4;  // missiles dÃ©truits total
    public TextMeshProUGUI textMission7;  // pubs regardÃ©es
    public TextMeshProUGUI textMission11; // ennemis détruits total
    public TextMeshProUGUI textMission12; // ennemis détruits total
    public TextMeshProUGUI textMission13; // ennemis détruits total
    public TextMeshProUGUI textMission15; // pubs regardées total

    [Header("RÃ©compense Mission 15")]
    [Tooltip("Index de l'avion Ã  dÃ©bloquer (position dans ChoosingPlaneScript, 0 = avion de base)")]
    public int mission15PlaneIndex = 6;
    public Sprite mission15PlaneSprite, starsSprite;
    public string mission15PlaneName = "JET";

    // Boutons collectés dynamiquement (clé = numéro de la mission, valeur = bouton)
    private System.Collections.Generic.Dictionary<int, Button> buttonMap = new System.Collections.Generic.Dictionary<int, Button>();
    private const int MISSION_COUNT = 15;

    public static AchievementsManager instance;

    // -------------------------------------------------------
    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    // -------------------------------------------------------
    void Awake()
    {
        if (instance == null) instance = this;
        CollectButtonsDynamically();
    }

    // -------------------------------------------------------
    // Parcourt Content â†’ chaque enfant Mission â†’ trouve "Button"
    // -------------------------------------------------------
    void CollectButtonsDynamically()
    {
        if (content == null)
        {
            Debug.LogError("[AchievementsManager] Content non assigné !");
            return;
        }

        buttonMap.Clear();
        int activeMissionIndex = 1;

        for (int i = 0; i < content.childCount; i++)
        {
            Transform mission = content.GetChild(i);
            
            // On ignore les missions désactivées dans l'éditeur (comme Mission 8 à 13)
            if (!mission.gameObject.activeSelf) continue;

            Transform btnTransform = mission.Find("Button");
            if (btnTransform != null)
            {
                Button btn = btnTransform.GetComponent<Button>();
                if (btn != null)
                {
                    // Associe le bouton au vrai index actif (1 à 9)
                    buttonMap[activeMissionIndex] = btn;
                    
                    // Sécurité : on force le bouton à envoyer ce bon index quand on clique,
                    // peu importe ce qui est écrit dans l'inspecteur Unity !
                    btn.onClick.RemoveAllListeners();
                    int capturedIndex = activeMissionIndex;
                    btn.onClick.AddListener(() => CollectRewardFromMission(capturedIndex));

                    activeMissionIndex++;
                }
            }
        }
        Debug.Log("[AchievementsManager] " + buttonMap.Count + " boutons actifs collectés et configurés.");
    }

    // -------------------------------------------------------
    void Update()
    {
        RefreshProgressTexts();
        RefreshAllButtons();
    }

    // -------------------------------------------------------
    // Met Ã  jour les textes de progression
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
        if (textMission15 != null) textMission15.text = ads.ToString();
    }

    // -------------------------------------------------------
    // RafraÃ®chit l'Ã©tat interactable de tous les boutons
    // -------------------------------------------------------
    void RefreshAllButtons()
    {
        foreach (var kvp in buttonMap)
        {
            int missionIndex = kvp.Key;
            Button btn = kvp.Value;

            if (btn == null) continue;

            bool completed = PlayerPrefs.GetString("mission" + missionIndex, "no") == "yes";
            bool collected = PlayerPrefs.GetString("mission" + missionIndex + "Collected", "no") == "yes";

            var label = btn.transform.GetChild(0).GetComponent<TextMeshProUGUI>();

            if (completed && !collected)
            {
                if (label != null) label.text = GetTranslation("COLLECTER", "COLLECTER");
                btn.interactable = true;
            }
            else
            {
                if (collected)
                {
                    if (label != null) label.text = GetTranslation("DEJA_COLLECTE", "DEJA COLLECTE");
                }
                else
                {
                    if (label != null) label.text = GetTranslation("NON_COMPLETE", "NON COMPLETE");
                }
                btn.interactable = false;
            }
        }
    }

    // -------------------------------------------------------
    // AppelÃ© par chaque bouton (passer l'index 1-based)
    // -------------------------------------------------------
    public void CollectRewardFromMission(int _index)
    {
        Debug.Log("[AchievementsManager] Attempting to collect reward for mission: " + _index);
        if (!buttonMap.ContainsKey(_index) || buttonMap[_index] == null) 
        {
            Debug.LogError("[AchievementsManager] Failed to collect: mission " + _index + " introuvable dans l'UI.");
            return;
        }

        Button clickedBtn = buttonMap[_index];
        clickedBtn.interactable = false;
        PlayerPrefs.SetString("mission" + _index + "Collected", "yes");
        PlayerPrefs.Save();
        
        Debug.Log("[AchievementsManager] Granting reward for mission " + _index);

        GrantReward(_index, clickedBtn.transform);
    }

    // -------------------------------------------------------
    // Vérifie si la mission est active dans l'UI (non grisée/désactivée)
    // -------------------------------------------------------
    public bool IsMissionActive(int index)
    {
        return buttonMap.ContainsKey(index) && buttonMap[index] != null && buttonMap[index].transform.parent.gameObject.activeInHierarchy;
    }

    // -------------------------------------------------------
    // Attribution des rÃ©compenses par mission
    // -------------------------------------------------------
    void GrantReward(int index, Transform btnTransform)
    {
        switch (index)
        {
            // ── MISSIONS TEMPS ──────────────────────────────
            case 1:  GiveStars(200, btnTransform);  break;
            case 5:  GiveStars(350, btnTransform);  break;
            case 8:  GiveStars(600, btnTransform);  break;
            // Mission 9 : Dernière mission du jeu, débloque l'avion
            case 9:  GivePlane(mission15PlaneIndex, mission15PlaneSprite, btnTransform); break;

            // ── MISSIONS MISSILES ────────────────────────────
            case 2:  GiveStars(500, btnTransform);  break;
            case 4:  GiveStars(1000, btnTransform); break;
            case 6:  GiveStars(400, btnTransform);  break;

            // ── MISSIONS POWER-UPS & PUBS ────────────────────
            case 3:  GiveStars(300, btnTransform);  break;
            case 7:  GiveStars(700, btnTransform);  break;

            // ── MISSIONS ENNEMIS ─────────────────────────────
            /*case 9:  GiveStars(150, btnTransform);  break;
            case 10: GiveStars(300, btnTransform);  break;
            case 11: GiveStars(500, btnTransform);  break;
            case 12: GiveStars(800, btnTransform);  break;

            // Mission 13 — Débloquer un avion (500 ennemis total)
            case 13: GiveStars(1500, btnTransform); break;

            // Mission 14 — 30 ennemis en une partie
            case 14: GiveStars(750, btnTransform); break;

/*case 15: GivePlane(mission15PlaneIndex, mission15PlaneSprite, btnTransform); break;
            // Mission 15 : Regarder 30 pubs (Désactivée)
            case 15: GiveStars(0, btnTransform); break;*/
        }
    }

    // -------------------------------------------------------
    // Helpers rÃ©compenses
    // -------------------------------------------------------
    void GiveStars(int amount, Transform btnTransform)
    {
        PlayerPrefs.SetInt("stars", PlayerPrefs.GetInt("stars", 0) + amount);
        PlayerPrefs.Save();
        
        StartCoroutine(AnimateReward(starsSprite, btnTransform, false));
    }

    // DÃ©bloque un avion exactement comme BuyPlaneScript.BuyCurrentPlane
    void GivePlane(int planeIndex, Sprite planeSprite, Transform btnTransform)
    {
        if (PlayerPrefs.GetInt("Unlocked_" + planeIndex, 0) == 1) return;

        PlayerPrefs.SetInt("Unlocked_" + planeIndex, 1);
        PlayerPrefs.Save();

        // RafraÃ®chir l'UI du sÃ©lecteur si on est dans la bonne scÃ¨ne
        if (ChoosingPlaneScript.instance != null && BuyPlaneScript.instance != null)
        {
            int savedIndex = ChoosingPlaneScript.instance.GetCurrentIndex();
            PlaneData data = ChoosingPlaneScript.instance.transform
                .GetChild(savedIndex).GetComponent<PlaneData>();
            BuyPlaneScript.instance.UpdateUI(savedIndex, data);
        }

        StartCoroutine(AnimateReward(planeSprite, btnTransform, true));
    }

    IEnumerator AnimateReward(Sprite sprite, Transform startTransform, bool isPlane)
    {
        // Création de l'objet UI pour l'animation
        GameObject animObj = new GameObject("RewardAnim");
        animObj.transform.SetParent(content.parent.parent, false); // On le met par-dessus, souvent sur le Canvas principal
        animObj.transform.SetAsLastSibling();
        
        Image img = animObj.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;

        RectTransform rt = img.rectTransform;
        rt.position = startTransform.position;
        rt.sizeDelta = isPlane ? new Vector2(250, 250) : new Vector2(100, 100);

        Vector3 startPos = rt.position;
        
        // 1. Pop animation (comme dans PickUpStar)
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime * 5f;
            float scale = Mathf.Sin(t * Mathf.PI * 0.8f) * 1.5f;
            rt.localScale = Vector3.one * scale;
            yield return null;
        }
        rt.localScale = Vector3.one;

        yield return new WaitForSeconds(0.2f);

        if (isPlane)
        {
            // Animation spéciale pour l'avion : Grossit et disparait (fade out)
            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 1.5f;
                rt.localScale = Vector3.one * (1f + (t * 0.5f));
                img.color = new Color(1, 1, 1, 1f - t);
                yield return null;
            }
        }
        else
        {
            // Animation étoile : Vole vers le compteur d'étoiles
            Transform targetTransform = null;
            if (UIManager.instance != null && UIManager.instance.starsText != null)
            {
                targetTransform = UIManager.instance.starsText.transform;
            }

            // Si pas trouvé on vole vers le haut au milieu
            Vector3 endPos = targetTransform != null ? targetTransform.position : new Vector3(Screen.width / 2f, Screen.height - 100f, 0);

            t = 0;
            while (t < 1)
            {
                t += Time.deltaTime * 2.5f;
                float easedT = t * t; // Accélération
                rt.position = Vector3.Lerp(startPos, endPos, easedT);
                rt.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.4f, easedT);
                yield return null;
            }

            if (targetTransform != null)
            {
                // Animation de pulsation du texte d'étoiles
                StartCoroutine(PulseText(targetTransform));
                
                // Mettre à jour visuellement le compteur d'étoiles
                UIManager.instance.starsText.text = PlayerPrefs.GetInt("stars", 0).ToString();
            }
        }

        Destroy(animObj);
    }

    IEnumerator PulseText(Transform textTransform)
    {
        float t = 0;
        Vector3 startScale = textTransform.localScale;
        // Evite que ça grossisse à l'infini si on spamme
        if (startScale.x > 1.5f) startScale = Vector3.one;

        while (t < 1)
        {
            t += Time.deltaTime * 6f;
            float scale = Mathf.Lerp(1f, 1.4f, Mathf.Sin(t * Mathf.PI));
            textTransform.localScale = startScale * scale;
            yield return null;
        }
        textTransform.localScale = startScale;
    }
}
