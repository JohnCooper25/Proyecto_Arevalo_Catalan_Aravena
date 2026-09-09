using UnityEngine;

public class CellularAutomata : MonoBehaviour
{
    [Header("Tamaño del mapa")]
    public int width = 50;
    public int height = 50;

    [Header("Parámetros del Cellular Automata")]
    [Range(0f, 1f)]
    public float rockChance = 0.45f;

    public int iterations = 2;
    public int neighborThreshold = 4;

    [Header("Visual")]
    public GameObject floorPrefab;
    public GameObject rockPrefab;

    [Header("Generación")]
    public bool generateOnStart = true;

    private int[,] grid;

    private void Start()
    {
        if (generateOnStart)
        {
            Generate();
        }
    }

    public void Generate()
    {
        InitializeGrid();

        RunCellularAutomata();

        DrawMap();
    }

    // ---------------------------------------------------------
    // 1. Crear la cuadrícula inicial
    // ---------------------------------------------------------
    private void InitializeGrid()
    {
        grid = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                // 0 = suelo
                // 1 = roca
                grid[x, y] = 0;
            }
        }

        // Cambiar aleatoriamente una proporción de celdas
        // de suelo a roca.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (Random.value < rockChance)
                {
                    grid[x, y] = 1;
                }
            }
        }
    }

    // ---------------------------------------------------------
    // 2. Ejecutar Cellular Automata
    // ---------------------------------------------------------
    private void RunCellularAutomata()
    {
        for (int iteration = 0; iteration < iterations; iteration++)
        {
            int[,] newGrid = new int[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    int rockNeighbors = CountRockNeighbors(x, y);

                    // Si tiene al menos T vecinos roca
                    // se convierte en roca.
                    if (rockNeighbors >= neighborThreshold)
                    {
                        newGrid[x, y] = 1;
                    }
                    else
                    {
                        newGrid[x, y] = 0;
                    }
                }
            }

            // Reemplazar la cuadrícula anterior
            grid = newGrid;
        }
    }

    // ---------------------------------------------------------
    // 3. Contar los 4 vecinos
    // ---------------------------------------------------------
    private int CountRockNeighbors(int x, int y)
    {
        int count = 0;

        // Arriba
        if (IsRock(x, y + 1))
            count++;

        // Abajo
        if (IsRock(x, y - 1))
            count++;

        // Izquierda
        if (IsRock(x - 1, y))
            count++;

        // Derecha
        if (IsRock(x + 1, y))
            count++;

        return count;
    }

    // ---------------------------------------------------------
    // Comprobar si una posición es roca
    // ---------------------------------------------------------
    public bool IsRock(int x, int y)
    {
        // Fuera del mapa = roca.
        // Esto ayuda a mantener los bordes cerrados.
        if (x < 0 || x >= width || y < 0 || y >= height)
        {
            return true;
        }

        return grid[x, y] == 1;
    }

    // ---------------------------------------------------------
    // 4. Dibujar el resultado
    // ---------------------------------------------------------
    private void DrawMap()
    {
        // Eliminar mapa anterior
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Destroy(transform.GetChild(i).gameObject);
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GameObject prefab;

                if (grid[x, y] == 1)
                {
                    prefab = rockPrefab;
                }
                else
                {
                    prefab = floorPrefab;
                }

                if (prefab != null)
                {
                    GameObject tile = Instantiate(
                        prefab,
                        new Vector3(x, y, 0),
                        Quaternion.identity,
                        transform
                    );

                    tile.name = $"Cell_{x}_{y}";
                }
            }
        }
    }
}