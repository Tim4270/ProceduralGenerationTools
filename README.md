# ProceduralGenerationTools
# ProceduralGenerationTools

Résumé
----
Outils et méthodes pour génération procédurale sur grille dans Unity. Le projet fournit :
- une implémentation de grille (`Grid` / `Cell`),
- une base réutilisable pour les méthodes de génération (`ProceduralGenerationMethod`),
- plusieurs generators exemples : `SimplexNoiseGenerator`, `SimpleRoomPlacement`,
- utilitaires : `RandomService`, `ScriptableObjectDatabase`.

Structure importante
----
- `Assets/Components/ProceduralGeneration/...` : dossiers contenant les méthodes de génération et exemples.
- `ProceduralGridGenerator` : composant MonoBehaviour qui orchestre la création de la `Grid`, injecte les dépendances (RandomService, Grid) et exécute la méthode de génération (ScriptableObject).

Concepts clés
----
- Grid  
  - Représente la grille logique. Propriétés importantes : `Width`, `Lenght`, `CellSize`, `OriginPosition`, `Cells`.  
  - Méthodes utiles :  
    - `TryGetCellByCoordinates(int x, int y, out Cell)` — obtenir une cellule par coordonnées.  
    - `TryGetCellByPosition(Vector3 pos, out Cell)` — obtenir cellule depuis une position monde.  
    - `GetWorldPosition(...)` et `GetCellsInCircle(...)`.  
  - `DrawGridDebug()` affiche la grille si activée.

- Cell  
  - Représente une case de la grille. Propriétés : `Coordinates` (Vector2Int), `ContainObject` (bool), `GridObject` (données), `View` (controller).  
  - Méthodes : `AddObject(GridObjectController)`, `ClearGridObject()`, `GetCenterPosition(Vector3 origin)`.

- ProceduralGenerationMethod (base)  
  - ScriptableObject servant de contrat pour un generator. Champs injectés à l'exécution :
    - `ProceduralGridGenerator GridGenerator` (injection),
    - `RandomService RandomService`,
    - `Grid Grid`.  
  - Constantes de noms de tuiles disponibles :
    - `ROOM_TILE_NAME`, `CORRIDOR_TILE_NAME`, `GRASS_TILE_NAME`, `WATER_TILE_NAME`, `ROCK_TILE_NAME`, `SAND_TILE_NAME`.
  - Helpers fournis :
    - `CanPlaceRoom(RectInt room, int spacing)` — vérifie la pose d'une salle,
    - `AddTileToCell(Cell cell, string tileName, bool overrideExistingObjects)` — ajoute/replace une tuile,
    - `SetCellView(Cell cell, string tileName, bool overrideExistingObjects)` — met à jour la vue.
  - Contrat : implémenter `protected override UniTask ApplyGeneration(CancellationToken cancellationToken)`.
  - Paramètre contrôlant la durée : `_maxSteps` et la génération peut être annulée via le `CancellationToken`.

- ProceduralGridGenerator  
  - Composant qui contient un champ `_generationMethod` (ScriptableObject).
  - Injecte les dépendances et appelle `Generate()` sur la méthode.  
  - Paramètre d'éditeur important : `_stepDelay` (exposé à runtime via `StepDelay`) pour visualiser pas-à-pas.

- RandomService  
  - Fournit génération aléatoire contrôlée par `Seed`. Méthodes : `Range`, `Chance`, `Pick`.

Générateurs présents (explication)
----
- `SimplexNoiseGenerator`  
  - Utilise `FastNoiseLite` pour produire du bruit 2D.  
  - Paramètres exposés : `noiseType`, `frequency`, `amplitude`, fractal (`octaves`, `lacunarity`, `persistence`), `offset`.  
  - Mappe l'échantillon bruit (après amplitude) vers des seuils de hauteur (`waterHeight`, `sandHeight`, `grassHeight`, `rockHeight`) pour choisir la tuile à placer avec `AddTileToCell`.  
  - Option `visualizeDuringGeneration` pour observer génération en temps réel.  
  - Astuce : les hauteurs doivent respecter `water <= sand <= grass <= rock`. Si l'ordre est incorrect, le rendu de `Sand` peut disparaître (voir `OnValidate()` dans le fichier).

- `SimpleRoomPlacement`  
  - Place jusqu'à `_maxRooms` salles rectangulaires aléatoires avec `CanPlaceRoom` pour vérifier collisions/espacement.  
  - Pour chaque salle placée, calcule le centre (`GetRoomCenter`) et la stocke.  
  - Connecte chaque salle à la plus proche via `PlaceCorridor`.  
  - Remplissage final du sol via `BuildGround()` qui obtient la template `Grass` depuis `ScriptableObjectDatabase` et instancie des `GridObject` sur chaque cellule.

Comment ajouter une nouvelle architecture / nouveau generator
----
1. Crée une nouvelle classe héritant de `ProceduralGenerationMethod` (ScriptableObject). Ajoute `CreateAssetMenu` pour la créer facilement depuis l'éditeur.  
2. Implémente `protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)`.  
   - Utilise `Grid`, `GridGenerator`, `RandomService`.  
   - Respecte `cancellationToken.ThrowIfCancellationRequested()` fréquemment.  
   - Pour modifier la grille, utilise les helpers fournis (`AddTileToCell`, `CanPlaceRoom`, `SetCellView`).  
   - Pour visualiser étape par étape, `await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken)`.  
3. Crée l'asset (clic droit → Create → ton menu).  
4. Assigne l'asset au champ `_generationMethod` du `ProceduralGridGenerator` dans la scène.  
5. Dans l'inspecteur de `ProceduralGridGenerator`, règle `StepDelay`, `Seed`, et exécute `Generate()` depuis l'UI/éditeur ou en Play.

Exemple minimal de template


Conseils de debug & pièges fréquents
----
- Les noms de tuiles sont sensibles (exact match). Vérifie `ScriptableObjectDatabase` pour les templates existants (`"Grass"`, `"Sand"`, etc.).  
- Respecte l'ordre des hauteurs (pour `SimplexNoiseGenerator`) : `water <= sand <= grass <= rock`.  
- Toujours vérifier `Grid.TryGetCellByCoordinates(...)` avant d'accéder à une cellule.  
- Utilise `cancellationToken` pour permettre l'annulation propre.  
- Si tu ne vois rien à l'écran : vérifier `ProceduralGridGenerator` (est‑il assigné à la scène ? _generationMethod_ est-il set ? `StepDelay` raisonnable ?).

Utilisation rapide (Unity)
----
1. Ouvrir la scène contenant `ProceduralGridGenerator` ou ajouter le composant.  
2. Créer un asset de la méthode (clic droit → Create → Procedural Generation Method → …).  
3. Assigner l'asset au champ `_generationMethod` du `ProceduralGridGenerator`.  
4. Régler `Seed` et `StepDelay`.  
5. Appuyer sur Generate (ou lancer en Play si la UI l'exécute).

Contribution
----
- Fork → nouvelle feature / bugfix → Pull Request.  
- Ouvrir une issue pour discuter d'un changement d'API (ex : nouveaux helpers dans `ProceduralGenerationMethod`).

Contact rapide
----
- Pour un bug reproductible, préciser : scène utilisée, valeurs du generator, capture d'écran de la grille, logs console.
