# ProceduralGenerationTools

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
