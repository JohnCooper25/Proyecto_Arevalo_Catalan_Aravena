using System.Collections.Generic;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    [Header("Conexión de Generadores")]
    [Tooltip("El generador de terreno con Diamond-Square.")]
    public TerrainGenerator terrainGenerator;

    [Tooltip("El tallador del sendero.")]
    public RandomWalkGenerator randomWalkGenerator;

    [Tooltip("El árbol base que se clonará por el mapa.")]
    public LSystemTreeGenerator treeGeneratorReference;

    [Header("Configuración del Bosque")]
    [Tooltip("Cantidad de árboles a esparcir en el terreno.")]
    public int numberOfTrees = 20;

    [Header("Adaptación al Entorno (Nieve)")]
    [Tooltip("Altura normalizada (0 a 1) desde la cual los árboles se pintan de blanco.")]
    public float snowThreshold = 0.7f;

    [Tooltip("El color que tomarán los árboles en la cima.")]
    public Color snowTreeColor = Color.white;

    [Tooltip("Distancia mínima para alejar los árboles del camino.")]
    public int pathClearance = 4;

    [Tooltip("Distancia mínima entre un árbol y otro para evitar que se superpongan.")]
    public float minTreeDistance = 8f;

    private List<GameObject> spawnedTrees = new List<GameObject>();

    public void GenerateWorld()
    {
        ClearWorld();
        terrainGenerator.GenerateTerrain();

        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();
        if (terrain == null)
        {
            Debug.LogError("No se encontró el Terreno generado.");
            return;
        }

        TerrainData tData = terrain.terrainData;
        int res = tData.heightmapResolution;
        float[,] heights = tData.GetHeights(0, 0, res, res);

        if (randomWalkGenerator != null)
        {
            randomWalkGenerator.CarvePath(heights);
        }

        tData.SetHeights(0, 0, heights);

        if (randomWalkGenerator != null)
        {
            PaintPathOnTerrain(terrain);
        }

        if (treeGeneratorReference != null)
        {
            ScatterTrees(terrain, res);
        }
    }

    private void PaintPathOnTerrain(Terrain terrain)
    {
        TerrainData tData = terrain.terrainData;
        int alphaRes = tData.alphamapResolution;
        int heightRes = tData.heightmapResolution;
        float[,,] alphamaps = tData.GetAlphamaps(0, 0, alphaRes, alphaRes);

        // Aumentamos el multiplicador a 0.8f para garantizar que los puntos
        // se toquen entre sí y formen una línea ininterrumpida.
        float brushRadius = Mathf.Max(1f, ((float)alphaRes / heightRes) * 0.8f);

        foreach (Vector2Int pos in randomWalkGenerator.pathPositions)
        {
            int pX = Mathf.RoundToInt((pos.x / (float)(heightRes - 1)) * (alphaRes - 1));
            int pY = Mathf.RoundToInt((pos.y / (float)(heightRes - 1)) * (alphaRes - 1));

            int startX = Mathf.Max(0, pX - Mathf.CeilToInt(brushRadius));
            int endX = Mathf.Min(alphaRes - 1, pX + Mathf.CeilToInt(brushRadius));
            int startY = Mathf.Max(0, pY - Mathf.CeilToInt(brushRadius));
            int endY = Mathf.Min(alphaRes - 1, pY + Mathf.CeilToInt(brushRadius));

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    float distance = Vector2.Distance(new Vector2(pX, pY), new Vector2(x, y));

                    if (distance <= brushRadius)
                    {
                        // Pintamos la tierra al 100% de opacidad para que el sendero sea sólido
                        alphamaps[y, x, 0] = 1f;
                        alphamaps[y, x, 1] = 0f;
                        alphamaps[y, x, 2] = 0f;
                    }
                }
            }
        }
        tData.SetAlphamaps(0, 0, alphamaps);
    }

    private void ScatterTrees(Terrain terrain, int resolution)
    {
        int treesPlaced = 0;
        int attempts = 0;
        List<Vector3> placedTreePositions = new List<Vector3>();

        while (treesPlaced < numberOfTrees && attempts < numberOfTrees * 30)
        {
            attempts++;

            int gridX = Random.Range(10, resolution - 10);
            int gridY = Random.Range(10, resolution - 10);

            bool tooCloseToPath = false;
            for (int x = -pathClearance; x <= pathClearance; x++)
            {
                for (int y = -pathClearance; y <= pathClearance; y++)
                {
                    if (randomWalkGenerator.pathPositions.Contains(new Vector2Int(gridX + x, gridY + y)))
                    {
                        tooCloseToPath = true;
                        break;
                    }
                }
                if (tooCloseToPath) break;
            }
            if (tooCloseToPath) continue;

            float offsetX = Random.Range(-2f, 2f);
            float offsetZ = Random.Range(-2f, 2f);

            float worldX = ((gridX / (float)(resolution - 1)) * terrain.terrainData.size.x) + offsetX;
            float worldZ = ((gridY / (float)(resolution - 1)) * terrain.terrainData.size.z) + offsetZ;

            Vector3 worldPos = new Vector3(worldX, 0, worldZ) + terrain.transform.position;
            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

            // --- CORRECCIÓN: CONTROL DE POBLACIÓN EN LAS MONTAÑAS ---
            float normalizedHeight = (worldPos.y - terrain.transform.position.y) / terrain.terrainData.size.y;

            // 1. Límite absoluto: Prohibido plantar en cumbres extremas
            if (normalizedHeight > 0.85f) continue;

            // 2. Control de densidad: Si entra a la nieve, tiene un 80% de probabilidad de ser cancelado
            if (normalizedHeight >= snowThreshold)
            {
                if (Random.value > 0.2f) continue;
            }
            // --------------------------------------------------------

            bool tooCloseToAnotherTree = false;
            foreach (Vector3 existingTreePos in placedTreePositions)
            {
                if (Vector3.Distance(worldPos, existingTreePos) < minTreeDistance)
                {
                    tooCloseToAnotherTree = true;
                    break;
                }
            }
            if (tooCloseToAnotherTree) continue;

            GameObject newTree = Instantiate(treeGeneratorReference.gameObject, Vector3.zero, Quaternion.identity, this.transform);
            newTree.name = "Arbol_Procedural_" + treesPlaced;

            LSystemTreeGenerator lSystem = newTree.GetComponent<LSystemTreeGenerator>();
            if (lSystem != null)
            {
                lSystem.GenerateTree();
            }

            if (normalizedHeight >= snowThreshold)
            {
                Renderer[] renderers = newTree.GetComponentsInChildren<Renderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();

                float maxLocalY = 0f;
                foreach (Renderer r in renderers)
                {
                    if (r.transform.localPosition.y > maxLocalY)
                    {
                        maxLocalY = r.transform.localPosition.y;
                    }
                }

                foreach (Renderer r in renderers)
                {
                    if (r.transform.localPosition.y > maxLocalY * 0.35f)
                    {
                        r.GetPropertyBlock(block);
                        block.SetColor("_BaseColor", snowTreeColor);
                        block.SetColor("_Color", snowTreeColor);
                        r.SetPropertyBlock(block);
                    }
                }
            }

            newTree.transform.position = worldPos;
            spawnedTrees.Add(newTree);
            placedTreePositions.Add(worldPos);
            treesPlaced++;
        }
    }

    public void ClearWorld()
    {
        // 1. Limpiar los árboles de la lista
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
            {
                tree.transform.SetParent(null); // Lo desvinculamos para que no estorbe
                tree.name = "ToDestroy";
                if (Application.isPlaying) Destroy(tree);
                else DestroyImmediate(tree);
            }
        }
        spawnedTrees.Clear();

        // 2. Limpiar árboles residuales
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Arbol_Procedural") || child.name.Contains("Generated Tree"))
            {
                child.SetParent(null);
                child.name = "ToDestroy";
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        // 3. Desvincular el terreno viejo antes de eliminarlo
        if (terrainGenerator != null)
        {
            Terrain oldTerrain = terrainGenerator.GetComponentInChildren<Terrain>();
            if (oldTerrain != null)
            {
                oldTerrain.transform.SetParent(null);
                oldTerrain.name = "Terrain_ToDestroy";
            }

            terrainGenerator.DeleteTerrain();
        }
    }
}