# SyncRooms : Liminal

Jeu d'exploration liminale / horreur à la première personne inspiré des **Backrooms**, réalisé avec **Unity 6 (6000.6.3f1) + HDRP**,
des modèles **Blender** générés par script et des assets libres (Poly Haven, ambientCG, Freesound).

## Niveaux

| Ordre | Niveau | Ambiance | Sortie |
|---|---|---|---|
| 1 | **Level 4 — "Abandoned Office"** | Open spaces, cubicules, néons (certains morts ou qui clignotent), fenêtres occultées, fontaines d'Almond Water | Escalier B (badge d'accès requis) |
| 2 | **Level 5 — "Terror Hotel"** | Hôtel années 30 : hall en marbre, gramophone, couloirs damassés, salle Beverly (portes partout, même au plafond), chaufferie, couloir-miroir infini | Chambre 237 (clé) → la fenêtre |
| 3 | **Level 94 — "Motion"** | Village des années 30 sur des collines infinies, sirènes musicales, château d'eau, pièces "désincarnées" dans les champs, château flottant | Plonger dans la fontaine |
| 4 | **Level 37 — "Sublimity"** | Complexe immergé de carrelage blanc et d'eau turquoise, caustiques, puits de lumière, aucun danger | Le tunnel sombre (fin) |

## Commandes

| Action | Clavier / souris | Manette |
|---|---|---|
| Se déplacer | ZQSD / WASD / flèches | Stick gauche |
| Regarder | Souris | Stick droit |
| Courir / S'accroupir / Sauter | Maj / Ctrl ou C / Espace | L3 / R3 / A |
| Interagir | E | X |
| Lampe torche | F | Croix ↑ |
| Inventaire | Tab ou I | Y |
| Raccourcis d'objets | 1 – 5 | — |
| Pause | Échap | Start |

**Inventaire** : grille 5×4 (les 5 premières cases = barre rapide), glisser-déposer, double-clic ou clic droit pour utiliser, bouton *Jeter*.
Objets : eau d'amande, lampe torche, piles, badge, clé 237, trousse de secours, rations, notes et cassette à lire, objets de collection.

**Santé mentale** : elle baisse avec le temps (plus vite dans le noir). En dessous de 55 %, vignettage, grain, aberration chromatique, battements de cœur et murmures apparaissent. À 0 %, vous perdez connaissance. Le Level 37 la restaure.

## Rendu (HDRP)

SSR + sondes de réflexion + réflexions planaires (marbre, miroir infini), SSGI, SSAO, ombres de contact, brouillard volumétrique,
ciel physique + nuages volumétriques (Level 94), système d'eau HDRP avec caustiques et rendu sous l'eau (Level 37), parallax occlusion mapping
sur carrelage, pavés, briques, parquet, clear-coat sur marbre et carrelage. Qualité réglable en jeu (Basse / Moyenne / Haute / Ultra).

## Structure

```
Assets/_Project/
  Scripts/       Runtime : Core (GameManager, Controls, Sfx), Player, Inventory, World, UI
  Scripts/Editor Générateurs : ProjectSetup, MaterialLibrary, ModelLibrary, ItemLibrary, LevelKit, Levels/*Builder
  Art/           Textures PBR, modèles Poly Haven + modèles Blender (Custom), icônes
  Resources/     Audio, polices, ItemDatabase
  Scenes/        MainMenu, Level4, Level5, Level94, Level37 (générées)
Tools/           Scripts Python : téléchargement d'assets, génération de textures, modèles Blender
```

## Tout régénérer

Les scènes sont **générées par code** (reproductibles). Dans Unity : menu **SyncRooms** → 1 à 5, ou en ligne de commande :

```
Unity.exe -batchmode -quit -projectPath . -executeMethod SyncRooms.EditorTools.BatchEntry.All
```

Scripts d'assets (Python fourni avec Blender) :

```
blender -b --factory-startup -P Tools/fetch_textures.py      # ambientCG / Poly Haven
blender -b --factory-startup -P Tools/fetch_models.py        # modèles Poly Haven -> FBX
blender -b --factory-startup -P Tools/generate_textures.py   # damas / moquette de l'hôtel
blender -b --factory-startup -P Tools/blender/models_office.py   (idem models_hotel.py, models_town.py)
python Tools/fetch_sounds.py                                  # Freesound (CC0 / CC-BY)
```

Crédits complets : [CREDITS.md](CREDITS.md).
