using System;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif
#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class PushNotificationManager : MonoBehaviour
{
    public static PushNotificationManager instance;

    private const string CHANNEL_ID = "default_channel";

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeNotifications();
    }

    private void InitializeNotifications()
    {
#if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "Default Channel",
            Importance = Importance.Default,
            Description = "Generic notifications",
        };
        AndroidNotificationCenter.RegisterNotificationChannel(channel);
#endif

#if UNITY_IOS
        StartCoroutine(RequestAuthorization());
#endif
    }

#if UNITY_IOS
    private System.Collections.IEnumerator RequestAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
            {
                yield return null;
            }
        }
    }
#endif

    public void ScheduleNotification(string titleKey, string textKey, float secondsDelay)
    {
        string title = titleKey;
        string text = textKey;

        // Try to translate the title
        string trTitle = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", titleKey);
        if (!string.IsNullOrEmpty(trTitle) && !trTitle.Contains("No translation")) 
        {
            title = trTitle;
        }

        // Try to translate the text
        string trText = UnityEngine.Localization.Settings.LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", textKey);
        if (!string.IsNullOrEmpty(trText) && !trText.Contains("No translation")) 
        {
            text = trText;
        }

#if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = text,
            FireTime = System.DateTime.Now.AddSeconds(secondsDelay)
        };

        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
#endif

#if UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger()
        {
            TimeInterval = new TimeSpan(0, 0, (int)secondsDelay),
            Repeats = false
        };

        var notification = new iOSNotification()
        {
            Identifier = "notif_" + System.DateTime.Now.Ticks.ToString(),
            Title = title,
            Body = text,
            ShowInForeground = true,
            ForegroundPresentationOption = (PresentationOption.Alert | PresentationOption.Sound),
            CategoryIdentifier = "category_a",
            ThreadIdentifier = "thread1",
            Trigger = timeTrigger,
        };

        iOSNotificationCenter.ScheduleNotification(notification);
#endif
        Debug.Log("Push Notification Scheduled in " + secondsDelay + " seconds. Title: " + title);
    }
}
