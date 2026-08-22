# 🚀 Too Close — Multijoueur & Rétention Style Brawl Stars

## Contexte

**Too Close** est un endless runner aérien vertical (2D, portrait, mobile) où le joueur esquive des missiles tout en accumulant des "Near Miss" pour scorer. Le jeu a déjà :
- Auth UGS (Google Play Games / Game Center)
- Leaderboards UGS (Easy/Hard)
- Friends UGS
- Profils, Achievements, AdMob, In-App Purchases
- 5 power-ups (Shield, Blaze, SlowMo, Zoom, EMP)
- Système d'upgrades avions (Speed/Handling/Armor)
- 2 modes de difficulté (Easy/Hard)

**Objectif** : Ajouter des modes multijoueurs compétitifs/coopératifs + un système de rétention profond inspiré de Brawl Stars, Crash of Cars et Toybox Turbos, tout en restant adapté à l'identité "esquive aérienne" de Too Close.

---

## Stack Technique

> [!IMPORTANT]
> Tu es déjà full UGS. On reste dans cet écosystème pour éviter de mixer les backends.

| Couche | Technologie | Rôle |
|--------|-------------|------|
| **Netcode** | Netcode for GameObjects (NGO) | Sync positions, missiles, scores en temps réel |
| **Transport** | Unity Relay | Connexion P2P sans exposer les IPs, bypass NAT mobile |
| **Matchmaking** | Unity Lobby | Création/recherche de parties, codes d'invitation |
| **Backend** | UGS (déjà en place) | Auth, Leaderboards, Friends, Analytics |
| **Données persistantes** | UGS Cloud Save | Progression, saison, inventaire, stats |

### Packages à ajouter au `manifest.json` :
```
com.unity.netcode.gameobjects    → NGO
com.unity.services.multiplayer   → Relay + Lobby unifié
com.unity.services.cloudsave     → Sauvegarde cloud (remplace PlayerPrefs à terme)
com.unity.multiplayer.tools      → Debug/profiling réseau (optionnel)
```

---

## 🎮 PARTIE 1 : Modes Multijoueurs

### Philosophie de design
> Le core gameplay de Too Close est l'esquive. Les modes multi DOIVENT tourner autour de ça — pas de deathmatch classique. L'avion ne tire pas sur les autres joueurs (sauf mode spécial). La compétition vient de **qui survit le mieux**, **qui prend le plus de risques**, et **qui gère la pression des autres**.

---

### Mode 1 : 🏆 **SURVIVAL RACE** (Le mode principal — 2 à 4 joueurs)
> *Inspiré de Toybox Turbos + Brawl Stars "Showdown"*

**Concept** : Tous les joueurs partagent le même espace. Dernière personne en vie gagne. La densité de missiles augmente plus vite qu'en solo.

| Aspect | Détail |
|--------|--------|
| Joueurs | 2-4 (matchmaking ou invite) |
| Caméra | Chaque joueur voit sa propre vue (pas de caméra partagée sur mobile) |
| Missiles | Le host spawne les missiles, synchronisés via NGO |
| Win condition | Dernier survivant |
| Durée | 60-120 secondes (la difficulté monte TRÈS vite) |
| Spécialité | Les power-ups sont **partagés** sur la map — si un joueur le prend, les autres ne l'ont pas |

**Twist unique — "Danger Zone"** :
- Chaque joueur qui meurt libère une **onde de choc** qui ajoute des missiles supplémentaires dans la zone des survivants
- Plus il y a de morts, plus c'est chaotique pour les derniers → moments clutch garantis

**Scoring** :
- 1er : +30 trophées
- 2ème : +15 trophées
- 3ème : +5 trophées
- 4ème : -5 trophées

---

### Mode 2 : ⚡ **NEAR MISS DUEL** (1v1 — Le mode skill)
> *Inspiré de Crash of Cars "King" + le Near Miss system*

**Concept** : 2 joueurs, même pluie de missiles, même durée. Celui qui a le plus haut score de near-miss gagne.

| Aspect | Détail |
|--------|--------|
| Joueurs | 1v1 |
| Durée | 90 secondes fixes |
| Missiles | Identiques pour les deux joueurs (seed partagée) |
| Win condition | Meilleur score near-miss à la fin du timer (ou dernier vivant si l'autre crash) |
| Affichage | Split-screen virtuel : ton avion à gauche, le fantôme de l'adversaire à droite (juste le score qui monte en temps réel) |

**Twist — "Combo Steal"** :
- Si ton adversaire maintient un combo de Near Miss x5+, un **missile bonus** apparaît dans TON espace → ça punit les passifs
- Mécanisme de comeback : si tu es en retard de score, les missiles dans ton espace sont légèrement plus lents (avantage du perdant, invisible)

**Scoring** :
- Victoire : +25 trophées
- Défaite : -10 trophées

---

### Mode 3 : 🌀 **CHAOS ARENA** (4 joueurs — Le mode fun)
> *Inspiré de Crash of Cars "Arena" + Brawl Stars "Heist"*

**Concept** : 4 joueurs dans une arène fermée (pas de défilement vertical). Les missiles rebondissent sur les murs. Des étoiles spawn au centre — celui qui en collecte le plus en 2 minutes gagne.

| Aspect | Détail |
|--------|--------|
| Joueurs | 4 (FFA) |
| Arène | Espace fermé, les bords sont des murs rebondissants |
| Missiles | Spawns au centre + rebondissent sur les murs → chaos total |
| Étoiles | Apparaissent par vagues au centre de l'arène (zone dangereuse) |
| Win condition | Plus d'étoiles collectées en 2 minutes |
| Mort | Tu respawn après 3s mais tu perds 30% de tes étoiles (elles retombent dans l'arène) |

**Twist — "Power-Up Wars"** :
- Les power-ups apparaissent mais ont des effets PvP modifiés :
  - **EMP** → repousse les AUTRES joueurs (pas que les missiles)
  - **Blaze** → vole les étoiles des joueurs qu'il touche
  - **Shield** → protège aussi tes étoiles (pas de perte à la mort)

---

### Mode 4 : 🤝 **CO-OP SURVIVAL** (2 joueurs — Le mode coopératif)
> *Inspiré de Brawl Stars "Duo Showdown"*

**Concept** : 2 joueurs ensemble contre une pluie de missiles intensifiée. Vous partagez les power-ups, l'objectif est de tenir le plus longtemps possible ensemble.

| Aspect | Détail |
|--------|--------|
| Joueurs | 2 (invite friend ou matchmaking) |
| Missiles | Densité x1.5 par rapport au solo |
| Win condition | Temps de survie → nouveau leaderboard "Co-op" |
| Respawn | Si un joueur meurt, l'autre peut le "ranimer" en passant sur un orbe qu'il lâche (dans les 5 secondes) |
| Fin | Quand les deux joueurs sont morts |

**Twist — "Combo Link"** :
- Les combos de Near Miss sont **partagés** → les deux joueurs alimentent le même multiplicateur
- Mécanisme de synergie : si les deux joueurs font un near-miss en moins de 0.5s d'écart → bonus x2 du combo → pousse les équipes à prendre des risques synchronisés

---

### Mode 5 : 💀 **HOT ZONE** (Événement rotatif — 4 joueurs)
> *Inspiré de Brawl Stars "Hot Zone"*

**Concept** : Une zone sûre (cercle) rétrécit progressivement. Rester dans la zone = gagner des points. Rester hors de la zone = perdre des PV lentement. Des missiles pleuvent partout.

| Aspect | Détail |
|--------|--------|
| Joueurs | 4 (FFA) |
| Zone | Cercle qui rétrécit toutes les 15 secondes |
| Points | +1 point par seconde dans la zone |
| Hors zone | -1 PV par seconde |
| Missiles | Spawns normaux + missiles bonus DANS la zone (la zone sûre est aussi la plus dangereuse) |
| Win condition | Plus de points à la fin (3 minutes) OU dernier vivant |

---

## 🔄 PARTIE 2 : Système de Rétention (Style Brawl Stars)

### 2.1 📊 Système de Trophées & Rang

Chaque avion a ses propres trophées (comme les brawlers dans Brawl Stars) :

```
Trophées par avion → Rang de l'avion
Somme des trophées → Rang global du joueur
```

| Rang | Trophées requis | Récompense |
|------|----------------|------------|
| 🥉 Bronze | 0-149 | Icône Bronze |
| 🥈 Argent | 150-299 | +50 étoiles |
| 🥇 Or | 300-499 | +100 étoiles + titre "Pilote d'Or" |
| 💎 Diamant | 500-749 | Skin exclusive |
| 👑 Légende | 750-999 | Traînée de particules spéciale |
| ⚡ Maître | 1000+ | Cadre de profil animé |

**Gain/Perte de trophées** : Uniquement en multijoueur (le solo ne change pas les trophées).

---

### 2.2 🎫 Brawl Pass → **"Flight Pass"** (Passe Saisonnier)

Saison de 8 semaines avec 2 tracks :

| Track | Contenu |
|-------|---------|
| **Gratuit** | Étoiles, boosts XP, 1 avion gratuit au niveau 30, caisses standards |
| **Premium** (achat IAP) | Étoiles x3, skins exclusives, avion premium au niveau 1, caisse légendaire au niveau 60 |

**XP du pass** gagné via :
- Jouer des parties multi (+XP)
- Compléter les quêtes journalières (+XP)
- Near-miss en multi (+XP bonus)
- Victoires en classé (+XP bonus)

---

### 2.3 📋 Quêtes & Missions

#### Quêtes Journalières (3 par jour, reset à minuit)
| Exemple | Récompense |
|---------|------------|
| "Fais 50 near-miss en multijoueur" | 30 étoiles + 20 XP Pass |
| "Termine top 2 dans 3 parties de Survival Race" | 50 étoiles + 30 XP Pass |
| "Joue une partie avec un ami" | 20 étoiles + 15 XP Pass |
| "Utilise 5 power-ups EMP en multi" | 25 étoiles + 20 XP Pass |

#### Quêtes Saisonnières (plus longues, récompenses majeures)
| Exemple | Récompense |
|---------|------------|
| "Gagne 100 parties en Survival Race cette saison" | Skin saisonnière |
| "Atteins 500 trophées avec 3 avions différents" | Avion saisonnier |
| "Accumule 10 000 near-miss en une saison" | Traînée de particules |

---

### 2.4 🎁 Système de Récompenses Quotidiennes

**Connexion quotidienne** (streak) :
| Jour | Récompense |
|------|------------|
| 1 | 20 étoiles |
| 2 | 50 étoiles |
| 3 | Caisse standard |
| 4 | 100 étoiles |
| 5 | Caisse rare |
| 6 | 150 étoiles |
| 7 | **Caisse épique** + reset du cycle |

**Récompenses d'inactivité** : Si le joueur ne s'est pas connecté depuis 3+ jours → notification push "Ton escadron a besoin de toi! Cadeau de retour de 200⭐ t'attend"

---

### 2.5 🏛️ Clubs / Escadrons (Guildes)

| Feature | Détail |
|---------|--------|
| Taille max | 30 membres |
| Chat | Chat textuel simple in-game |
| Classement club | Basé sur les trophées cumulés des membres |
| **Événement Club : "Mega Raid"** | Chaque semaine, le club affronte un boss commun (pluie de missiles ultra-dense). Chaque membre contribue avec son meilleur temps de survie. Le cumul donne un score club → récompenses par paliers |
| Rôles | Président, Vice-président, Membre |

---

### 2.6 📦 Système de Caisses (Loot)

Pas de lootbox P2W. Les caisses contiennent :
- **Étoiles** (monnaie commune)
- **Fragments d'avion** (collecter X fragments = débloquer l'avion)
- **Skins cosmétiques** (traînées, couleurs, effets d'explosion)
- **Boosters temporaires** (XP x2 pendant 1h, étoiles x2 pendant 1h)

| Type | Contenu garanti |
|------|----------------|
| Standard | 20-50 étoiles + 1 cosmétique commun |
| Rare | 50-150 étoiles + 1-3 fragments d'avion |
| Épique | 100-300 étoiles + 5-10 fragments + 1 skin rare |
| Légendaire | 300-500 étoiles + avion complet OU skin épique |

---

### 2.7 🏆 Saisons Classées

Saison de 4 semaines avec un classement séparé :

| Ligue | Trophées classés |
|-------|-----------------|
| Bronze | 0-199 |
| Argent | 200-399 |
| Or | 400-599 |
| Diamant | 600-799 |
| Champion | 800+ |

**Récompenses de fin de saison** basées sur la ligue atteinte (étoiles, skins, caisses).
**Soft reset** des trophées classés à chaque nouvelle saison (tu redescends de 50%).

---

## 📐 Architecture Technique

### Nouveaux Scripts à Créer

```
Assets/Scripts/
├── Multiplayer/
│   ├── NetworkGameManager.cs      → Gère le cycle de vie d'une partie multi (NGO)
│   ├── NetworkPlayerController.cs → Version réseau de PlayerMovement
│   ├── NetworkMissileSpawner.cs   → Spawn autoritatif des missiles (host only)
│   ├── LobbyManager.cs           → Création/recherche de lobby (UGS Lobby)
│   ├── RelayManager.cs           → Allocation Relay + connexion (UGS Relay)
│   ├── MatchmakingUI.cs          → UI de recherche/création de partie
│   └── MultiplayerHUD.cs         → Affichage des scores adverses en temps réel
│
├── GameModes/
│   ├── GameModeBase.cs            → Classe abstraite pour les modes
│   ├── SurvivalRaceMode.cs        → Mode Survival Race
│   ├── NearMissDuelMode.cs        → Mode 1v1 Near Miss
│   ├── ChaosArenaMode.cs          → Mode arène FFA
│   ├── CoopSurvivalMode.cs        → Mode Co-op
│   └── HotZoneMode.cs             → Mode événement rotatif
│
├── Retention/
│   ├── SeasonManager.cs           → Gestion des saisons (Flight Pass)
│   ├── FlightPassUI.cs            → UI du pass saisonnier
│   ├── QuestManager.cs            → Quêtes journalières & saisonnières
│   ├── QuestUI.cs                 → UI des quêtes
│   ├── DailyRewardsManager.cs     → Récompenses connexion quotidienne
│   ├── DailyRewardsUI.cs          → UI des récompenses journalières
│   ├── TrophyManager.cs           → Système de trophées par avion
│   ├── RankManager.cs             → Rangs et saisons classées
│   ├── LootBoxManager.cs          → Système de caisses
│   └── LootBoxUI.cs               → UI d'ouverture de caisses (animation)
│
├── Social/
│   ├── ClubManager.cs             → Gestion des clubs/escadrons
│   ├── ClubUI.cs                  → UI des clubs
│   ├── ClubRaidManager.cs         → Événement Mega Raid hebdomadaire
│   └── ChatManager.cs             → Chat in-game simple
│
└── Data/
    ├── CloudSaveManager.cs        → Sync PlayerPrefs → UGS Cloud Save
    ├── SeasonData.cs              → ScriptableObject pour le contenu saisonnier
    ├── QuestData.cs               → ScriptableObject pour les quêtes
    └── LootTableData.cs           → ScriptableObject pour les tables de loot
```

### Modifications aux Scripts Existants

#### [MODIFY] [PlayerMovement.cs](file:///d:/Unity/Projects/tooClose/Assets/Scripts/PlayerMovement.cs)
- Hériter de `NetworkBehaviour` au lieu de `MonoBehaviour`
- Les inputs sont locaux (client), le mouvement est synchronisé via `NetworkVariable<Vector3>` pour la position
- Ajouter un flag `isMultiplayer` pour basculer entre logique solo et multi

#### [MODIFY] [MissileSpawner.cs](file:///d:/Unity/Projects/tooClose/Assets/Scripts/MissileSpawner.cs)
- En multi, seul le **host** spawn les missiles
- Les missiles sont des `NetworkObjects` instanciés via `NetworkManager.SpawnManager`
- Ajouter des profils de spawn par mode de jeu

#### [MODIFY] [Inventory.cs](file:///d:/Unity/Projects/tooClose/Assets/Scripts/Inventory.cs)
- Ajouter les trophées par avion
- Sync avec Cloud Save en multi
- Ajouter le tracking des quêtes (compteurs de near-miss, kills, etc.)

#### [MODIFY] [NearMissManager.cs](file:///d:/Unity/Projects/tooClose/Assets/Scripts/NearMissManager.cs)
- En multi, envoyer un RPC au host pour valider le near-miss (anti-triche)
- Broadcaster le score near-miss aux autres joueurs pour l'affichage

#### [MODIFY] [UIManager.cs](file:///d:/Unity/Projects/tooClose/Assets/Scripts/UIManager.cs)
- Ajouter le bouton "Multiplayer" dans le menu principal
- Navigation vers l'écran de sélection de mode
- Intégrer le Flight Pass et les quêtes dans l'UI existante

#### [MODIFY] [manifest.json](file:///d:/Unity/Projects/tooClose/Packages/manifest.json)
- Ajouter les packages NGO, Multiplayer Services, Cloud Save

---

## User Review Required

> [!IMPORTANT]
> **Choix du modèle réseau** : Je recommande un modèle **Host-autoritatif via Relay** (pas de serveurs dédiés). Le host spawne les missiles, valide les scores. C'est gratuit sur le free tier UGS et parfait pour du 2-4 joueurs mobile. Mais ça signifie qu'un joueur peut potentiellement tricher s'il est host. Pour un jeu mobile casual, c'est acceptable. OK pour toi?

> [!IMPORTANT]
> **Priorité d'implémentation** : C'est un projet MASSIF. Je recommande de le faire en phases. Quel ordre tu préfères?
> - **Option A** : Multiplayer d'abord (le fun) → puis rétention (le business)
> - **Option B** : Rétention d'abord (garder les joueurs actuels) → puis multiplayer
> - **Option C** : Un mode multi simple (Survival Race) + les bases de rétention (quêtes + daily rewards) en parallèle

> [!WARNING]
> **Clubs/Escadrons et Mega Raid** nécessitent un backend custom (UGS n'a pas de système de guildes built-in). On devrait utiliser **UGS Cloud Save** + **Cloud Code** (serverless functions) pour ça, ou un serveur léger type Firebase/Supabase. Ça ajoute de la complexité. On peut le repousser en Phase 3.

## Open Questions

1. **Combien d'avions as-tu actuellement dans le jeu?** (Pour calibrer les fragments et le système de caisses)
2. **As-tu déjà des skins/variantes visuelles pour les avions?** (Ou c'est à créer from scratch?)
3. **Le jeu est-il déjà publié sur les stores?** (Impact sur le versioning des saves et la migration)
4. **Budget serveur** : Es-tu prêt à payer pour UGS au-delà du free tier si le jeu scale? (Le free tier couvre ~50 CCUs pour Relay)
5. **Mode ranked** : Tu veux un vrai anti-triche sérieux (serveurs dédiés) ou le host-autoritatif est OK pour commencer?

---

## Plan d'Implémentation par Phases

### Phase 1 — Infrastructure Multi (2-3 semaines)
- Ajouter les packages NGO + Relay + Lobby
- Créer `RelayManager`, `LobbyManager`, `NetworkGameManager`
- Convertir `PlayerMovement` en `NetworkBehaviour`
- Créer `NetworkMissileSpawner` (host-only spawning)
- UI basique de matchmaking (créer/rejoindre une partie)
- **Livrable** : 2 joueurs peuvent jouer ensemble en temps réel

### Phase 2 — Premier Mode: Survival Race (1-2 semaines)
- Implémenter `SurvivalRaceMode` (2-4 joueurs FFA)
- Scoring réseau + affichage des adversaires
- Système de "Danger Zone" (mort = onde de choc)
- Écran de résultats multi
- **Livrable** : Mode Survival Race jouable

### Phase 3 — Rétention Basique (2 semaines)
- `TrophyManager` (trophées par avion)
- `QuestManager` + `QuestUI` (3 quêtes journalières)
- `DailyRewardsManager` (streak de 7 jours)
- Migration PlayerPrefs → Cloud Save pour les données sensibles
- **Livrable** : Boucle de rétention quotidienne fonctionnelle

### Phase 4 — Modes Supplémentaires (2-3 semaines)
- Near Miss Duel (1v1)
- Chaos Arena (4 joueurs FFA avec arène fermée)
- Co-op Survival (2 joueurs)
- **Livrable** : 4 modes multijoueurs

### Phase 5 — Flight Pass & Saisons (2 semaines)
- `SeasonManager` + `FlightPassUI`
- Track gratuit + premium
- Système de caisses et fragments d'avion
- Skins cosmétiques
- **Livrable** : Premier Flight Pass jouable

### Phase 6 — Social Avancé (2 semaines)
- Clubs/Escadrons
- Chat simple
- Mega Raid hebdomadaire
- Ranked seasons
- **Livrable** : Système social complet

---

## Verification Plan

### Automated Tests
- Tests unitaires pour le `TrophyManager`, `QuestManager`, `DailyRewardsManager` (logique pure)
- Tests d'intégration avec UGS Cloud Save en mode sandbox

### Manual Verification
- Test multi-instance dans l'éditeur Unity via **Multiplayer Play Mode**
- Test sur 2 appareils Android via WiFi pour valider la latence Relay
- Test de déconnexion/reconnexion mid-game
- Stress test avec 4 joueurs simultanés
- Validation de la sync des trophées/quêtes entre sessions
