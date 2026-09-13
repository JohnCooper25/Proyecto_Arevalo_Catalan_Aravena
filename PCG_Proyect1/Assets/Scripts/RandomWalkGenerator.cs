using System.Collections.Generic;
using UnityEngine;

public class RandomWalkGenerator : MonoBehaviour
{
    [Header("Parámetros del Sendero Procedural")]
    public int maxSteps = 300;
    public int numberOfPaths = 5; 
    public int pathWidth = 0;

    [Range(0f, 0.05f)]
    public float pathDepth = 0.01f;

    [Header("Comportamiento Orgánico")]
    [Range(0f, 100f)]
    public float directionChangeProbability = 15f;

    [Range(0f, 1f)]
    public float flattenStrength = 0.3f;

    [Header("Control de Montaña")]
    [Tooltip("Altura máxima a la que el camino subirá. Ajústalo para que llegue a la ladera pero no cruce la nieve (Ej: 0.8).")]
    [Range(0.5f, 1f)]
    public float maxMountainHeight = 0.8f;

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
            int center = resolution / 2;
            int numRivers = numberOfPaths; 
            int stepsPerRiver = maxSteps / numRivers;

            for (int r = 0; r < numRivers; r++)
            {
                Vector2Int currentPos = new Vector2Int(center, center);
                int currentDirIndex = Random.Range(0, 8);

                for (int i = 0; i < stepsPerRiver; i++)
                {
                    CarveAreaDynamic(heights, currentPos, resolution, pathWidth);

                    if (Random.Range(0f, 100f) < directionChangeProbability)
                    {
                        int turn = (Random.value > 0.5f) ? 1 : -1;
                        currentDirIndex = (currentDirIndex + turn + 8) % 8;
                    }

                    Vector2Int nextPos = currentPos + directions[currentDirIndex];

                    if (nextPos.x <= pathWidth + 1 || nextPos.x >= resolution - pathWidth - 2 ||
                        nextPos.y <= pathWidth + 1 || nextPos.y >= resolution - pathWidth - 2)
                    {
                        break;
                    }
                    currentPos = nextPos;
                }

                int originalWidth = pathWidth;
                pathWidth = originalWidth * Random.Range(6, 10);
                CarveAreaDynamic(heights, currentPos, resolution, pathWidth);
                pathWidth = originalWidth;
            }
        }
        else
        {
            // LÓGICA DE BOSQUE/NIEVE
           
            HashSet<Vector2Int> globalVisited = new HashSet<Vector2Int>();

            for (int p = 0; p < numberOfPaths; p++)
            {
                Vector2Int currentPos = Vector2Int.zero;
                int currentDirIndex = 0;

                if (p == 0) { currentPos = new Vector2Int(Random.Range(10, resolution - 10), 3); currentDirIndex = 0; }
                else if (p == 1) { currentPos = new Vector2Int(Random.Range(10, resolution - 10), resolution - 4); currentDirIndex = 4; }
                else if (p == 2) { currentPos = new Vector2Int(3, Random.Range(10, resolution - 10)); currentDirIndex = 2; }
                else { currentPos = new Vector2Int(resolution - 4, Random.Range(10, resolution - 10)); currentDirIndex = 6; }

                if (heights[currentPos.y, currentPos.x] > maxMountainHeight) continue;

                for (int i = 0; i < maxSteps; i++)
                {
                    globalVisited.Add(currentPos);
                    CarveAreaDynamic(heights, currentPos, resolution, pathWidth);

                    if (Random.Range(0f, 100f) < directionChangeProbability)
                    {
                        int turn = (Random.value > 0.5f) ? 1 : -1;
                        currentDirIndex = (currentDirIndex + turn + 8) % 8;
                    }

                    Vector2Int nextPos = currentPos + directions[currentDirIndex];

                    // Detiene el camino si choca con los límites de altura o con su propio rastro
                    if (nextPos.x <= 2 || nextPos.x >= resolution - 3 ||
                        nextPos.y <= 2 || nextPos.y >= resolution - 3 ||
                        heights[nextPos.y, nextPos.x] > maxMountainHeight ||
                        globalVisited.Contains(nextPos))
                    {
                        // Intenta esquivar la montaña bordeándola en lugar de rendirse de inmediato
                        bool found = false;
                        for (int offset = 1; offset <= 2; offset++)
                        {
                            int dir1 = (currentDirIndex + offset) % 8;
                            Vector2Int p1 = currentPos + directions[dir1];
                            if (p1.x > 2 && p1.x < resolution - 3 && p1.y > 2 && p1.y < resolution - 3 &&
                                heights[p1.y, p1.x] <= maxMountainHeight && !globalVisited.Contains(p1))
                            {
                                currentDirIndex = dir1; found = true; break;
                            }

                            int dir2 = (currentDirIndex - offset + 8) % 8;
                            Vector2Int p2 = currentPos + directions[dir2];
                            if (p2.x > 2 && p2.x < resolution - 3 && p2.y > 2 && p2.y < resolution - 3 &&
                                heights[p2.y, p2.x] <= maxMountainHeight && !globalVisited.Contains(p2))
                            {
                                currentDirIndex = dir2; found = true; break;
                            }
                        }
                        if (!found) break;
                    }
                    else
                    {
                        currentPos = nextPos;
                    }
                }
            }
        }
    }

    private void CarveAreaDynamic(float[,] heights, Vector2Int center, int resolution, int width)
    {
        float centerHeight = heights[center.y, center.x] - pathDepth;

        for (int x = center.x - width; x <= center.x + width; x++)
        {
            for (int y = center.y - width; y <= center.y + width; y++)
            {   
                if (x >= 0 && x < resolution && y >= 0 && y < resolution)
                {
                    float distance = Vector2.Distance(new Vector2(center.x, center.y), new Vector2(x, y));
                    if (distance <= width)
                    {
                        pathPositions.Add(new Vector2Int(x, y));

                        float falloff = 1f - (distance / (float)Mathf.Max(1, width));
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