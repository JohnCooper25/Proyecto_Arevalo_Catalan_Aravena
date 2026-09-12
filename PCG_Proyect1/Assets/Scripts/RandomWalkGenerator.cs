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
            // LÓGICA VOLCÁN: Múltiples ríos que fluyen desde el cráter hacia afuera
            int center = resolution / 2;
            int numRivers = 5; // 5 lenguas de lava principales
            int stepsPerRiver = maxSteps / numRivers;

            for (int r = 0; r < numRivers; r++)
            {
                Vector2Int currentPos = new Vector2Int(center, center);
                int currentDirIndex = Random.Range(0, 8);

                for (int i = 0; i < stepsPerRiver; i++)
                {
                    // Generar charcos dinámicos
                    if (Random.Range(0f, 100f) < 3f)
                    {
                        int originalWidth = pathWidth;
                        pathWidth = originalWidth * Random.Range(2, 5);
                        CarveArea(heights, currentPos, resolution);
                        pathWidth = originalWidth;
                    }
                    else
                    {
                        CarveArea(heights, currentPos, resolution);
                    }

                    // Inercia pesada: La lava gira menos que un caminante normal para simular flujo
                    if (Random.Range(0f, 100f) < (directionChangeProbability * 0.4f))
                    {
                        int turn = (Random.value > 0.5f) ? 1 : -1;
                        currentDirIndex = (currentDirIndex + turn + 8) % 8;
                    }

                    Vector2Int nextPos = currentPos + directions[currentDirIndex];

                    // Si la lava llega al borde del mapa, se derrama y el río termina (sin rebotar)
                    if (nextPos.x <= pathWidth + 1 || nextPos.x >= resolution - pathWidth - 2 ||
                        nextPos.y <= pathWidth + 1 || nextPos.y >= resolution - pathWidth - 2)
                    {
                        break;
                    }

                    currentPos = nextPos;
                }
            }
        }
        else
        {
            // LÓGICA NIEVE: Un solo explorador que rebota por todo el mapa
            Vector2Int currentPos = new Vector2Int(Random.Range(10, resolution - 10), Random.Range(10, resolution - 10));
            int currentDirIndex = Random.Range(0, 8);

            for (int i = 0; i < maxSteps; i++)
            {
                CarveArea(heights, currentPos, resolution);

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