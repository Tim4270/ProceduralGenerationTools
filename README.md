# ProceduralGenerationTools

![Unity](https://img.shields.io/badge/Unity-2022.3+-black?logo=unity)
![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)
![C#](https://img.shields.io/badge/C%23-8.0-blue)

Outils modulaires et extensibles pour créer des systèmes de **génération procédurale sur grille** dans Unity.  
Pensé pour être simple, flexible, maintenable et compatible avec tout type d’architecture.

---

## ✨ Caractéristiques principales

- **Système de grille flexible** : `Grid`, `Cell`, `GridObject`
- **Méthodes de génération extensibles** basées sur `ProceduralGenerationMethod`
- **Génération asynchrone** (support `CancellationToken`)
- **Visualisation temps-réel** des étapes
- **Base solide pour créer vos propres algorithmes** : bruit, donjons, automates cellulaires, etc.
- **Aucune dépendance externe obligatoire**

---

## 📦 Technologies utilisées (inspirations)

```
UniTask
Zenject
R3
PrimeTween
```

---

## 📥 Installation

Clonez ce dépôt :

```bash
git clone https://github.com/Tim4270/ProceduralGenerationTools.git
```

Puis ouvrez le projet dans **Unity 2022.3+**.

---

## 🚀 Exemple rapide

### 1. Créer une grille

```csharp
var grid = new Grid(width: 32, length: 32, cellSize: 1f);
```

### 2. Lancer une génération

```csharp
var method = ScriptableObject.CreateInstance<SimplexNoiseGenerator>();
await method.GenerateAsync(grid);
```

### 3. Visualiser  
Ajoutez un composant `ProceduralGridGenerator` dans la scène et assignez votre generator.

---

## 🧱 Architecture du projet

```
ProceduralGenerationTools
 ├─ Grid/
 │   ├─ Grid
 │   ├─ Cell
 │   └─ GridObject
 ├─ GenerationMethods/
 │   ├─ ProceduralGenerationMethod (abstract)
 │   ├─ SimplexNoiseGenerator
 │   └─ SimpleRoomPlacement
 ├─ Services/
 │   └─ RandomService
 ├─ Visual/
 │   └─ ProceduralGridGenerator
 └─ ScriptableObjectDatabase/
```

---

## 🧩 Créer votre propre générateur

1. Créez une classe dérivée :

```csharp
public class MyGenerator : ProceduralGenerationMethod
{
    protected override async UniTask ApplyGeneration(CancellationToken token)
    {
        foreach (var cell in Grid.Cells)
        {
            token.ThrowIfCancellationRequested();
            cell.SetCellView("Grass");
            await DelayStep(token); // pour visualisation
        }
    }
}
```

2. Créez l’asset via :  
**Create → Procedural Generation → MyGenerator**

3. Assignez-le dans `ProceduralGridGenerator`.

---

## 🔍 Méthodes existantes

### **SimplexNoiseGenerator**
- Génère une heightmap
- Mapping : eau, sable, herbe, roche
- Basé sur `FastNoiseLite`

### **SimpleRoomPlacement**
- Placement aléatoire de salles
- Création de corridors
- Génération finale du sol

---

## 🔧 Paramètres disponibles

| Paramètre        | Description |
|------------------|-------------|
| **Seed**         | Graine aléatoire utilisée pour la génération |
| **StepDelay**    | Délai visuel entre les étapes |
| **GridSize**     | Dimensions de la grille |
| **TileDatabase** | Base des tuiles affichables |

---

## 📚 API (Résumé)

### `Grid`
```csharp
public int Width;
public int Length;
public float CellSize;
public Cell[,] Cells;
```

### `Cell`
```csharp
public Vector2Int Coordinates;
public bool ContainObject;
public GridObject GridObject;
public void SetCellView(string id);
```

### `ProceduralGenerationMethod`
```csharp
protected abstract UniTask ApplyGeneration(CancellationToken token);
protected UniTask DelayStep(CancellationToken token);
```

---

## 🐛 Débogage

- Assurez-vous que `ProceduralGridGenerator` est présent dans la scène  
- Assurez-vous qu’une méthode est assignée  
- Vérifiez les IDs des tuiles dans `ScriptableObjectDatabase`  
- Un `StepDelay` trop bas peut rendre la visualisation difficile  

---

## 🤝 Contribution

Les contributions sont les bienvenues !

1. Forkez ce dépôt  
2. Créez une branche : `feature/ma-feature`  
3. Ouvrez une Pull Request  

---

## 📄 Licence

MIT — libre d’utilisation et de modification.

---

## 👤 Auteur

**Tim4270**
