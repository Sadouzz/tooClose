using TMPro;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Cinemachine;

public class UIManager : MonoBehaviour
{
    public GameObject playPanel, menuPanel, diePanel, pausePanel, highscorePanel, settingsPanel, shopPanel, infoPanel;
    public TextMeshProUGUI starsText;

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
        SceneManager.LoadScene(0);
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

        PlayerMovement.instance.transform.position = Vector2.zero;
        PlayerMovement.instance.transform.rotation = Quaternion.identity;

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

    public void EnableShopPanel(bool status)
    {
        shopPanel.SetActive(status);
        //shopPanel.GetComponent<Animator>().SetBool("out", status);
    }

    public void EnableInfoPanel(bool status)
    {
        infoPanel.SetActive(status);
    }
}
