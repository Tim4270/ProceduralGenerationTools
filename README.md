# ProceduralGenerationTools

Outils modulaires pour créer des systèmes de **génération procédurale** dans Unity.  
Le projet repose sur une architecture flexible basée sur une **Grid**, des **Cell**, et des **méthodes de génération** interchangeables.  
Tous les algorithmes sont situés dans :

```
Assets/Components/ProceduralGeneration/
```

---

# Architecture générale

L’architecture du projet se compose de trois éléments principaux :

---

##  1. Grid  
La **Grid** représente une carte rectangulaire composée de cellules.  
Elle contient :

- La largeur / hauteur de la grille  
- Une matrice de **Cell**  
- Des fonctions utilitaires :  
  - `GetCell(x, y)`  
  - `GetNeighbors(cell)`  
  - Itération simplifiée  

Tous les algorithmes manipulent directement la Grid.

---

##  2. Cell  
Une **Cell** représente une case du niveau. Elle peut stocker :

- un type (mur, sol, vide…)  
- une valeur numérique (dans le cas du noise)  
- des drapeaux ou métadonnées  

Les algorithmes se basent sur ces données pour créer des structures.

---

##  3. ProceduralGenerationMethod  

Classe abstraite utilisée comme base pour tous les algorithmes :

```csharp
public abstract class ProceduralGenerationMethod : ScriptableObject
{
    public abstract UniTask Generate(Grid grid, CancellationToken token);
}
```

Chaque algorithme est implémenté dans un fichier séparé situé dans :

```
Components/ProceduralGeneration/Methods/
```

---

#  Ajouter un nouvel algorithme

1. Crée un script dans :

```
Components/ProceduralGeneration/Methods/
```

2. Hérite de `ProceduralGenerationMethod` :

```csharp
[CreateAssetMenu(menuName = "Procedural Generation/MyCustomMethod")]
public class MyCustomMethod : ProceduralGenerationMethod
{
    public override async UniTask Generate(Grid grid, CancellationToken token)
    {
        foreach (var cell in grid)
        {
            cell.Type = CellType.Wall;
            await UniTask.Yield(token);
        }
    }
}
```

3. Crée un asset dans Unity  
4. Ajoute-le au pipeline de génération  

---

#  Algorithmes inclus

##  1. Simple Room Placement
![Simple Room Placement](Assets/Gifs/simpleroom.gif)
Place des pièces rectangulaires aléatoirement sans overlap.  
**Utilité :** donjons simples, prototypage.  
**Limites :** pas de couloirs, layout très carré.

---

##  2. BSP Dungeon
![BSP Dungeon](Assets/Gifs/BSP.gif)
Division de la carte en zones via Binary Space Partitioning, placement de pièces et génération de couloirs.  
**Utilité :** donjons structurés, équilibrés.  
**Limites :** parfois trop régulier.

---

##  3. Cellular Automata
![Cellular Automata](Assets/Gifs/Cellular%20Automata.gif)
Automate cellulaire appliqué sur une carte aléatoire pour générer des formes organiques.  
**Utilité :** grottes, cavernes.  
**Limites :** zones parfois isolées.

---

##  4. Simplex Noise Generator
![Simplex Noise Generator](Assets/Gifs/SimplexNoise.gif)
Génère des valeurs continues via Simplex Noise.  
**Utilité :** terrains, biomes, heatmaps.  
**Limites :** ne produit pas de structures.

---

#  Installation

```
git clone https://github.com/Tim4270/ProceduralGenerationTools.git
```

Ouvrir dans Unity **2022.3+**

---

#  Licence
MIT License
