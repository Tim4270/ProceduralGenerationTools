using UnityEngine;
using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;

[CreateAssetMenu(menuName = "Procedural Generation Method/ Cellular Automata")]

public class CellularAutomata : ProceduralGenerationMethod
{
    [Header("Parameters")]
    [SerializeField] private int iterations;
    [SerializeField] private int noise_density;
    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        Debug.Log("Starting Cellular Automata Generation");


        BuildNoiseGrid();
        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);

        for (int i = 0; i < iterations; i++)
        {
            PerformIteration();
            await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
        }
    }

    public void BuildNoiseGrid()
    {
        // Implementation for building noise grid if needed
        int width = Grid.Width;
        int height = Grid.Lenght;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // Randomly set cells as alive or dead
                bool isAlive = RandomService.Range(0, 101) < noise_density; 
                if (isAlive)
                {
                    if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                    {
                        AddTileToCell(cell, GRASS_TILE_NAME, true);
                    }
                }
                else
                {
                    if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                    {
                        AddTileToCell(cell, WATER_TILE_NAME, true);
                    }
                }
            }
        }
    }

    public void PerformIteration()
    {
        int width = Grid.Width;
        int height = Grid.Lenght;
        // Crée une copie de l'état actuel de la grille
        bool[,] newGridState = new bool[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int aliveNeighbors = CountAliveNeighbors(x, y);
                if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                {
                    newGridState[x, y] = aliveNeighbors >= 4; // elle devient une grass si elle a 4 voisins ou plus
                }
            }
        }
        // Update the grid based on the new state
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                {
                    if (newGridState[x, y])
                    {
                        AddTileToCell(cell, GRASS_TILE_NAME, true);
                    }
                    else
                    {
                        AddTileToCell(cell, WATER_TILE_NAME, true);
                    }
                }
            }
        }
    }

    public int CountAliveNeighbors(int x, int y)
    {
        int aliveCount = 0;
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0) continue; // Skip the cell itself
                int nx = x + dx;
                int ny = y + dy;
                if (Grid.TryGetCellByCoordinates(nx, ny, out var neighborCell))
                {
                    if (neighborCell.ContainObject && neighborCell.GridObject.Template.Name == GRASS_TILE_NAME)
                    {
                        aliveCount++;
                    }
                }
            }
        }
        return aliveCount;
    }

}
