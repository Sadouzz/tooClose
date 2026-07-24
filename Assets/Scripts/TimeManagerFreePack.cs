using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class TimeManagerFreePack : MonoBehaviour
{
    public Button freepackButton;
    //public float totalSecondsRemaining;
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

    public static TimeManagerFreePack instance;

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
        //SceneManager.sceneUnloaded += OnSceneUnloaded;

        DateTime dateNow = DateTime.Now;
        //DateTime dateQuit = DateTime.Parse(PlayerPrefs.GetString("dateQuit", ""));
        DateTime dateFinish = DateTime.Parse(PlayerPrefs.GetString("dateFinish", DateTime.Now.ToString()));
        Debug.Log("DateFinish " + dateFinish);
        Debug.Log("DateNow " + dateNow);
        //TimeSpan interval = dateNow - dateQuit;
        //totalSecondsRemaining = PlayerPrefs.GetFloat("totalSecondsRemaining", 900);
        //TimeSpan remainingTime = TimeSpan.FromSeconds(totalSecondsRemaining);
        /*if(totalSecondsRemaining == 0)
        {
            remainingTime = TimeSpan.Zero;
        }*/

        //Debug.Log("Test" + Convert.ToSingle(dateNow.Subtract(dateFinish)));

        //TimeSpan difference = dateNow.Subtract(dateQuit);

        TimeSpan difference = dateFinish.Subtract(dateNow);
        Debug.Log("difference " + difference);

        //TimeSpan ts = remainingTime - difference;


        if (difference <= TimeSpan.Zero)
        {
            //Finished
            finished = true;
            timerText.text = GetTranslation("Collecter", "Collecter");
            freepackButton.interactable = true;
        }
        else
        {
            //Not finished
            finished = false;
            //totalSecondsRemaining = Convert.ToSingle(difference.TotalSeconds);
        }
    }

    // Update is called once per frame
    void Update()
    {
        TimeSpan ts = DateTime.Parse(PlayerPrefs.GetString("dateFinish", DateTime.Now.ToString())).Subtract(DateTime.Now);
        if (!finished)
        {
            freepackButton.interactable = false;
            //totalSecondsRemaining -= Time.deltaTime;
            //TimeSpan ts = TimeSpan.FromSeconds(totalSecondsRemaining);
            
            timerText.text = ts.Minutes + GetTranslation("min", "min") + ts.Seconds + GetTranslation("s", "s");
        }
        else
        {
            timerText.text = GetTranslation("Collecter", "Collecter");
        }

        if(ts <= TimeSpan.Zero)
        {
            finished = true;
            freepackButton.interactable = true;
        }
        
    }

    public void OnResetTimer()
    {
        //totalSecondsRemaining = 900;
        finished = false;
        PlayerPrefs.SetString("dateFinish", DateTime.Now.AddSeconds(900).ToString());   
    }

    /*private void OnApplicationQuit()
    {
        SavingTime();
    }*/
    /*void OnSceneUnloaded(Scene scene)
    {
        SavingTime();
        Debug.Log("Change Scene");
    }*/

    public void SavingTime()
    {
        //DateTime dateQuit = DateTime.Now;
        /*if(totalSecondsRemaining <= 0)
        {
            PlayerPrefs.SetFloat("totalSecondsRemaining", 0);
        }
        else
        {
            PlayerPrefs.SetFloat("totalSecondsRemaining", totalSecondsRemaining);
        }*/
        
        //PlayerPrefs.SetString("dateQuit", DateTime.Now.ToString());
    }
}
