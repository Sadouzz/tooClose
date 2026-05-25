using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AchievementsScript : MonoBehaviour
{
    public Sprite missionsSprite;

    void Update()
    {
        CheckMissions();
    }

    private void CheckMissions()
    {
        // ── MISSIONS TEMPS ───────────────────────────────────────────
        // Mission 1 — Jouer 20 secondes en une partie
        if (Inventory.instance.totalSeconds >= 20)
            CompleteMission("mission1", "MISSION 1 TERMINEE");

        // Mission 5 — Jouer 120 secondes en une partie
        if (Inventory.instance.totalSeconds >= 120)
            CompleteMission("mission5", "MISSION 5 TERMINEE");

        // Mission 8 — Jouer 240 secondes en une partie
        if (Inventory.instance.totalSeconds >= 240)
            CompleteMission("mission8", "MISSION 8 TERMINEE");

        // ── MISSIONS MISSILES ─────────────────────────────────────────
        // Mission 2 — Détruire 150 missiles au total
        if (PlayerPrefs.GetInt("totalDestroyedMissiles", 0) >= 150)
            CompleteMission("mission2", "MISSION 2 TERMINEE");

        // Mission 4 — Détruire 500 missiles au total
        if (PlayerPrefs.GetInt("totalDestroyedMissiles", 0) >= 500)
            CompleteMission("mission4", "MISSION 4 TERMINEE");

        // Mission 6 — Détruire 100 missiles en une partie
        if (MissileSpawner.instance.destroyedMissiles >= 100)
            CompleteMission("mission6", "MISSION 6 TERMINEE");

        // ── MISSIONS POWER-UPS & PUBS ─────────────────────────────────
        // Mission 3 — Utiliser 10 power-ups en une partie
        if (PlayerPowerUpManager.instance.usedPowersCount >= 10)
            CompleteMission("mission3", "MISSION 3 TERMINEE");

        // Mission 7 — Regarder 10 publicités
        if (AdMob.instance != null && AdMob.instance.watchedCount >= 10)
            CompleteMission("mission7", "MISSION 7 TERMINEE");

        // ── MISSIONS ENNEMIS ──────────────────────────────────────────
        // Mission 9 — Détruire 1 ennemi en une partie
        if (MissileSpawner.instance.destroyedEnemies >= 1)
            CompleteMission("mission9", "MISSION 9 TERMINEE");

        // Mission 10 — Détruire 10 ennemis en une partie
        if (MissileSpawner.instance.destroyedEnemies >= 10)
            CompleteMission("mission10", "MISSION 10 TERMINEE");

        // Mission 11 — Detruire 50 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 50)
            CompleteMission("mission11", "MISSION 11 TERMINEE");

        // Mission 12 — Detruire 200 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 200)
            CompleteMission("mission12", "MISSION 12 TERMINEE");

        // Mission 13 — Detruire 500 ennemis au total
        if (PlayerPrefs.GetInt("totalDestroyedEnemies", 0) >= 500)
            CompleteMission("mission13", "MISSION 13 TERMINEE");

        // Mission 14 — Detruire 30 ennemis en une seule partie
        if (MissileSpawner.instance.destroyedEnemies >= 30)
            CompleteMission("mission14", "MISSION 14 TERMINEE");
    }

    // -------------------------------------------------------
    // Helper : complète une mission et déclenche la notif
    // -------------------------------------------------------
    private void CompleteMission(string key, string label)
    {
        if (PlayerPrefs.GetString(key, "no") != "no") return;

        PlayerPrefs.SetString(key, "yes");
        PlayerPrefs.Save();

        if (NotificationScript.instance != null)
            NotificationScript.instance.CallNotif(label, missionsSprite);
    }
}
