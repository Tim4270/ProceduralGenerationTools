using Components.ProceduralGeneration;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(menuName = "Procedural Generation Method/ Simplex Noise Generator")]
public class SimplexNoiseGenerator : ProceduralGenerationMethod
{
    public enum NoiseAlgo { OpenSimplex2, OpenSimplex2S, Cellular, Perlin, ValueCubic, Value }
    public enum FractalAlgo { None, FBm, Ridged, PingPong, DomainWarpProgressive, DomainWarpIndependent }

    [Header("Noise Parameters")]
    [SerializeField] private NoiseAlgo noiseType = NoiseAlgo.OpenSimplex2;
    [Tooltip("Frequency (higher -> finer details)")]
    [SerializeField, Range(0.0001f, 1f)] private float frequency = 0.03f;
    [Tooltip("Amplitude multiplier applied to raw noise")]
    [SerializeField, Range(0f, 10f)] private float amplitude = 1.0f;

    [Header("Fractal Parameters")]
    [SerializeField] private FractalAlgo fractalType = FractalAlgo.None;
    [SerializeField, Range(1, 8)] private int octaves = 2;
    [SerializeField, Range(0.1f, 4f)] private float lacunarity = 1.2f;
    [SerializeField, Range(0f, 1f)] private float persistence = 0.5f;

    [Header("Offsets")]
    [SerializeField] private Vector2 offset = Vector2.zero;

    [Header("Heights (-1 .. 1) order:  water <= sand <= grass <= rock")]
    [SerializeField, Range(-1f, 1f)] private float waterHeight = -0.92f;
    [SerializeField, Range(-1f, 1f)] private float sandHeight = -0.76f;
    [SerializeField, Range(-1f, 1f)] private float grassHeight = 0.64f;
    [SerializeField, Range(-1f, 1f)] private float rockHeight = 1f;

    [Header("Runtime / Debug")]
    [SerializeField] private bool visualizeDuringGeneration = false;
    [SerializeField, Range(1, 1000)] private int visualDelayMs = 1;

    // internal
    private FastNoiseLite _noise;

    private void OnValidate()
    {
        // enforce ordering sand <= water <= grass <= rock (correct values if user moves sliders)
        float s = sandHeight;
        float w = Mathf.Max(waterHeight, s);
        float g = Mathf.Max(grassHeight, w);
        float r = Mathf.Max(rockHeight, g);
        sandHeight = Mathf.Clamp(s, -1f, 1f);
        waterHeight = Mathf.Clamp(w, -1f, 1f);
        grassHeight = Mathf.Clamp(g, -1f, 1f);
        rockHeight = Mathf.Clamp(r, -1f, 1f);

        // reconfigure noise only (do not write grid during edit-time)
        InitializeNoise();

        // If in Play mode and generator was initialized, reapply immediately
        if (Application.isPlaying && GridGenerator != null)
        {
            // fire & forget, Refresh will ensure main thread and GridGenerator present
            Refresh().Forget();
        }
    }

    private void InitializeNoise()
    {
        var seed = RandomService != null ? RandomService.Seed : 1337;
        _noise = new FastNoiseLite(seed);

        // map enum
        _noise.SetNoiseType(noiseType switch
        {
            NoiseAlgo.OpenSimplex2 => FastNoiseLite.NoiseType.OpenSimplex2,
            NoiseAlgo.OpenSimplex2S => FastNoiseLite.NoiseType.OpenSimplex2S,
            NoiseAlgo.Cellular => FastNoiseLite.NoiseType.Cellular,
            NoiseAlgo.Perlin => FastNoiseLite.NoiseType.Perlin,
            NoiseAlgo.ValueCubic => FastNoiseLite.NoiseType.ValueCubic,
            NoiseAlgo.Value => FastNoiseLite.NoiseType.Value,
            _ => FastNoiseLite.NoiseType.OpenSimplex2
        });

        _noise.SetSeed(seed);
        _noise.SetFrequency(frequency);

        if (fractalType != FractalAlgo.None)
        {
            _noise.SetFractalType(fractalType switch
            {
                FractalAlgo.FBm => FastNoiseLite.FractalType.FBm,
                FractalAlgo.Ridged => FastNoiseLite.FractalType.Ridged,
                FractalAlgo.PingPong => FastNoiseLite.FractalType.PingPong,
                FractalAlgo.DomainWarpProgressive => FastNoiseLite.FractalType.DomainWarpProgressive,
                FractalAlgo.DomainWarpIndependent => FastNoiseLite.FractalType.DomainWarpIndependent,
                _ => FastNoiseLite.FractalType.None
            });

            _noise.SetFractalOctaves(Mathf.Max(1, octaves));
            _noise.SetFractalGain(persistence);
            _noise.SetFractalLacunarity(lacunarity);
        }
        else
        {
            _noise.SetFractalType(FastNoiseLite.FractalType.None);
        }
    }

    // public API (runtime)
    public void SetFrequency(float newFrequency, bool reapply = true)
    {
        frequency = Mathf.Max(0.0001f, newFrequency);
        InitializeNoise();
        if (reapply && Application.isPlaying) Refresh().Forget();
    }

    public void SetAmplitude(float newAmplitude, bool reapply = true)
    {
        amplitude = Mathf.Max(0f, newAmplitude);
        if (reapply && Application.isPlaying) Refresh().Forget();
    }

    public void SetHeightsOrdered(float newSand, float newWater, float newGrass, float newRock, bool reapply = true)
    {
        waterHeight = Mathf.Clamp(newWater, -1f, 1f);
        sandHeight = Mathf.Clamp(Mathf.Max(newSand, waterHeight), -1f, 1f);
        grassHeight = Mathf.Clamp(Mathf.Max(newGrass, sandHeight), -1f, 1f);
        rockHeight = Mathf.Clamp(Mathf.Max(newRock, grassHeight), -1f, 1f);
        if (reapply && Application.isPlaying) Refresh().Forget();
    }

    // Reapply noise to grid on main thread
    public async UniTask Refresh()
    {
        InitializeNoise();
        if (!Application.isPlaying) return;

        // Ensure GridGenerator and RandomService are injected
        if (GridGenerator == null)
        {
            Debug.LogWarning("SimplexNoiseGenerator.Refresh: GridGenerator not set. Call ProceduralGridGenerator.Initialize(...) before refreshing.");
            return;
        }

        await UniTask.SwitchToMainThread();
        await ApplyNoiseToGridAsync(CancellationToken.None, visualizeDuringGeneration);
    }

    protected override async UniTask ApplyGeneration(CancellationToken cancellationToken)
    {
        Debug.Log("Starting Simplex Noise Generation");
        InitializeNoise();
        await UniTask.SwitchToMainThread();
        await ApplyNoiseToGridAsync(cancellationToken, visualizeDuringGeneration);
        Debug.Log("Simplex Noise Generation finished");
        await UniTask.Delay(GridGenerator.StepDelay, cancellationToken: cancellationToken);
    }

    private async UniTask ApplyNoiseToGridAsync(CancellationToken cancellationToken, bool allowDelay)
    {
        // ensure we are on main thread because we access Grid / Unity objects
        await UniTask.SwitchToMainThread();

        if (_noise == null) InitializeNoise();

        int width = Grid.Width;
        int height = Grid.Lenght;

        for (int x = 0; x < width; x++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            for (int y = 0; y < height; y++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                double sx = x + offset.x;
                double sy = y + offset.y;
                float raw = _noise.GetNoise(sx, sy); // [-1..1]
                raw *= amplitude;                     // apply amplitude

                // clamp to [-1,1] to compare with heights
                float sample = Mathf.Clamp(raw, -1f, 1f);

                string target;
                if (sample <= waterHeight) target = WATER_TILE_NAME;
                else if (sample <= sandHeight) target = SAND_TILE_NAME;
                else if (sample <= grassHeight) target = GRASS_TILE_NAME;
                else target = ROCK_TILE_NAME;

                if (Grid.TryGetCellByCoordinates(x, y, out var cell))
                {
                    // avoid unnecessary writes
                    if (!cell.ContainObject)
                    {
                        AddTileToCell(cell, target, true);
                    }
                    else
                    {
                        var current = cell.GridObject?.Template?.Name;
                        if (current != target)
                            AddTileToCell(cell, target, true);
                    }
                }
            }

            if (allowDelay && visualizeDuringGeneration)
            {
                await UniTask.Delay(visualDelayMs, cancellationToken: cancellationToken);
            }
        }
    }
}
