using System.Collections.Generic;
using UnityEngine;

public class RandomWalkGenerator : MonoBehaviour
{
    [Header("Parámetros del Sendero Procedural")]
    [Tooltip("Usa un valor MUY ALTO (ej. 3000 o 5000) para que tenga tiempo de explorar todo el mapa.")]
    public int maxSteps = 3000;
    public int pathWidth = 2; // Usa 2 para evitar el patrón cuadriculado/punteado

    [Range(0f, 0.05f)]
    public float pathDepth = 0.01f;

    [Header("Comportamiento Orgánico")]
    [Tooltip("Probabilidad de girar levemente (0 a 100).")]
    [Range(0f, 100f)]
    public float directionChangeProbability = 15f;

    [Range(0f, 1f)]
    public float flattenStrength = 0.6f;

    public HashSet<Vector2Int> pathPositions { get; private set; } = new HashSet<Vector2Int>();

    // Las 8 direcciones posibles (N, NE, E, SE, S, SO, O, NO)
    private Vector2Int[] directions = {
        new Vector2Int(0, 1),
        new Vector2Int(1, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, -1),
        new Vector2Int(0, -1),
        new Vector2Int(-1, -1),
        new Vector2Int(-1, 0),
        new Vector2Int(-1, 1)
    };

    public void CarvePath(float[,] heights)
    {
        pathPositions.Clear();
        int resolution = heights.GetLength(0);

        // Empezamos en un punto aleatorio del mapa, lejos de los bordes
        Vector2Int currentPos = new Vector2Int(Random.Range(10, resolution - 10), Random.Range(10, resolution - 10));

        // Empezamos apuntando en una dirección aleatoria
        int currentDirIndex = Random.Range(0, 8);

        for (int i = 0; i < maxSteps; i++)
        {
            CarveArea(heights, currentPos, resolution);

            // Inercia Direccional: El caminante gira suavemente de a 45 grados
            if (Random.Range(0f, 100f) < directionChangeProbability)
            {
                int turn = (Random.value > 0.5f) ? 1 : -1;
                currentDirIndex = (currentDirIndex + turn + 8) % 8;
            }

            Vector2Int nextPos = currentPos + directions[currentDirIndex];

            // SISTEMA DE REBOTE: Si choca con los límites, NO HACEMOS BREAK. 
            // Obligamos a la dirección a dar un giro brusco hacia el interior y saltamos el paso.
            if (nextPos.x <= pathWidth + 1 || nextPos.x >= resolution - pathWidth - 2 ||
                  nextPos.y <= pathWidth + 1 || nextPos.y >= resolution - pathWidth - 2)
            {
                // Sumar 2 índices (+90°) o 6 índices (-90°)
                int turnAngle = (Random.value > 0.5f) ? 2 : 6;
                currentDirIndex = (currentDirIndex + turnAngle) % 8;
                continue;
            }

            currentPos = nextPos;
        }
    }

    private void CarveArea(float[,] heights, Vector2Int center, int resolution)
    {
        float centerHeight = heights[center.y, center.x] - pathDepth;

        for (int x = center.x - pathWidth; x <= center.x + pathWidth; x++)
        {
            for (int y = center.y - pathWidth; y <= center.y + pathWidth; y++)
            {
                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    pathPositions.Add(pos);

                    float distance = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(x, y));
                    if (distance <= pathWidth)
                    {
                        float falloff = 1f - (distance / (float)Mathf.Max(1, pathWidth));
                        falloff = Mathf.SmoothStep(0f, 1f, falloff);

                        float targetHeight = heights[y, x] - (pathDepth * falloff);
                        targetHeight = Mathf.Lerp(targetHeight, centerHeight, falloff * flattenStrength);

                        heights[y, x] = Mathf.Clamp01(targetHeight);
                    }
                }
            }
        }
    }
}