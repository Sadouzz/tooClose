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
            timerText.text = "Regarder";
            freepackButton.interactable = true;
        }
        else
        {
            //Not finished
            finished = false;
        }
    }

    // Update is called once per frame
    void Update()
    {
        TimeSpan ts = DateTime.Parse(PlayerPrefs.GetString("dateFinishWithAd", DateTime.Now.ToString())).Subtract(DateTime.Now);
        if (!finished)
        {
            freepackButton.interactable = false;
            timerText.text = ts.Minutes + "min" + ts.Seconds + "s";
        }
        else
        {
            timerText.text = "REGARDER UNE PUB";
        }

        if (ts <= TimeSpan.Zero)
        {
            finished = true;
            freepackButton.interactable = true;
        }
        
    }

    public void OnResetTimer()
    {
        finished = false;
        PlayerPrefs.SetString("dateFinishWithAd", DateTime.Now.AddSeconds(interval).ToString());
    }
}
