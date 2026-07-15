using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class UIManager : MonoBehaviour
{
    public GameObject playPanel, menuPanel, diePanel, pausePanel, highscorePanel, settingsPanel, shopPanel, infoPanel, missionsPanel, upgradePanel;
    public TextMeshProUGUI starsText;
    
    [Header("Missions")]
    public GameObject missionsBadge; // Le badge visuel (ex: un point rouge) sur le bouton mission
    private float badgeCheckTimer = 0f;

    public static UIManager instance;

    [Header("Camera Settings")]
    public CinemachineCamera vcam;
    public float transitionDuration = 0.5f;

    [Header("Audio Settings")]
    public AudioSource musicSource;
    public float volumeInPlay = 0.4f; // La valeur 'x' que tu souhaites
    public float fadeDuration = 1.5f; // Temps pour descendre le volume

    // Fonction utilitaire pour reset l'offset


    private void Awake()
    {
        if (instance != null)
        {
            return;
        }
        instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        starsText.text = PlayerPrefs.GetInt("stars", 0).ToString();

        // Vérifie les récompenses en attente (badge) toutes les secondes
        badgeCheckTimer -= Time.deltaTime;
        if (badgeCheckTimer <= 0)
        {
            badgeCheckTimer = 1f;
            CheckMissionsBadge();
        }
    }

    void CheckMissionsBadge()
    {
        if (missionsBadge == null) return;
        
        bool hasPendingReward = false;
        // On vérifie les 15 missions
        for (int i = 1; i <= 15; i++)
        {
            bool completed = PlayerPrefs.GetString("mission" + i, "no") == "yes";
            bool collected = PlayerPrefs.GetString("mission" + i + "Collected", "no") == "yes";
            if (completed && !collected)
            {
                hasPendingReward = true;
                break;
            }
        }
        
        missionsBadge.SetActive(hasPendingReward);
    }

    /*public void SaveData()
    { 
        PlayerPrefs.SetInt("stars", PlayerPrefs.GetInt("stars", 0) + Inventory.instance.starsPicked);
    }*/

    private IEnumerator SmoothCameraOffset(float targetY)
    {
        var composer = vcam.GetComponent<CinemachinePositionComposer>();
        if (composer == null) yield break;

        float startY = composer.TargetOffset.y;
        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float percent = elapsed / transitionDuration;

            // On utilise Lerp pour une transition fluide
            // "SmoothStep" rend le début et la fin encore plus doux
            composer.TargetOffset.y = Mathf.SmoothStep(startY, targetY, percent);

            yield return null;
        }

        composer.TargetOffset.y = targetY; // Sécurité finale
    }

    public void OpenDiePanel(string time, int totalSeconds, int score, int destroyedTurrets, int stars)
    {
        StartCoroutine(OpenDiePanelCoroutine(time, totalSeconds, score, destroyedTurrets, stars));
    }

    public IEnumerator OpenDiePanelCoroutine(string time, int totalSeconds, int score, int destroyedTurrets, int stars)
    {
        diePanel.SetActive(true);

        yield return null; // attendre Awake()

        DieManagerUI.instance.DisplayPanel(time, totalSeconds, score, destroyedTurrets, stars);

        diePanel.GetComponent<Animator>().SetBool("out", true);
        PlayerMovement.instance.move = false;

        /*yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0;*/
    }

    public void Home()
    {
        Time.timeScale = 1;

        // Sauvegarder la progression si le joueur quitte la partie en cours depuis le menu pause
        if (Inventory.instance != null && Inventory.instance.inPlay && !Inventory.instance.dead)
        {
            int sessionStars = Inventory.instance.CalculateStars();
            Inventory.instance.SaveData(sessionStars);
        }
        
        // --- SOFT RESET : On nettoie la scène au lieu de la recharger ---
        
        // 1. Reset des données
        Inventory.instance.ResetData();

        // 2. Nettoyage de l'écran
        MissileSpawner.instance.DestroyAllMissiles();
        SpawnObjects.instance.DestroyAllObjects();
        PowerUpUIManager.instance.ClearStoredPowerUp();
        PlayerPowerUpManager.instance.Reset();

        // 3. Reset du joueur et de la caméra
        if (vcam != null) vcam.enabled = false;
        
        PlayerMovement.instance.transform.position = Vector2.zero;
        PlayerMovement.instance.transform.rotation = Quaternion.identity;
        PlayerMovement.instance.ResetCameraProxy();
        
        Rigidbody2D rb = PlayerMovement.instance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        if (vcam != null)
        {
            vcam.transform.position = new Vector3(0, 0, vcam.transform.position.z);
            var composer = vcam.GetComponent<CinemachinePositionComposer>();
            if (composer != null) 
            {
                // Dans le menu principal, l'offset Y doit être à 2.25 (et 0 dans le jeu)
                composer.TargetOffset = new Vector3(composer.TargetOffset.x, 2.25f, composer.TargetOffset.z);
            }
            vcam.enabled = true;
        }

        if (Camera.main != null)
            Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);

        ChunkManager[] chunkManagers = FindObjectsByType<ChunkManager>(FindObjectsSortMode.None);
        foreach (ChunkManager cm in chunkManagers)
        {
            cm.ForceUpdateChunks();
        }
        
        GameObject playerShip = PlayerMovement.instance.transform.GetChild(0).GetChild(ChoosingPlaneScript.instance.currentIndex).gameObject;
        playerShip.SetActive(true);

        // 4. Reset des états
        Inventory.instance.inPlay = false;
        Inventory.instance.dead = false;
        Inventory.instance.menu = true;
        PlayerMovement.instance.move = false;

        // 5. Affichage des panels pour le menu principal
        playPanel.SetActive(false);
        diePanel.SetActive(false);
        pausePanel.SetActive(false);
        menuPanel.SetActive(true);

        // Remettre la musique au volume d'origine du menu (si on avait fait un fade)
        if (musicSource != null)
        {
            StopAllCoroutines(); // Stoppe le fade en cours
            musicSource.volume = 1f; // Remet le volume à 100% pour le menu (ou la valeur que vous préférez)
        }
    }

    public void Play()
    {
        playPanel.SetActive(true);
        menuPanel.SetActive(false);
        diePanel.SetActive(false);

        if (vcam != null)
        {
            // On ne fait plus StopAllCoroutines ici car cela stopperait aussi le fade du son
            // On stoppe spécifiquement la coroutine de la caméra si nécessaire
            StartCoroutine(SmoothCameraOffset(0f));
        }

        // --- LANCEMENT DU FADE SONORE ---
        if (musicSource != null)
        {
            StartCoroutine(FadeMusicVolume(volumeInPlay));
        }

        Inventory.instance.inPlay = true;
        Inventory.instance.dead = false;
        Inventory.instance.menu = false;
        PlayerMovement.instance.move = true;
    }

    IEnumerator FadeMusicVolume(float targetVolume)
    {
        float startVolume = musicSource.volume;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(startVolume, targetVolume, timer / fadeDuration);
            yield return null;
        }

        musicSource.volume = targetVolume;
    }

    public void EnablePausePanel(bool status)
    {
        pausePanel.SetActive(status);
        if (status)
        { 
            Time.timeScale = 0;
        }
    }

    public void Resume()
    {
        EnablePausePanel(false);
        Time.timeScale = 1;
    }

    public void Retry()
    {
        // 1. Reset des scores et du temps
        Inventory.instance.ResetData();

        // 2. Reset de la position et de la rotation
        MissileSpawner.instance.DestroyAllMissiles();
        SpawnObjects.instance.DestroyAllObjects();
        PowerUpUIManager.instance.ClearStoredPowerUp();
        PlayerPowerUpManager.instance.Reset();

        // Disable Cinemachine virtual camera to prevent it from interpolating/damping from old position
        if (vcam != null)
        {
            vcam.enabled = false;
        }

        PlayerMovement.instance.transform.position = Vector2.zero;
        PlayerMovement.instance.transform.rotation = Quaternion.identity;
        PlayerMovement.instance.ResetCameraProxy();

        if (vcam != null)
        {
            vcam.transform.position = new Vector3(0, 0, vcam.transform.position.z);
            
            // If there's a composer offset, reset it immediately to avoid offset lag
            var composer = vcam.GetComponent<CinemachinePositionComposer>();
            if (composer != null)
            {
                composer.TargetOffset = Vector3.zero;
            }
            
            vcam.enabled = true;
        }

        if (Camera.main != null)
        {
            Camera.main.transform.position = new Vector3(0, 0, Camera.main.transform.position.z);
        }

        // Force all ChunkManagers in the scene to immediately update active chunks around the player's new position
        ChunkManager[] chunkManagers = FindObjectsByType<ChunkManager>(FindObjectsSortMode.None);
        foreach (ChunkManager cm in chunkManagers)
        {
            cm.ForceUpdateChunks();
        }

        GameObject playerShip = PlayerMovement.instance.transform.GetChild(0).GetChild(ChoosingPlaneScript.instance.currentIndex).gameObject;
        playerShip.SetActive(true);

        // 3. Reset de la physique (très important pour éviter l'élan résiduel)
        Rigidbody2D rb = PlayerMovement.instance.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }

        // 4. On relance la logique de jeu
        Play();
    }

    public void EnableSettingsPanel(bool status)
    {
        settingsPanel.SetActive(status);
        //settingsPanel.GetComponent<Animator>().SetBool("out", status);
    }

    public void EnableMissionsPanel(bool status)
    {
        missionsPanel.SetActive(status);
    }

    public void EnableShopPanel(bool status)
    {
        shopPanel.SetActive(status);
        //shopPanel.GetComponent<Animator>().SetBool("out", status);
    }

    public void EnableInfoPanel(bool status)
    {
        infoPanel.SetActive(status);
    }

    public void EnableUpgradePanel(bool status)
    {
        upgradePanel.SetActive(status);
    }
}
