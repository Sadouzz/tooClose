using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Connection
{
    public class MatchmakingUI : MonoBehaviour
    {
        public GameObject matchmakingPanel;
        public TextMeshProUGUI statusText;
        public Button playButton;

        public void OnClickPlayMultiplayer()
        {
            if (MultiplayerManager.instance == null)
            {
                Debug.LogError("MultiplayerManager est introuvable !");
                return;
            }

            playButton.interactable = false;
            matchmakingPanel.SetActive(true);
            statusText.text = "Recherche d'un adversaire...";

            string selectedMode = "SurvivalRace"; // Fallback par defaut
            if (GameModeSelectorUI.instance != null)
            {
                selectedMode = GameModeSelectorUI.instance.SelectedMode;
            }

            MultiplayerManager.instance.StartMatchmaking(selectedMode);
        }

        public void UpdateStatus(string status)
        {
            if (statusText != null)
            {
                statusText.text = status;
            }
        }
    }
}
