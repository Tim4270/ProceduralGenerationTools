# ProceduralGenerationTools

Outils modulaires pour créer des systèmes de **génération procédurale** dans Unity.  
Le projet repose sur une architecture flexible basée sur une **Grid**, des **Cell**, et des **méthodes de génération** interchangeables. 
Ces outils ont été réalises durant mon cursus au Gaming Campus à Lyon. 
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

Chaque algorithme est implémenté dans un dossier correspondant à leur génération procédurale :

```
Components/ProceduralGeneration/NomdelaGénération/Script.cs
```

---

#  Ajouter un nouvel algorithme

EXEMPLE : 

1. Creer un script dans :

```
Components/ProceduralGeneration/Methods/
```

2. Faite le hériter de `ProceduralGenerationMethod` :

```csharp
[CreateAssetMenu(menuName = "Procedural Generation/MyCustomGenerationMethod")]
public class MyCustomGenerationMethod : ProceduralGenerationMethod
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

3. Creer un asset dans Unity :
Create → Procedural Generation → MyCustomGenerationMethod

4. Dans la scène, ajouter/choisir un ProceduralGridGenerator et sélectionner votre méthode.

---

#  Algorithmes inclus

##  1. Simple Room Placement
![Simple Room Placement](Assets/Gifs/simpleroom.gif)

Place des pièces rectangulaires aléatoirement sans chevauchement.  
**Utilité :** donjons simples, prototypage.  
**Limites :** pas de couloirs, layout très carré.

---

##  2. BSP Dungeon
![BSP Dungeon](Assets/Gifs/BSP.gif)

La grille est split de facon récursive sur les enfants crées par ces meme splits puis les connecte avec une liste de room placés  
**Utilité :** donjons structurés, équilibrés.  
**Limites :** parfois trop régulier.

---

##  3. Cellular Automata
![Cellular Automata](Assets/Gifs/Cellular%20Automata.gif)

Basé sur le jeu de la vie, on place des tiles avec une noise density défini dans l'inspector.  
**Utilité :** grottes, cavernes.  
**Limites :** zones parfois isolées.

---

##  4. Simplex Noise Generator
![Simplex Noise Generator](Assets/Gifs/SimplexNoise.gif)

Génère un noise grace à la lib "FastNoiseLit".  
**Utilité :** terrains, biomes, heatmaps.  
**Limites :** ne produit pas de structures.

---

#  Installation

```
git clone https://github.com/Tim4270/ProceduralGenerationTools.git
```

Ouvrir dans Unity **2022.3+** et amusez vous à tester les différents générations procédurales !

---

#  Licence
MIT License
