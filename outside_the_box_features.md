# 🔥 Too Close — Ideas "Outside the Box" pour CARTONNER

> Ce document va AU-DELÀ du plan classique (modes multi + pass saisonnier). Ce sont des features qui créent de la **viralité organique**, des **moments mémorables**, et une **addiction saine**. Chaque idée est pensée pour l'identité unique de Too Close : l'esquive, la tension, le frisson du "TOO CLOSE!".

---

## 1. 🎮 **PUPPET MASTER** — Le mode où tu ES les missiles
> *Aucun jeu mobile ne fait ça.*

**Concept** : 1v1 asymétrique. Un joueur pilote son avion normalement. L'autre joueur **contrôle les missiles en temps réel** — il dessine des trajectoires avec son doigt pour tenter de tuer le pilote.

| Aspect | Détail |
|--------|--------|
| Joueur 1 (Pilote) | Joue normalement — esquive, collecte des étoiles, fait des near-miss |
| Joueur 2 (Puppet Master) | Voit la MÊME map mais d'en haut. Il **tape/swipe pour lancer des missiles**. Chaque missile suit la trajectoire de son doigt |
| Cooldown | Le Puppet Master a un budget de missiles (ex: 1 toutes les 2 secondes) qui augmente avec le temps |
| Win condition | Pilote survit 90s → il gagne. Puppet Master le tue → il gagne |
| Rotation | Les joueurs échangent les rôles au round 2 |

**Pourquoi ça cartonne** :
- C'est un concept **viral par nature** — les gens veulent montrer leurs "designed kills"
- Ça crée une tension PvP DIRECTE sans changer le core gameplay
- Inspiration : "Evolve", "Dead by Daylight", mais adapté au mobile en 2 minutes
- Les replays de ce mode sont du **contenu TikTok prêt à poster**

---

## 2. 👻 **GHOST HAUNTING** — Tes morts hantent les autres joueurs
> *Le méta-game asynchrone qui rend chaque run unique*

**Concept** : Quand tu meurs en multi, ton "fantôme" (ta trajectoire de vol des 5 dernières secondes avant la mort) est sauvegardée. Ce fantôme apparaît dans les parties d'AUTRES joueurs comme un **obstacle supplémentaire** — une traînée spectrale semi-transparente qui traverse l'écran.

| Aspect | Détail |
|--------|--------|
| Enregistrement | Les 5 dernières secondes de mouvement avant chaque mort |
| Apparition | 1-2 fantômes aléatoires par run multi (venant d'autres joueurs random) |
| Danger | Le fantôme est un obstacle traversable MAIS si tu le touches → ralenti de 0.5s (pas de dégât) |
| Near-miss fantôme | Frôler un fantôme = **GHOST MISS** → x2 les points d'un near-miss normal |
| Social | Après la partie, tu vois "Haunted by @PlayerName" → tu peux les ajouter en ami |

**Pourquoi ça cartonne** :
- Ça crée un **lien social indirect** entre joueurs qui ne jouent même pas ensemble
- Le "GHOST MISS" est un flex → les bons joueurs CHERCHENT les fantômes au lieu de les éviter
- Ça rend chaque run imprévisible sans aucun coût serveur (juste des replays stockés)
- C'est **narrativement cool** — "le cimetière des pilotes perdus"

---

## 3. 📱 **AUTO CLIP GENERATOR** — TikTok/Reels intégré
> *La feature qui transforme tes joueurs en ambassadeurs marketing*

**Concept** : Le jeu détecte automatiquement les **moments épiques** et crée un clip de 5-15 secondes avec des effets visuels et de la musique, prêt à partager sur TikTok/Instagram Reels.

### Moments détectés automatiquement :
| Trigger | Type de clip |
|---------|-------------|
| Near-miss x10+ combo | "INSANE DODGE" avec slow-mo au dernier moment |
| Survie < 0.5 unité d'un missile | "BY A PIXEL" avec zoom dramatique |
| Kill d'un Rammer par un missile | "CALCULATED" avec replay angle différent |
| Victoire 1v1 sur le fil | "CLUTCH WIN" avec score qui s'affiche |
| Mort épique (touché par 3 missiles) | "WASTED" style GTA |
| Puppet Master kill créatif | "PUPPET MASTER KILL" avec la trajectoire dessinée |

### Implémentation technique :
- **Unity Recorder** (déjà dans tes packages!) capture les 15 dernières secondes en boucle
- Quand un trigger se déclenche → sauvegarde le buffer
- Post-processing automatique : ajout du logo Too Close, effets de ralenti, texte animé
- Bouton "Share" → directement vers TikTok/Instagram/X via les share intents natifs Android/iOS

**Pourquoi ça cartonne** :
- **Marketing gratuit** — chaque joueur devient un créateur de contenu
- Les clips de near-miss sont **visuellement spectaculaires** et parfaits pour le format court
- Regarde ce que ça a fait pour Subway Surfers, Stumble Guys → les clips viraux = croissance explosive

---

## 4. 🎰 **WAGER MODE** — Mise tes étoiles sur ta skill
> *La tension psychologique de l'enjeu réel*

**Concept** : Avant un match 1v1, les deux joueurs **misent des étoiles**. Le gagnant remporte la mise de l'adversaire.

| Mise | Entrée | Gain victoire |
|------|--------|--------------|
| Bronze | 50 ⭐ | +50 ⭐ (de l'adversaire) |
| Argent | 200 ⭐ | +200 ⭐ |
| Or | 500 ⭐ | +500 ⭐ |
| Diamant | 1000 ⭐ | +1000 ⭐ |

**Gardes-fous** :
- Matchmaking par rang similaire uniquement
- Max 3 wagers par jour (anti-addiction)
- Pop-up de confirmation "Tu es sûr ?"
- Impossible de miser si solde < mise × 3 (tu ne peux pas tout perdre)

**Pourquoi ça cartonne** :
- L'enjeu change TOUT psychologiquement — même gameplay, 10x plus de tension
- Les joueurs en parlent ("j'ai perdu 500 étoiles sur un wager!!")
- Crée du **contenu streamable** naturellement

---

## 5. 🗺️ **MISSILE PATTERN CREATOR** — User-Generated Content
> *Les joueurs créent les niveaux les plus durs*

**Concept** : Un éditeur simple où les joueurs peuvent **dessiner des patterns de missiles** (timing, trajectoire, vitesse) et les publier comme des "challenges".

| Feature | Détail |
|---------|--------|
| Éditeur | Interface touch simple — dessine les trajectoires avec le doigt, place les timings sur une timeline |
| Publication | Chaque pattern a un ID unique → partageable via lien/QR code |
| Validation | Le créateur DOIT survivre à son propre pattern avant de le publier (anti-impossible) |
| Leaderboard par pattern | Chaque challenge a son propre leaderboard (meilleur score, meilleur temps, plus de near-miss) |
| Votes | Les joueurs votent "🔥" ou "💀" après avoir joué un pattern |
| Trending | Section "Trending" avec les patterns les plus joués de la semaine |

**Pourquoi ça cartonne** :
- **Contenu infini** généré par les joueurs = ZÉRO coût de production pour toi
- Les bons créateurs deviennent des "célébrités" du jeu → profil avec nombre de joueurs, taux de survie
- Les patterns "impossibles" deviennent viraux sur les réseaux
- Inspiration : Mario Maker, mais pour les missiles

---

## 6. 🐉 **WORLD BOSS RAIDS** — Événements communautaires globaux
> *Toute la communauté contre UN boss*

**Concept** : Chaque mois, un **boss géant** apparaît (vaisseau-mère, dragon mécanique, tempête de météorites). TOUS les joueurs du monde contribuent à le "vaincre" en accumulant des near-miss et du temps de survie contre ses patterns uniques.

| Phase | Durée | Mécanisme |
|-------|-------|-----------|
| **Phase 1 : Scout** | Jour 1-2 | Le boss est révélé. Les joueurs découvrent ses patterns uniques. Pas de dégâts au boss. |
| **Phase 2 : Assaut** | Jour 3-5 | Chaque near-miss contre un missile du boss = 1 point de dégât au boss. Compteur global affiché sur le menu principal. |
| **Phase 3 : Rage** | Jour 6-7 | Le boss passe en mode rage (patterns plus intenses). Les near-miss comptent x3. |

**Barre de vie globale** affichée sur le menu : "Boss HP: 847,293,000 / 1,000,000,000"

**Récompenses par paliers communautaires** :
| Palier | Récompense pour TOUS |
|--------|---------------------|
| 25% HP | 100 étoiles à tous les participants |
| 50% HP | Skin "Raid Warrior" exclusive |
| 75% HP | Caisse épique à tous |
| 100% (Boss vaincu) | Avion exclusif "Dragonslayer" + titre permanent |

**Récompenses individuelles** basées sur ta contribution (top 1%, top 10%, top 50%).

**Pourquoi ça cartonne** :
- Crée un **sentiment de communauté** incroyable — "on est tous ensemble contre le boss"
- FOMO maximal — l'événement dure 7 jours, les récompenses sont EXCLUSIVES
- Pas besoin de netcode synchrone — c'est asynchrone, chaque joueur joue solo mais contribue au global
- Facilement implémentable : juste un compteur Cloud Save global + des prefabs de boss uniques

---

## 7. ⚔️ **RIVALRY SYSTEM** — Ton ennemi juré
> *Le réseau social par la compétition*

**Concept** : Le jeu détecte automatiquement tes "rivaux" — des joueurs avec un rang et un score similaires. Tu les affrontes régulièrement et tu développes un **historique** avec eux.

| Feature | Détail |
|---------|--------|
| Détection | Après 3+ matchs contre le même joueur, il devient ton "Rival" |
| Profil rival | Tu vois son historique V/D contre toi, son avion préféré, son meilleur near-miss |
| Notifications | "Ton rival @SkyKing vient de battre ton record sur le pattern #4829!" |
| Rivalité classée | Win streak contre un rival = bonus de trophées (+5 par victoire consécutive) |
| Taunts | Emotes rapides en match (🔥, 😎, 💀, 👋) visibles par l'adversaire |

**Pourquoi ça cartonne** :
- La compétition **personnelle** est 10x plus engageante que le matchmaking anonyme
- Les joueurs reviennent SPÉCIFIQUEMENT pour battre leur rival
- Crée des **histoires** naturelles que les joueurs racontent

---

## 8. 🔄 **PRESTIGE SYSTEM** — L'endgame pour les hardcore
> *Pourquoi les joueurs de Call of Duty prestigent 10 fois*

**Concept** : Quand tu max un avion (toutes les upgrades, 1000+ trophées), tu peux le **"Prestige"**. Ça reset ses upgrades et trophées MAIS :

| Prestige | Récompense permanente |
|----------|----------------------|
| ⭐ Prestige 1 | Étoile dorée sur l'avion + bonus passif +5% étoiles gagnées |
| ⭐⭐ Prestige 2 | Traînée de particules spéciale + bonus passif +10% étoiles |
| ⭐⭐⭐ Prestige 3 | Skin "Master" exclusive + bonus passif +15% étoiles |
| 💎 Prestige MAX | Cadre diamant animé + titre "Ace" + bonus +20% étoiles |

**Pourquoi ça cartonne** :
- Les joueurs qui ont "fini" le jeu ont une RAISON de continuer
- Le prestige est un **flex social** visible par tous les adversaires
- Reset stratégique — tu choisis QUAND prestige en fonction de tes autres avions

---

## 9. 🌍 **LIVE WORLD EVENTS** — Le jeu qui vit avec le monde réel
> *Le jeu change en fonction de la réalité*

| Événement réel | Effet in-game |
|----------------|---------------|
| **Nouvel An** | Missiles remplacés par des feux d'artifice (même gameplay, visuel festif). Étoiles deviennent des confettis dorés. |
| **Halloween** | Mode nuit — visibilité réduite, les missiles sont des fantômes. Near-miss = "SPOOKY MISS" |
| **Ramadan/Eid** | Événement de générosité — étoiles x2, caisses de bienvenue, skin spéciale croissant |
| **Été** | Missiles en mode "vague de chaleur" — ils laissent des traînées qui brûlent l'écran (zones de danger persistantes) |
| **Vendredi 13** | Mode chaos — tous les power-ups ont des effets inversés/aléatoires. Le Shield ATTIRE les missiles. |
| **Événement custom** | Tu peux push un événement à tout moment via Remote Config UGS |

**Pourquoi ça cartonne** :
- FOMO permanent — "j'ai raté l'event Halloween avec la skin fantôme?!"
- Les joueurs reviennent spécifiquement pour les événements
- Coût de dev minimal — c'est du reskin + des tweaks de paramètres

---

## 10. 🏅 **LEGACY ACHIEVEMENTS** — Les trophées "impossibles"
> *Le "Dark Souls" des achievements*

Des achievements ultra-rares qui deviennent des légendes dans la communauté :

| Achievement | Condition | % joueurs estimé |
|-------------|-----------|-----------------|
| 🌟 "Untouchable" | Survive 5 minutes sans prendre un seul dégât en Hard | < 0.1% |
| 👻 "Ghost Dancer" | Fais 50 GHOST MISS en une seule partie | < 0.5% |
| 🎯 "Perfect Storm" | Fais un near-miss x30 combo | < 0.01% |
| 💀 "Death Wish" | Gagne un 1v1 avec 1 PV restant | < 2% |
| 🎮 "Puppet Genocide" | En Puppet Master, tue le pilote en moins de 10 secondes | < 1% |
| 🏆 "All Prestige" | Prestige tous les avions au max | < 0.001% |
| 🔥 "World First" | Sois le premier joueur à vaincre un World Boss raid | 1 seul joueur |

**Affichage** : Les legacy achievements apparaissent sur ton profil avec un badge animé. Les joueurs peuvent voir les achievements des autres dans le lobby.

**Pourquoi ça cartonne** :
- Les achievement hunters sont une communauté ENTIÈRE
- Les achievements ultra-rares créent du **contenu YouTube/TikTok** ("J'ai ENFIN eu Untouchable!!")
- Le "World First" crée une course compétitive pendant les raids

---

## 11. 📡 **LIVE SPECTATOR MODE** — Regarde et parie
> *Transforme les joueurs passifs en spectateurs engagés*

**Concept** : Les joueurs peuvent **regarder des matchs en direct** depuis le menu principal. Pendant qu'ils regardent, ils peuvent "parier" des étoiles sur le gagnant.

| Feature | Détail |
|---------|--------|
| Feed live | Liste des matchs en cours (Survival Race, 1v1, Puppet Master) |
| Spectate | Vue en temps réel du match (latence ~2-3s via Relay) |
| "Bet" | Avant le match, mise 10-100 étoiles sur un joueur → gain x1.5 si tu as raison |
| Réactions | Boutons d'emotes en temps réel (🔥 CLUTCH! 💀 RIP! 😱 TOO CLOSE!) visibles par les joueurs |
| Replays | Les meilleurs matchs de la journée sont sauvegardés en "Match of the Day" |

**Pourquoi ça cartonne** :
- Les joueurs qui n'ont pas envie de jouer RESTENT dans le jeu → engagement time massif
- Le betting crée de l'investissement émotionnel sans skill requise
- Les emotes créent un sentiment de **live event** (comme Twitch)

---

## 12. 🧑‍🏫 **MENTOR SYSTEM** — Les vétérans forment les rookies
> *Rétention par le social*

**Concept** : Les joueurs de rang Diamant+ peuvent devenir "Mentors". Ils sont matchés avec des joueurs nouveaux et gagnent des récompenses quand leur protégé progresse.

| Feature | Détail |
|---------|--------|
| Éligibilité Mentor | Rang Diamant+ avec 100+ parties multi |
| Matchmaking | Auto-match avec un joueur qui vient de commencer le multi |
| Récompenses Mentor | +20 étoiles quand le protégé gagne sa 1ère partie, +50 quand il atteint Bronze, etc. |
| Badge | Badge "Mentor" sur le profil avec le nombre de protégés formés |
| Chat | Canal de chat privé Mentor ↔ Protégé |

**Pourquoi ça cartonne** :
- Résout le problème #1 du multi mobile : **l'onboarding des nouveaux joueurs**
- Les mentors se sentent valorisés → rétention des vétérans
- Crée des liens sociaux forts → les protégés invitent le mentor dans leur club

---

## 🎯 Résumé : Les 5 Features les Plus Impactantes

Si tu devais en choisir seulement 5, voici mon ranking par **impact viralité × faisabilité** :

| Rang | Feature | Viralité | Rétention | Difficulté |
|------|---------|----------|-----------|------------|
| 🥇 | **Puppet Master** | 🔥🔥🔥🔥🔥 | 🔥🔥🔥🔥 | ⚠️ Moyenne |
| 🥈 | **Auto Clip Generator** | 🔥🔥🔥🔥🔥 | 🔥🔥 | ⚠️ Moyenne |
| 🥉 | **World Boss Raids** | 🔥🔥🔥🔥 | 🔥🔥🔥🔥🔥 | ✅ Facile |
| 4 | **Ghost Haunting** | 🔥🔥🔥🔥 | 🔥🔥🔥 | ✅ Facile |
| 5 | **Missile Pattern Creator** | 🔥🔥🔥🔥🔥 | 🔥🔥🔥🔥🔥 | ⚠️⚠️ Hard |

---

## Comment ça s'intègre dans le plan existant

Ces features s'ajoutent aux phases du plan principal :

| Phase | Ajout |
|-------|-------|
| Phase 1 (Infra) | Préparer le système de replay/clip en arrière-plan |
| Phase 2 (Survival Race) | Ajouter Ghost Haunting (asynchrone, pas besoin de netcode) |
| Phase 3 (Rétention) | Ajouter Prestige System, Legacy Achievements, Daily Wager |
| Phase 4 (Modes) | **Puppet Master** comme mode phare + Live Spectator basique |
| Phase 5 (Saisons) | World Boss Raids comme événement saisonnier + Live Events |
| Phase 6 (Social) | Rivalry System, Mentor System, Pattern Creator |

> [!IMPORTANT]
> **La feature la plus importante pour la viralité est l'Auto Clip Generator.** Si les gens ne peuvent pas partager facilement leurs moments "TOO CLOSE!", tu rates 80% du potentiel viral. C'est la PREMIÈRE chose que je recommande d'implémenter, même avant le multi.

Dis-moi lesquelles te parlent le plus et on les intègre dans le plan d'implémentation! 🚀
