# Game Design Document: tooClose

## 1. Vue d'ensemble du Jeu
- **Titre :** tooClose
- **Genre :** Arcade / Action (Dodge & Shoot 'em up)
- **Caméra :** 2D Top-Down (Défilement vertical automatique)
- **Pitch :** Vous pilotez un avion et devez esquiver une pluie infinie de missiles et d'ennemis. Le cœur de l'expérience réside dans la mécanique "Near Miss" : plus vous frôlez le danger, plus vous gagnez de points. Survivez le plus longtemps possible, accumulez des étoiles et améliorez votre flotte d'avions.

## 2. Boucle de Gameplay (Core Loop)
1. **Survivre et Esquiver :** Diriger l'avion à travers des vagues de missiles.
2. **Prendre des risques (Near Miss) :** Frôler les obstacles délibérément pour augmenter son Combo et multiplier son score.
3. **Collecter :** Ramasser des étoiles (monnaie du jeu) et activer des power-ups.
4. **Fin de partie :** En cas de crash, le temps de survie et le score se convertissent en un bonus d'étoiles final.
5. **Améliorer (Méta-jeu) :** Utiliser ses étoiles dans le hangar pour acheter de nouveaux avions ou améliorer leurs statistiques.

## 3. Mécaniques Principales

### 3.1. Les Contrôles
Le joueur peut choisir entre deux modes de contrôle (avec Juice/Inclinaison visuelle dans les virages) :
- **Mode Joystick :** Déplacement libre à 360° via un joystick virtuel.
- **Mode Latéral (Touch) :** Toucher la gauche ou la droite de l'écran pour diriger l'avion ("Tap-to-turn").

### 3.2. Le Système de "Near Miss" (L'Esquive de justesse)
L'identité du jeu repose sur la prise de risque constante :
- **Déclenchement :** Le détecteur de l'avion ("NearMissDetector") s'active s'il frôle un missile sans impact.
- **Feedback Joueur :**
  - Un texte pop-up "TOO CLOSE!" surgit à l'écran.
  - Distorsion visuelle brutale de l'écran (Lens Distortion).
  - Ralenti très léger et vibration de l'appareil (Haptic Feedback) pour maximiser le "Game Feel".
- **Récompenses :** Ajout de points immédiat, augmentation du multiplicateur de score global. Un timer limite le combo, forçant le joueur à enchaîner les risques pour le maintenir.

### 3.3. Phase de Combat (Shooting Phase)
- Évènement temporaire (ou débloqué) modifiant le gameplay.
- **Mode "Acecraft" :** L'avion arrête d'avancer passivement pour s'aligner face au danger. Le joueur dispose d'un contrôle total (Strafe X/Y) sur l'écran et l'avion tire automatiquement un déluge de balles/lasers sur les cibles ennemies.

## 4. Les Menaces et Obstacles
- **Missiles Droits :** Se déplacent en ligne droite (Pattern simple).
- **Trackers :** Missiles à tête chercheuse, obligeant le joueur à ruser pour les esquiver.
- **Rammers :** Vaisseaux ennemis programmés pour foncer sur le joueur.
- **Spawn Dynamique :** La difficulté (quantité, fréquence) croît avec le temps de la session, gérée de façon procédurale (ChunkManager & MissileSpawner).

## 5. Progression et Améliorations (RPG Elements)

### 5.1. Stats des Avions
Chaque appareil de la flotte possède un niveau maximum d'amélioration spécifique à 3 caractéristiques :
- **VITESSE (Speed) :** Avancer plus vite, manœuvrer avec plus d'agilité.
- **CONTROLE (Handling) :** Virages plus serrés, temps de réponse accru.
- **BLINDAGE (Armor) :** La vie du joueur (nombre de chocs mortels évitables).

### 5.2. Économie
- Les achats d'améliorations (Niveau 1 à 10 ou 5) coûtent de plus en plus d'étoiles de façon exponentielle.

## 6. Monétisation, Rétention et Pubs
- **Ads Reward (Revive) :** À sa mort, le joueur peut visionner une publicité pour ressusciter. Cela offre un instant d'invincibilité et nettoie l'écran des menaces pour un nouveau départ serein.
- **Upgrade Gratuit Aléatoire :** Dans le menu d'amélioration, il y a un taux de chance (ex: 30%) de pouvoir regarder une pub pour passer au niveau supérieur d'une caractéristique sans dépenser d'étoiles.
- **Doubler les Gains :** Possibilité (souvent par pub) de doubler le butin d'une partie (Double End Game Rewards).
- **Missions & Succès :** (AchievementsManager) Système de récompenses poussant à accomplir des objectifs spécifiques.

## 7. Direction Technique & UX
- **Game Feel :** Les secousses d'écran (CameraShake) intenses lors de la mort ou d'un impact garantissent que la défaite soit marquante.
- **Juice :** Particules d'explosions, sillages derrière les balles, UI adaptative (Onglets qui s'étirent, chiffres qui pulsent).
- **Sauvegarde :** Utilisation intensive des PlayerPrefs pour retenir les combos, étoiles, High Scores et niveaux d'améliorations de chaque modèle d'avion.
