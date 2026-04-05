using TMPro;
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Inventory : MonoBehaviour
{
    public static Inventory instance;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI destroyedMissilesText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI pickedStarsText;

    [Header("Current Run Stats")]
    public int score;
    public int scoreMultiplier = 1;
    public int starsPicked;
    public float totalSeconds;

    private int addedStarsLastDie; // Pour la soustraction si Ads
    private float scoreTimer;

    [Header("State")]
    public bool inPlay = false;
    public bool dead = false, menu = true;
    public GameObject player; // Assigne l'objet qui contient le PlayerMovement

    /*[Header("Near Miss System")]
    public TextMeshProUGUI nearMissText; // Assigne un TextMeshPro caché par défaut
    public Animator nearMissAnimator;    // L'animateur du texte
    private int nearMissCombo = 0;
    private float comboResetTimer;
    private const float COMBO_LEEWAY = 1.5f;*/


    private void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if (!inPlay || dead) return;

        // Gestion du temps
        if (player == null) player = GameObject.FindGameObjectWithTag("Player");


        totalSeconds += Time.deltaTime;
        TimeSpan ts = TimeSpan.FromSeconds(totalSeconds);
        timerText.text = string.Format("{0:00}:{1:00}:{2:00}", ts.Hours, ts.Minutes, ts.Seconds);
        UIManager.instance.highscorePanel.SetActive(false);

        /*if (comboResetTimer > 0)
        {
            comboResetTimer -= Time.deltaTime;
            if (comboResetTimer <= 0) nearMissCombo = 0;
        }*/

        // Gestion du score automatique
        scoreTimer += Time.deltaTime;
        if (scoreTimer > 0.1f)
        {
            score += 1 * scoreMultiplier;
            scoreText.text = score.ToString();
            scoreTimer = 0;
        }
    }

    public void AddStars(int amount)
    {
        starsPicked += amount;
        pickedStarsText.text = starsPicked.ToString();
    }

    public void RefreshDestroyedMissiles(int amount)
    {
        destroyedMissilesText.text = amount.ToString();
    }

    public void DieProcess()
    {
        if (dead) return;
        StartCoroutine(ExecuteDeathSequence());
    }

    /*IEnumerator ExecuteDeathSequence()
    {
        dead = true;
        inPlay = false;

        // --- 1. FREEZE FRAME (Le choc) ---
        // On bloque le temps totalement
        Time.timeScale = 0f;

        // On lance la secousse d'écran (qui utilise unscaledDeltaTime donc elle bouge quand même !)
        if (CameraShake.instance != null)
            CameraShake.instance.Shake(0.25f, 0.4f);

        // On attend 0.2 secondes réelles pendant que le jeu est figé
        yield return new WaitForSecondsRealtime(0.2f);

        // --- 2. IMPACT & EXPLOSION ---
        // On remet le temps à 1 pour que les animations/particules fonctionnent
        Time.timeScale = 1f;

        // Désactive le visuel et lance ton effet d'explosion ici
        player.SetActive(false);
        // Exemple : Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);

        // --- 3. CALCULS ET UI ---
        int sessionStars = CalculateStars();
        SaveData(sessionStars);

        // On attend un tout petit peu que l'explosion soit visible avant le panel
        yield return new WaitForSeconds(0.5f);

        UIManager.instance.OpenDiePanel(
            timerText.text,
            (int)totalSeconds,
            score,
            MissileSpawner.instance.destroyedMissiles,
            starsPicked
        );
    }*/

    IEnumerator ExecuteDeathSequence()
    {
        dead = true;
        inPlay = false;

        // 1. LE CHOC (Freeze + Shake + Zoom)
        Time.timeScale = 0f; // On fige tout

        if (CameraShake.instance != null)
        {
            // On secoue fort (Amplitude 3.0)
            CameraShake.instance.Shake(0.3f, 3f);
            // On zoom sur le crash (Taille ortho de 5.0 à 3.0 par exemple)
            CameraShake.instance.ImpactZoom(3f, 10f, 2f, 0.5f);
        }

        // On laisse le joueur "admirer" son crash pendant 0.25s
        yield return new WaitForSecondsRealtime(0.25f);

        // 2. L'EXPLOSION (On libère le temps)
        Time.timeScale = 1f;
        player.SetActive(false);

        // Ici, fais apparaître ton explosion de particules
        // Instantiate(explosionPrefab, player.transform.position, Quaternion.identity);

        yield return new WaitForSeconds(0.6f);

        // --- 3. UI ---
        UIManager.instance.OpenDiePanel(
            timerText.text,
            (int)totalSeconds,
            score,
            MissileSpawner.instance.destroyedMissiles,
            starsPicked
        );
    }

    int CalculateStars()
    {
        // Ta logique de conversion Score/Temps -> Etoiles
        return (int)(totalSeconds / 10) + (score / 10) + MissileSpawner.instance.destroyedMissiles + starsPicked;
    }

    public void SaveData(int stars)
    {
        // 1. Mise à jour du total d'étoiles permanent
        addedStarsLastDie = stars;
        int currentTotal = PlayerPrefs.GetInt("stars", 0);
        PlayerPrefs.SetInt("stars", currentTotal + stars);
        // On récupère la difficulté et on compare directement pour obtenir un booléen
        bool isHardMode = PlayerPrefs.GetString("Difficulty", "Easy") != "Easy";

        // On envoie le résultat au script de Highscore
        HighScoreScript.instance.SaveNewScore(score, isHardMode);

    }

    // Appelé par le bouton "Revive / Watch Ad"
    public void AdsReward()
    {
        // 1. On annule les étoiles gagnées (puisqu'on continue la partie)
        int currentTotal = PlayerPrefs.GetInt("stars", 0);
        PlayerPrefs.SetInt("stars", currentTotal - addedStarsLastDie);

        // 2. On réinitialise les états
        dead = false;
        inPlay = true;

        // 3. ON RÉACTIVE LE JOUEUR
        player.SetActive(true);

        // 4. On lance l'invincibilité et le clignotement
        if (PlayerMovement.instance != null)
        {
            StartCoroutine(PlayerMovement.instance.InvincibleTiming());
            PlayerMovement.instance.move = true;
        }

        // 5. Nettoyage de l'écran
        MissileSpawner.instance.DestroyAllMissiles();
        
        

        // 6. UI
        UIManager.instance.diePanel.SetActive(false);
        
    }

    public void ResetData()
    {
        // Réinitialisation des statistiques numériques
        score = 0;
        scoreTimer = 0;
        totalSeconds = 0;
        starsPicked = 0;
        addedStarsLastDie = 0;
        MissileSpawner.instance.ResetData();
        // Réinitialisation de l'UI
        scoreText.text = "0";
        timerText.text = "00:00:00";
        pickedStarsText.text = "0";
        destroyedMissilesText.text = "0";

        

        // Réinitialisation des états
        //dead = false;
        //inPlay = true; // On repasse en jeu
    }

    /*public void RegisterNearMiss(Vector3 missilePos)
    {
        // Calcul du combo
        comboResetTimer = 1.5f;
        nearMissCombo++;

        // Calcul du score
        int gain = 50 * nearMissCombo;
        score += gain;
        scoreText.text = score.ToString();

        // APPEL DE L'ANIMATION COOL
        //PowerUpUIManager.instance.ShowNearMissFeedback(nearMissCombo, gain, missilePos);

        // Effet de ralenti (optionnel mais recommandé pour le feeling)
        //StartCoroutine(BriefSlowMotion());
    }*/

    /*public void RegisterNearMiss(float proximityBonus)
    {
        // 1. Calcul du combo
        comboResetTimer = COMBO_LEEWAY;
        nearMissCombo++;

        // 2. Calcul du gain : (Valeur fixe + Bonus proximité) * Combo
        int gain = Mathf.RoundToInt((50 * proximityBonus) * nearMissCombo);
        score += gain;
        scoreText.text = score.ToString();

        // 3. Affichage du texte animé
        if (nearMissText != null)
        {
            nearMissText.text = "NEAR MISS x" + nearMissCombo + "\n+" + gain;
            // Déclenche l'animation (nomme ton trigger "Pop" dans l'Animator)
            nearMissAnimator.SetTrigger("Pop");
        }
    }*/
}