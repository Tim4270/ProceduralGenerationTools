using Cysharp.Threading.Tasks;
using Microsoft.Unity.VisualStudio.Editor;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Rendering;
using VTools.Grid;
using VTools.ScriptableObjectDatabase;

namespace Components.ProceduralGeneration.SimpleRoomPlacement
{
    [CreateAssetMenu(menuName = "Procedural Generation Method/Simple Room Placement")]
    public class SimpleRoomPlacement : ProceduralGenerationMethod
    {
        [Header("Room Parameters")]
        [SerializeField] private int _maxRooms = 10;

        protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
        {
            // Declare variables here
            List<Vector2Int> _roomCenters = new List<Vector2Int>();
            // ........

            for (int i = 0; i < _maxSteps; i++)
            {

                if( _roomCenters.Count >= _maxRooms)
                    break;

                // Check for cancellation
                cancellationToken.ThrowIfCancellationRequested();

                // Your algorithm here
                // .......

                int x = RandomService.Range(0, Grid.Width);
                int y = RandomService.Range(0, Grid.Lenght);
                int width = RandomService.Range(5, 10);




                RectInt roomRect = new RectInt(x, y, width, 10);
                if (CanPlaceRoom(roomRect, 1))
                {
                    PlaceRoom(roomRect);

                    Vector2Int center = GetRoomCenter(roomRect);

                    _roomCenters.Add(center);

                    if (_roomCenters.Count >= _maxRooms)
                        break;
                }
                else
                {
                    // Room cannot be placed, try again in the next iteration.
                    continue;
                }

            // Waiting between steps to see the result.
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

            }

            // Après avoir placé toutes les salles, connecter chaque salle à la salle la plus proche
            for (int i = 1; i < _roomCenters.Count; i++)
            {
                Vector2Int current = _roomCenters[i];

                int bestIndex = 0;
                int bestDistSq = int.MaxValue;

                // chercher la salle la plus proche parmi celles déjà placées (index < i)
                for (int j = 0; j < i; j++)
                {
                    int dx = _roomCenters[j].x - current.x;
                    int dy = _roomCenters[j].y - current.y;
                    int distSq = dx * dx + dy * dy;
                    if (distSq < bestDistSq)
                    {
                        bestDistSq = distSq;
                        bestIndex = j;
                    }
                }

                PlaceCorridor(_roomCenters[bestIndex], current, 1);
            }

            // Final ground building.
            BuildGround();
        }
        
        private void BuildGround()
        {
            var groundTemplate = ScriptableObjectDatabase.GetScriptableObject<GridObjectTemplate>("Grass");
            
            // Instantiate ground blocks
            for (int x = 0; x < Grid.Width; x++)
            {
                for (int z = 0; z < Grid.Lenght; z++)
                {
                    if (!Grid.TryGetCellByCoordinates(x, z, out var chosenCell))
                    {
                        Debug.LogError($"Unable to get cell on coordinates : ({x}, {z})");
                        continue;
                    }
                    
                    GridGenerator.AddGridObjectToCell(chosenCell, groundTemplate, false);
                }
            }
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
    }
}