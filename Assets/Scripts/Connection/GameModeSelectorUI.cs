using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace Connection
{
    public class GameModeSelectorUI : MonoBehaviour
    {
        public static GameModeSelectorUI instance;

        [Header("UI Elements")]
        public TextMeshProUGUI modeNameText;

        [Header("Modes List")]
        // Noms exacts utilises en base de donnees (Lobby UGS)
        public List<string> availableModes = new List<string>() 
        { 
            "SurvivalRace", 
            "PuppetMaster" 
        };
        
        private int currentModeIndex = 0;

        // Propriete lue par MatchmakingUI
        public string SelectedMode => availableModes[currentModeIndex];

        private void Awake()
        {
            if (instance == null) instance = this;
        }

        private void Start()
        {
            UpdateModeDisplay();
        }

        // A lier au bouton ">"
        public void NextMode()
        {
            currentModeIndex++;
            if (currentModeIndex >= availableModes.Count) currentModeIndex = 0;
            UpdateModeDisplay();
        }

        // A lier au bouton "<"
        public void PreviousMode()
        {
            currentModeIndex--;
            if (currentModeIndex < 0) currentModeIndex = availableModes.Count - 1;
            UpdateModeDisplay();
        }

        private void UpdateModeDisplay()
        {
            if (modeNameText != null)
            {
                // Affichage formaté pour l'interface utilisateur
                string displayText = SelectedMode;
                if (SelectedMode == "SurvivalRace") displayText = "SURVIVAL RACE";
                else if (SelectedMode == "PuppetMaster") displayText = "PUPPET MASTER";
                
                modeNameText.text = "🎮 " + displayText;
            }
        }
    }
}
