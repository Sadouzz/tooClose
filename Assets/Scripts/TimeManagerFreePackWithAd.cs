using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TimeManagerFreePackWithAd : MonoBehaviour
{
    public Button freepackButton;
    public float interval;
    public bool finished;

    public TextMeshProUGUI timerText;

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
    // Start is called before the first frame update
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
            timerText.text = ts.Minutes + "m " + ts.Seconds + "s";
        }
        else
        {
            if (AdMob.instance != null && !AdMob.instance.adReady)
            {
                timerText.text = "PAS DE PUB";
            }
            else
            {
                timerText.text = "REGARDER UNE PUB";
            }
        }
    }

    public void OnResetTimer()
    {
        finished = false;
        PlayerPrefs.SetString("dateFinishWithAd", DateTime.Now.AddSeconds(interval).ToString());
    }
}
