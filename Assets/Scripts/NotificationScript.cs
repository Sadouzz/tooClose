using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

public class NotificationScript : MonoBehaviour
{
    public GameObject notifPanel;
    public TextMeshProUGUI text;
    public TextMeshProUGUI title;
    public Image reward;

    [Header("Localization")]
    public string stringTableName = "UITexts";

    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    public static NotificationScript instance;

    private Queue<(string label, Sprite sprite)> _queue = new Queue<(string, Sprite)>();
    private bool _isShowing = false;

    private void Awake()
    {
        if (instance != null) return;
        instance = this;
    }

    // -----------------------------------------------------------
    // Point d'entrée unique — ajoute à la file et lance si dispo
    // -----------------------------------------------------------
    public void CallNotif(string _text, Sprite _image)
    {
        _queue.Enqueue((_text, _image));
        if (!_isShowing)
            StartCoroutine(ShowNext());
    }

    // Met à jour uniquement le titre (ex. récompense récupérée)
    public void ChangeTitle(string _text)
    {
        title.text = _text;
    }

    // -----------------------------------------------------------
    // Coroutine : dépile et affiche les notifs une par une
    // -----------------------------------------------------------
    private IEnumerator ShowNext()
    {
        while (_queue.Count > 0)
        {
            _isShowing = true;
            var (label, sprite) = _queue.Dequeue();

            title.text  = GetTranslation("MISSION TERMINÉE", "MISSION TERMINÉE");
            text.text   = label;
            reward.sprite = sprite;

            notifPanel.SetActive(true);
            yield return new WaitForSecondsRealtime(2f);
            notifPanel.SetActive(false);

            // Petite pause entre deux notifs consécutives
            if (_queue.Count > 0)
                yield return new WaitForSecondsRealtime(0.3f);
        }
        _isShowing = false;
    }
}
