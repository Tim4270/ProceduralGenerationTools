using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using VTools.RandomService;


[CreateAssetMenu(menuName = "Procedural Generation Method/ BSP")]

public class BSP : ProceduralGenerationMethod
{
    [SerializeField] private int minLeafSize = 6;

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        Debug.Log("BSP Generation Method Applied");


        // Example BSP generation logic

        
        var allGrid = new RectInt(0, 0, Grid.Width, Grid.Lenght);
        List<Vector2Int> _roomCenters = new List<Vector2Int>();

        // Remplir la grille de tiles d'herbe
        for (int x = allGrid.xMin; x < allGrid.xMax; x++)
        {
            for (int y = allGrid.yMin; y < allGrid.yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var chosenCell))
                    continue;

                AddTileToCell(chosenCell, GRASS_TILE_NAME, true);
            }
        }

        var root = new BSPNode(allGrid, RandomService);
        
        root.RecursiveSplit(minLeafSize);

        var splitRects = new List<RectInt>();
        root.CollectSplitRects(splitRects);

        var leaves = new List<BSPNode>();
        root.CollectLeaves(leaves);

        Debug.Log($"Total Leaves: {leaves.Count}");



        PlaceSplitRects(splitRects);

        // Optionnel : petit délai pour visualiser la génération étape par étape
        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

        // Générer une room à l'intérieur de chaque feuille
        const int minRoomSize = 3;
        const int roomPadding = 2; // espace entre la room et le bord de la feuille
        const int maxAttemptsPlacements = 5;
        int roomsPlaced = 0;
        int leavesCount = leaves.Count;
        for (int li = 0; li < leaves.Count && roomsPlaced < leavesCount; li++)
        {
            var leaf = leaves[li];
            var area = leaf.Area;

            // Tenter plusieurs fois de placer une room dans la feuille
            bool roomPlaced = false;
            for (int attempt = 0; attempt < maxAttemptsPlacements; attempt++)
            {

                // Calculer largeur possible pour la room
                int maxRoomWidth = area.width - 2 * roomPadding;
                int roomWidth;
                if (maxRoomWidth >= minRoomSize)
                    roomWidth = RandomService.Range(minRoomSize, maxRoomWidth + 1); // maxExclusive
                else
                    roomWidth = Mathf.Max(1, maxRoomWidth); // si la zone est trop petite, on prend ce qui rentre

                // Calculer hauteur possible pour la room
                int maxRoomHeight = area.height - 2 * roomPadding;
                int roomHeight;
                if (maxRoomHeight >= minRoomSize)
                    roomHeight = RandomService.Range(minRoomSize, maxRoomHeight + 1);
                else
                    roomHeight = Mathf.Max(1, maxRoomHeight);

                // Si roomWidth ou roomHeight invalides (zone trop petite), on skip
                if (roomWidth <= 0 || roomHeight <= 0)
                    continue;

                // Position aléatoire de la room à l'intérieur de la feuille (en respectant le padding)
                int minX = area.xMin + roomPadding;
                int maxStartXExclusive = area.xMax - roomWidth + 1; // +1 car Range est exclusive sur la borne supérieure
                int startX = (maxStartXExclusive > minX)
                    ? RandomService.Range(minX, maxStartXExclusive)
                    : minX;

                int minY = area.yMin + roomPadding;
                int maxStartYExclusive = area.yMax - roomHeight + 1;
                int startY = (maxStartYExclusive > minY)
                    ? RandomService.Range(minY, maxStartYExclusive)
                    : minY;

                var roomRect = new RectInt(startX, startY, roomWidth, roomHeight);

                // NE PAS placer une room qui touche un split : vérifier intersection avec les splits agrandis (padding 1)
                bool touchesSplit = false;
                const int splitPadding = 1; // empêche le contact direct
                foreach (var s in splitRects)
                {
                    var expanded = new RectInt(s.xMin - splitPadding, s.yMin - splitPadding, s.width + splitPadding * 2, s.height + splitPadding * 2);
                    if (roomRect.Overlaps(expanded))
                    {
                        touchesSplit = true;
                        break;
                    }
                }
                if (touchesSplit)
                    continue;

                // Vérifier que la room peut être placée (spacing 1 pour éviter chevauchement immédiat)
                if (!CanPlaceRoom(roomRect, 1))
                    continue;

                // Sauvegarder la room dans le noeud pour usage ultérieur et placer les tiles
                leaf.room = roomRect;
                PlaceRoom(roomRect);
                roomPlaced = true;

                // Collecter le centre de la room (liste de rooms placées)
                var center = GetRoomCenter(roomRect);
                _roomCenters.Add(center);

                Debug.Log($"Placed room at {roomRect} (center {center})");

                // Optionnel : petit délai pour visualiser la génération étape par étape
                await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
            }

            if (!roomPlaced)
            {
                Debug.Log($"Failed to place room in leaf area {area}");
            }
        }

        

        // Connecter les rooms entre elles via les corridors
        // Remplace la connexion basée sur l'arbre par une connexion séquentielle suivant l'ordre de placement.
        ConnectRoomsSequentially(_roomCenters, 1);

        // Optionnel : petit délai pour visualiser la génération étape par étape
        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);


    }


    private void PlaceRoom(RectInt room)
    {
        for (int x = room.xMin; x < room.xMax; x++)
        {
            for (int y = room.yMin; y < room.yMax; y++)
            {
                if (!Grid.TryGetCellByCoordinates(x, y, out var chosenCell))
                    continue;

                AddTileToCell(chosenCell, ROOM_TILE_NAME, true);
            }
        }
    }
    public Vector2Int GetRoomCenter(RectInt room)
    {
        return new Vector2Int(room.xMin + room.width / 2, room.yMin + room.height / 2);
    }

    private void PlaceCorridor(Vector2Int from, Vector2Int to, int corridorWidth)
    {
        bool horizontalFirst = RandomService.Chance(0.5f);

        if (horizontalFirst)
        {
            int startX = Mathf.Min(from.x, to.x);
            int endX = Mathf.Max(from.x, to.x);
            for (int x = startX; x <= endX; x++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int yy = from.y + w;
                    if (!Grid.TryGetCellByCoordinates(x, yy, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
                }
            }

            int startY = Mathf.Min(from.y, to.y);
            int endY = Mathf.Max(from.y, to.y);
            for (int y = startY; y <= endY; y++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int xx = to.x + w;
                    if (!Grid.TryGetCellByCoordinates(xx, y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
                }
            }
        }
        else
        {
            int startY = Mathf.Min(from.y, to.y);
            int endY = Mathf.Max(from.y, to.y);
            for (int y = startY; y <= endY; y++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int xx = from.x + w;
                    if (!Grid.TryGetCellByCoordinates(xx, y, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
                }
            }

            int startX = Mathf.Min(from.x, to.x);
            int endX = Mathf.Max(from.x, to.x);
            for (int x = startX; x <= endX; x++)
            {
                for (int w = 0; w < corridorWidth; w++)
                {
                    int yy = to.y + w;
                    if (!Grid.TryGetCellByCoordinates(x, yy, out var cell)) continue;
                    AddTileToCell(cell, CORRIDOR_TILE_NAME, true);
                }
            }
        }
    }
    private void PlaceSplitRects(List<RectInt> roomRects)
    {
        
        foreach (var roomRect in roomRects)
        {
            for (int x = roomRect.xMin; x < roomRect.xMax; x++)
            {
                for (int y = roomRect.yMin; y < roomRect.yMax; y++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, y, out var chosenCell))
                        continue;
                    AddTileToCell(chosenCell, ROCK_TILE_NAME, true);
                }
            }
        }
        
    }

    // Parcours post-order : connecte soeurs, puis remonte pour fournir un représentant au parent.
    // Le représentant est le centre de la room choisie (ici on retourne le midpoint arrondi entre les deux salles connectées)
    // si aucun room dans le sous-arbre -> retourne null.
    private Vector2Int? ConnectAndPropagate(BSPNode node)
    {
        if (node == null) return null;
        if (node.IsLeaf)
        {
            if (node.room.HasValue)
                return GetRoomCenter(node.room.Value);
            return null;
        }

        // d'abord traiter enfants (post-order)
        var repA = ConnectAndPropagate(node.Child1);
        var repB = ConnectAndPropagate(node.Child2);

        // si enfants ne contiennent pas directement de représentants, collecter centres dans chaque sous-arbre
        var aCenters = new List<Vector2Int>();
        var bCenters = new List<Vector2Int>();

        if (node.Child1 != null)
            node.Child1.CollectRoomCenters(aCenters);
        if (node.Child2 != null)
            node.Child2.CollectRoomCenters(bCenters);

        // si aucun centre dans l'un des sous-arbres, essayer d'utiliser repA/repB
        if (aCenters.Count == 0 && repA.HasValue) aCenters.Add(repA.Value);
        if (bCenters.Count == 0 && repB.HasValue) bCenters.Add(repB.Value);

        if (aCenters.Count > 0 && bCenters.Count > 0)
        {
            // trouver la paire la plus proche
            int bestDist = int.MaxValue;
            Vector2Int bestA = default;
            Vector2Int bestB = default;
            foreach (var a in aCenters)
            {
                foreach (var b in bCenters)
                {
                    int dx = a.x - b.x;
                    int dy = a.y - b.y;
                    int distSq = dx * dx + dy * dy;
                    if (distSq < bestDist)
                    {
                        bestDist = distSq;
                        bestA = a;
                        bestB = b;
                    }
                }
            }

            // placer le corridor entre les deux salles choisies (soeurs)
            PlaceCorridor(bestA, bestB, 1);
            Debug.Log($"Connected sisters {bestA} <-> {bestB}");

            // retourner un représentant pour le parent : midpoint arrondi
            var rep = new Vector2Int((bestA.x + bestB.x) / 2, (bestA.y + bestB.y) / 2);
            return rep;
        }

        // si on n'a qu'un seul côté avec des centres, retourner un représentant de ce côté
        if (aCenters.Count > 0) return aCenters[0];
        if (bCenters.Count > 0) return bCenters[0];

        return null;
    }

    // Nouvelle méthode : connecte les rooms dans l'ordre donné (liste de centres)
    private void ConnectRoomsSequentially(List<Vector2Int> centers, int corridorWidth = 1)
    {
        if (centers == null || centers.Count < 2)
        {
            Debug.Log("ConnectRoomsSequentially: pas assez de rooms à connecter.");
            return;
        }

        for (int i = 1; i < centers.Count; i++)
        {
            var a = centers[i - 1];
            var b = centers[i];
            PlaceCorridor(a, b, corridorWidth);
            Debug.Log($"Connected sequentially {a} -> {b}");
        }
    }
}


public class BSPNode
{
    private RectInt _area;
    private BSPNode child1;
    private BSPNode child2;
    public RectInt? room;

    private RandomService _randomService;   

    public RectInt Area => _area;
    public BSPNode Child1 => child1;
    public BSPNode Child2 => child2;

    public bool IsLeaf => child1 == null && child2 == null;
    public BSPNode(RectInt area, RandomService randomService)
    {
        _area = area;
        _randomService = randomService;

    }

    public bool Split(int minSize)
    {
        if(!IsLeaf)
            return false;
        if(_area.width < minSize *2 && _area.height < minSize *2)
            return false;

        bool splitHorizontal;
        if(_area.width < _area.height)
            splitHorizontal = true;
        else if(_area.width > _area.height)
            splitHorizontal = false;
        else
            splitHorizontal = _randomService.Chance(0.5f);

        if (splitHorizontal)
        {
            int  maxSplit = _area.height - minSize;
            int minSplit = minSize;
            if(maxSplit <= minSplit)
                return false;
            int split =  _randomService.Range(minSplit, maxSplit + 1);

            var a = new RectInt(_area.x, _area.y , _area.width , split);
            var b = new RectInt(_area.x, _area.y + split , _area.width , _area.height - split);

            child1 = new BSPNode(a, _randomService);
            child2 = new BSPNode(b, _randomService);

            Debug.Log($"Split Horizontal at {split} : A{a} B{b}");
            return true;
        }

        else
        {
            int  maxSplit = _area.width - minSize;
            int minSplit = minSize;
            if(maxSplit <= minSplit)
                return false;
            int split = _randomService.Range(minSplit, maxSplit + 1);
            var a = new RectInt(_area.x, _area.y , split , _area.height);
            var b = new RectInt(_area.x + split, _area.y , _area.width - split , _area.height);
            child1 = new BSPNode(a, _randomService);
            child2 = new BSPNode(b, _randomService);

            Debug.Log($"Split Vertical at {split} : A{a} B{b}");
            return true;
        }

       
    }

    public void RecursiveSplit(int minSize)
    {
        if(Split(minSize))
        {
            child1.RecursiveSplit(minSize);
            child2.RecursiveSplit(minSize);
        }
    }

    public void CollectLeaves(List<BSPNode> leaves)
    {
        if(IsLeaf)
        {
            leaves.Add(this);
            return;
        }
        else
        {
            child1.CollectLeaves(leaves);
            child2.CollectLeaves(leaves);
        }
    }

    public void CollectSplitRects(List<RectInt> outRects)
    {
        if (IsLeaf) return;

        if (child1 != null && child2 != null)
        {
            var a = child1.Area;
            var b = child2.Area;

            // Split horizontal : children empilés (même x/width)
            if (a.x == b.x && a.width == b.width)
            {
                int splitY = b.yMin;
                // rectangle horizontal d'épaisseur 1
                var rect = new RectInt(a.xMin, splitY, a.width, 1);
                outRects.Add(rect);
            }
            // Split vertical : children côte à côte (même y/height)
            else if (a.y == b.y && a.height == b.height)
            {
                int splitX = b.xMin;
                // rectangle vertical d'épaisseur 1
                var rect = new RectInt(splitX, a.yMin, 1, a.height);
                outRects.Add(rect);
            }
            else
            {
                // Cas non aligné : construit le rect englobant entre les deux bords de séparation
                int xMin = Mathf.Min(a.xMin, b.xMin);
                int xMax = Mathf.Max(a.xMax, b.xMax);
                int yMin = Mathf.Min(a.yMin, b.yMin);
                int yMax = Mathf.Max(a.yMax, b.yMax);
                var rect = new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
                outRects.Add(rect);
            }
        }

        child1?.CollectSplitRects(outRects);
        child2?.CollectSplitRects(outRects);
    }

    public void CollectRoomCenters(List<Vector2Int> roomCenters)
    {
        if (IsLeaf)
        {
            if (room.HasValue)
            {
                var center = new Vector2Int(
                    room.Value.xMin + room.Value.width / 2,
                    room.Value.yMin + room.Value.height / 2);
                roomCenters.Add(center);
            }
            return;
        }
        child1?.CollectRoomCenters(roomCenters);
        child2?.CollectRoomCenters(roomCenters);
    }
}