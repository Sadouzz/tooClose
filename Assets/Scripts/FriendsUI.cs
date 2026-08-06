using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;

/// <summary>
/// Gère l'interface sociale (Ajout d'amis, affichage du PlayerID, liste des requêtes).
/// </summary>
public class FriendsUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject socialPanel;

    [Header("Onglets Principaux")]
    public Button tabAmisButton;
    public Button tabRequetesButton;
    public GameObject amisPanel;
    public GameObject requetesPanel;
    public Color tabActiveColor = new Color(0.2f, 0.6f, 1f);
    public Color tabInactiveColor = new Color(0.3f, 0.3f, 0.3f);

    [Header("Mon Profil (Amis Panel)")]
    public TextMeshProUGUI[] myPlayerIdTexts;
    public Button[] copyIdButtons;

    [Header("Liste d'amis (Amis Panel)")]
    public Transform friendsContainer;
    public GameObject friendRowPrefab; // Préfab avec NameText

    [Header("Ajouter un ami (Requêtes Panel)")]
    public TMP_InputField friendIdInput;
    public Button addFriendButton;

    [Header("Toast Notification (InfoPanelCode)")]
    public GameObject infoPanel;
    public TextMeshProUGUI infoPanelText;

    [Header("Requêtes reçues (Requêtes Panel)")]
    public Transform requestsContainer;
    public GameObject requestRowPrefab; // Préfab avec NameText et AcceptButton

    void Start()
    {
        if (socialPanel != null) socialPanel.SetActive(false);
        
        if (tabAmisButton != null) tabAmisButton.onClick.AddListener(() => SwitchTab(true));
        if (tabRequetesButton != null) tabRequetesButton.onClick.AddListener(() => SwitchTab(false));

        if (copyIdButtons != null)
        {
            foreach (var btn in copyIdButtons)
            {
                if (btn != null) btn.onClick.AddListener(CopyMyId);
            }
        }
        if (addFriendButton != null) addFriendButton.onClick.AddListener(SendRequest);

        FriendsManager.OnFriendsUpdated += RefreshUI;
        FriendsManager.OnFriendRequestReceived += OnRequestReceived;
    }

    void OnDestroy()
    {
        FriendsManager.OnFriendsUpdated -= RefreshUI;
        FriendsManager.OnFriendRequestReceived -= OnRequestReceived;
    }

    public void SwitchTab(bool isAmis)
    {
        if (amisPanel != null) amisPanel.SetActive(isAmis);
        if (requetesPanel != null) requetesPanel.SetActive(!isAmis);

        if (tabAmisButton != null)
        {
            var img = tabAmisButton.GetComponent<Image>();
            if (img != null) img.color = isAmis ? tabActiveColor : tabInactiveColor;
        }
        if (tabRequetesButton != null)
        {
            var img = tabRequetesButton.GetComponent<Image>();
            if (img != null) img.color = isAmis ? tabInactiveColor : tabActiveColor;
        }
    }

    public void OpenSocialPanel()
    {
        if (socialPanel != null) socialPanel.SetActive(true);
        if (infoPanel != null) infoPanel.SetActive(false);
        
        SwitchTab(true); // Ouvre sur l'onglet Amis par défaut
        
        if (AuthManager.instance != null && myPlayerIdTexts != null)
        {
            foreach (var txt in myPlayerIdTexts)
            {
                if (txt != null) txt.text = AuthManager.instance.PlayerId;
            }
        }

        RefreshUI();
    }

    public void CloseSocialPanel()
    {
        if (socialPanel != null) socialPanel.SetActive(false);
    }

    private string GetTranslation(string key, string fallback)
    {
        try
        {
            string tr = LocalizationSettings.StringDatabase.GetLocalizedString("UITexts", key);
            if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
            return tr;
        }
        catch
        {
            return fallback;
        }
    }

    private void CopyMyId()
    {
        if (AuthManager.instance != null && !string.IsNullOrEmpty(AuthManager.instance.PlayerId))
        {
            GUIUtility.systemCopyBuffer = AuthManager.instance.PlayerId;
            ShowInfoToast(GetTranslation("ID_COPIE", "ID Copie"));
        }
    }

    private async void SendRequest()
    {
        if (friendIdInput == null || string.IsNullOrEmpty(friendIdInput.text)) return;

        if (addFriendButton != null) addFriendButton.interactable = false;
        
        bool success = await FriendsManager.instance.SendFriendRequestAsync(friendIdInput.text);

        ShowInfoToast(success ? GetTranslation("DEMANDE_ENVOYEE", "Demande envoyee") : GetTranslation("ERREUR_AMI", "Erreur ou deja ami"));

        friendIdInput.text = "";
        if (addFriendButton != null) addFriendButton.interactable = true;
    }

    private void ShowInfoToast(string message)
    {
        if (infoPanel != null && infoPanelText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ToastCoroutine(message));
        }
    }

    private System.Collections.IEnumerator ToastCoroutine(string message)
    {
        infoPanelText.text = message;
        infoPanel.SetActive(true);

        // Récupérer ou ajouter un CanvasGroup pour l'opacité
        CanvasGroup canvasGroup = infoPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = infoPanel.AddComponent<CanvasGroup>();

        RectTransform rectTransform = infoPanel.GetComponent<RectTransform>();
        
        float transitionDuration = 0.25f;
        float waitDuration = 2.0f;
        float timer = 0f;

        // Zoom In & Fade In
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            // Courbe d'interpolation douce (ease-out)
            float smoothT = t * (2f - t); 
            
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, smoothT);
            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(new Vector3(0.8f, 0.8f, 0.8f), Vector3.one, smoothT);
            
            yield return null;
        }

        canvasGroup.alpha = 1f;
        if (rectTransform != null) rectTransform.localScale = Vector3.one;

        // Wait
        yield return new WaitForSeconds(waitDuration);

        // Zoom Out & Fade Out
        timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            // Courbe d'interpolation douce (ease-in)
            float smoothT = t * t;

            canvasGroup.alpha = Mathf.Lerp(1f, 0f, smoothT);
            if (rectTransform != null)
                rectTransform.localScale = Vector3.Lerp(Vector3.one, new Vector3(0.8f, 0.8f, 0.8f), smoothT);

            yield return null;
        }

        canvasGroup.alpha = 0f;
        infoPanel.SetActive(false);
    }

    private void OnRequestReceived(string requesterId)
    {
        if (socialPanel != null && socialPanel.activeSelf)
        {
            RefreshUI();
        }
        else
        {
            // Peut-être afficher une pastille de notification sur le bouton Social ici
        }
    }

    public void RefreshUI()
    {
        if (socialPanel == null || !socialPanel.activeSelf || FriendsManager.instance == null) return;

        // 1. Nettoyer et lister les amis (Amis Panel)
        if (friendsContainer != null)
        {
            foreach (Transform child in friendsContainer)
            {
                Destroy(child.gameObject);
            }

            var friends = FriendsManager.instance.GetFriends();
            foreach (var friend in friends)
            {
                if (friendRowPrefab != null)
                {
                    GameObject row = Instantiate(friendRowPrefab, friendsContainer);
                    var nameTxt = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    if (nameTxt != null) nameTxt.text = friend.Member.Profile?.Name ?? friend.Member.Id;

                    var removeBtn = row.transform.Find("Remove")?.GetComponent<Button>();
                    if (removeBtn != null)
                    {
                        string friendId = friend.Member.Id;
                        removeBtn.onClick.AddListener(async () =>
                        {
                            removeBtn.interactable = false;
                            bool success = await FriendsManager.instance.RemoveFriendAsync(friendId);
                            if (success)
                            {
                                Destroy(row);
                            }
                            else
                            {
                                removeBtn.interactable = true;
                            }
                        });
                    }
                }
            }
        }

        // 2. Nettoyer et lister les requêtes (Requêtes Panel)
        if (requestsContainer != null)
        {
            foreach (Transform child in requestsContainer)
            {
                Destroy(child.gameObject);
            }

            var requests = FriendsManager.instance.GetIncomingRequests();
            foreach (var req in requests)
            {
                if (requestRowPrefab != null)
                {
                    GameObject row = Instantiate(requestRowPrefab, requestsContainer);
                    
                    var nameTxt = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
                    var acceptBtn = row.transform.Find("AcceptButton")?.GetComponent<Button>();

                    if (nameTxt != null) nameTxt.text = req.Member.Profile?.Name ?? req.Member.Id;
                    
                    if (acceptBtn != null)
                    {
                        string requesterId = req.Member.Id;
                        acceptBtn.onClick.AddListener(async () =>
                        {
                            acceptBtn.interactable = false;
                            bool success = await FriendsManager.instance.AcceptFriendRequestAsync(requesterId);
                            if (success)
                            {
                                Destroy(row);
                            }
                            else
                            {
                                acceptBtn.interactable = true;
                            }
                        });
                    }
                }
            }
        }
    }
}
