using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class TimeManagerFreePackWithAd : MonoBehaviour
{
    public Button freepackButton;
    public float interval;
    public bool finished;

    public TextMeshProUGUI timerText;

    [Header("Localization")]
    public string stringTableName = "UITexts"; // Modifiez ceci avec le nom de votre Table
    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    public static TimeManagerFreePackWithAd instance;
    private void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("Il y a plus d'un inventaire");
            return;
        }
        instance = this;
    }

    void Start()
    {
        DateTime dateNow = DateTime.Now;
        DateTime dateFinish = DateTime.Parse(PlayerPrefs.GetString("dateFinishWithAd", DateTime.Now.ToString()));
        TimeSpan difference = dateFinish.Subtract(dateNow);

        if(difference <= TimeSpan.Zero)
        {
            //Finished
            finished = true;
        }
        else
        {
            finished = false;
        }
    }

    void Update()
    {
        TimeSpan ts = DateTime.Parse(PlayerPrefs.GetString("dateFinishWithAd", DateTime.Now.ToString())).Subtract(DateTime.Now);
        
        if (ts <= TimeSpan.Zero)
        {
            finished = true;
        }
        else
        {
            finished = false;
        }

        if (!finished)
        {
            timerText.text = ts.Minutes + GetTranslation("m ", "m ") + ts.Seconds + GetTranslation("s", "s");
        }
        else
        {
            if (AdMob.instance != null && !AdMob.instance.adReady)
            {
                timerText.text = GetTranslation("PAS DE PUB", "PAS DE PUB");
            }
            else
            {
                timerText.text = GetTranslation("REGARDER UNE PUB", "REGARDER UNE PUB");
            }
        }
    }

    public void OnResetTimer()
    {
        finished = false;
        PlayerPrefs.SetString("dateFinishWithAd", DateTime.Now.AddSeconds(interval).ToString());

        // 1 sur 4 : On incremente un compteur, si c'est un multiple de 4, on envoie la notif
        int adPackCount = PlayerPrefs.GetInt("AdPackNotificationCount", 0);
        adPackCount++;
        PlayerPrefs.SetInt("AdPackNotificationCount", adPackCount);

        if (adPackCount % 4 == 0)
        {
            if (PushNotificationManager.instance != null)
            {
                PushNotificationManager.instance.ScheduleNotification("250 étoiles vous attendent !", "Regardez une courte vidéo pour récupérer 250 étoiles !", interval);
            }
        }
    }
}
