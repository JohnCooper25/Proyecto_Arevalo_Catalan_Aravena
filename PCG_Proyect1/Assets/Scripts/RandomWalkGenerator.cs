using System.Collections.Generic;
using UnityEngine;

public class RandomWalkGenerator : MonoBehaviour
{
    [Header("Parámetros del Sendero Procedural")]
    public int maxSteps = 3000;
    public int pathWidth = 2;

    [Range(0f, 0.05f)]
    public float pathDepth = 0.01f;

    [Header("Comportamiento Orgánico")]
    [Range(0f, 100f)]
    public float directionChangeProbability = 15f;

    [Range(0f, 1f)]
    public float flattenStrength = 0.6f;

    public HashSet<Vector2Int> pathPositions { get; private set; } = new HashSet<Vector2Int>();

    private Vector2Int[] directions = {
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(1, 0), new Vector2Int(1, -1),
        new Vector2Int(0, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 0), new Vector2Int(-1, 1)
    };

    public void CarvePath(float[,] heights)
    {
        pathPositions.Clear();
        int resolution = heights.GetLength(0);

        if (gameObject.name == "Terreno_Volcan")
        {
            // --- CONTEXTO 2: RÍOS FINOS CON CHARCO AL FINAL ---
            int center = resolution / 2;
            int numRivers = 5;
            int stepsPerRiver = maxSteps / numRivers;

            for (int r = 0; r < numRivers; r++)
            {
                Vector2Int currentPos = new Vector2Int(center, center);
                int currentDirIndex = Random.Range(0, 8);

                // 1. Dibuja el río de manera continua y delgada
                for (int i = 0; i < stepsPerRiver; i++)
                {
                    // Usa el pathWidth normal (ej. 2) todo el tiempo
                    CarveArea(heights, currentPos, resolution);

                    if (Random.Range(0f, 100f) < (directionChangeProbability * 0.4f))
                    {
                        int turn = (Random.value > 0.5f) ? 1 : -1;
                        currentDirIndex = (currentDirIndex + turn + 8) % 8;
                    }

                    Vector2Int nextPos = currentPos + directions[currentDirIndex];

                    if (nextPos.x <= pathWidth + 1 || nextPos.x >= resolution - pathWidth - 2 ||
                        nextPos.y <= pathWidth + 1 || nextPos.y >= resolution - pathWidth - 2)
                    {
                        break; // El río choca con el borde y se detiene
                    }

                    currentPos = nextPos;
                }

                // 2. EL CHARCO FINAL: Una vez que el ciclo for termina, inflamos la brocha en la última posición
                int originalWidth = pathWidth;
                pathWidth = originalWidth * Random.Range(6, 10); // Escala del lago final

                CarveArea(heights, currentPos, resolution); // Dibuja la laguna

                pathWidth = originalWidth; // Restaura el ancho fino para el siguiente río
            }
        }
        else
        {
            // --- CONTEXTO 1: EXPLORACIÓN NEVADA Y ZONAS DE DESCANSO ---
            Vector2Int currentPos = new Vector2Int(Random.Range(10, resolution - 10), Random.Range(10, resolution - 10));
            int currentDirIndex = Random.Range(0, 8);

            for (int i = 0; i < maxSteps; i++)
            {
                float currentHeight = heights[currentPos.y, currentPos.x];

                if (currentHeight < 0.45f && Random.Range(0f, 100f) < 2f)
                {
                    int originalWidth = pathWidth;
                    pathWidth = originalWidth * Random.Range(4, 7);
                    CarveArea(heights, currentPos, resolution);
                    pathWidth = originalWidth;
                }
                else
                {
                    CarveArea(heights, currentPos, resolution);
                }

                if (Random.Range(0f, 100f) < directionChangeProbability)
                {
                    int turn = (Random.value > 0.5f) ? 1 : -1;
                    currentDirIndex = (currentDirIndex + turn + 8) % 8;
                }

                Vector2Int nextPos = currentPos + directions[currentDirIndex];

                if (nextPos.x <= pathWidth + 1 || nextPos.x >= resolution - pathWidth - 2 ||
                    nextPos.y <= pathWidth + 1 || nextPos.y >= resolution - pathWidth - 2)
                {
                    int turnAngle = (Random.value > 0.5f) ? 2 : 6;
                    currentDirIndex = (currentDirIndex + turnAngle) % 8;
                    continue;
                }

                currentPos = nextPos;
            }
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
                    // Evaluamos la distancia PRIMERO
                    float distance = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(x, y));

                    if (distance <= pathWidth)
                    {
                        // SOLUCIÓN: Solo agregamos el punto al camino de color SI pertenece al círculo
                        pathPositions.Add(new Vector2Int(x, y));

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