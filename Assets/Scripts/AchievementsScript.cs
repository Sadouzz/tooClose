using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Settings;

public class AchievementsScript : MonoBehaviour
{
    public Sprite missionsSprite;

    [Header("Localization")]
    public string stringTableName = "UITexts";

    private string GetTranslation(string key, string fallback)
    {
        string tr = LocalizationSettings.StringDatabase.GetLocalizedString(stringTableName, key);
        if (string.IsNullOrEmpty(tr) || tr.Contains("No translation")) return fallback;
        return tr;
    }

    void Update()
    {
        CheckMissions();
    }

    private void CheckMissions()
    {
        // ── MISSIONS TEMPS ───────────────────────────────────────────
        // Mission 1 — Jouer 20 secondes en une partie
        if (Inventory.instance.totalSeconds >= 20)
            CompleteMission("mission1", "1");

        // Mission 5 — Jouer 120 secondes en une partie
        if (Inventory.instance.totalSeconds >= 120)
            CompleteMission("mission5", "5");

        // Mission 8 — Jouer 240 secondes en une partie
        if (Inventory.instance.totalSeconds >= 240)
            CompleteMission("mission8", "8");

        // ── MISSIONS MISSILES ─────────────────────────────────────────
        // Mission 2 — Détruire 150 missiles au total
        if (PlayerPrefs.GetInt("totalDestroyedMissiles", 0) >= 150)
            CompleteMission("mission2", "2");

        // Mission 4 — Détruire 500 missiles au total
        if (PlayerPrefs.GetInt("totalDestroyedMissiles", 0) >= 500)
            CompleteMission("mission4", "4");

        // Mission 6 — Détruire 100 missiles en une partie
        if (MissileSpawner.instance.destroyedMissiles >= 100)
            CompleteMission("mission6", "6");

        // ── MISSIONS POWER-UPS & PUBS ─────────────────────────────────
        // Mission 3 — Utiliser 10 power-ups en une partie
        if (PlayerPowerUpManager.instance.usedPowersCount >= 10)
            CompleteMission("mission3", "3");

        // Mission 7 — Regarder 10 publicités
        if (AdMob.instance != null && AdMob.instance.watchedCount >= 10)
            CompleteMission("mission7", "7");

        // ── MISSIONS ENNEMIS ──────────────────────────────────────────
        // Mission 9 — Détruire 1 ennemi en une partie
        if (MissileSpawner.instance.destroyedEnemies >= 1)
            CompleteMission("mission9", "9");

        // Mission 10 — Détruire 10 ennemis en une partie
        if (MissileSpawner.instance.destroyedEnemies >= 10)
            CompleteMission("mission10", "10");

        // Mission 11 — Detruire 50 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 50)
            CompleteMission("mission11", "11");

        // Mission 12 — Detruire 200 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 200)
            CompleteMission("mission12", "12");

        // Mission 13 — Detruire 500 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 500)
            CompleteMission("mission13", "13");

        // Mission 14 — Detruire 30 ennemis en une seule partie
        if (MissileSpawner.instance.destroyedEnemies >= 30)
            CompleteMission("mission14", "14");

        // Mission 15 — Regarder 30 pubs
        if (AdMob.instance != null && AdMob.instance.watchedCount >= 30)
            CompleteMission("mission15", "15");
    }

    // -------------------------------------------------------
    // Helper : complète une mission et déclenche la notif
    // -------------------------------------------------------
    private void CompleteMission(string key, string missionNumber)
    {
        if (PlayerPrefs.GetString(key, "no") != "no") return;

        PlayerPrefs.SetString(key, "yes");
        PlayerPrefs.Save();

        // On fabrique la phrase automatiquement : MISSION + numéro + COMPLETE
        string translatedLabel = GetTranslation("MISSION", "MISSION") + " " + missionNumber + " " + GetTranslation("COMPLETE", "COMPLETE");

        if (NotificationScript.instance != null)
            NotificationScript.instance.CallNotif(translatedLabel, missionsSprite);
    }
}
