using System.Collections.Generic;
using UnityEngine;

public class VolcanoManager : MonoBehaviour
{
    [Header("Conexión de Generadores")]
    [Tooltip("El generador de terreno accidentado (Diamond-Square).")]
    public TerrainGenerator terrainGenerator;

    [Tooltip("El tallador del sendero seguro.")]
    public RandomWalkGenerator randomWalkGenerator;

    [Tooltip("Generador de lagos de lava.")]
    public CellularAutomata lavaGenerator;

    [Tooltip("El cristal base de basalto que se clonará por el mapa.")]
    public LSystemTreeGenerator crystalGeneratorReference;

    [Header("Configuración del Volcán")]
    [Tooltip("Cantidad de formaciones cristalinas a esparcir en el terreno.")]
    public int numberOfCrystals = 40;

    [Header("Adaptación al Entorno (Magma)")]
    [Tooltip("Altura normalizada (0 a 1) por DEBAJO de la cual los cristales brillan por el calor de la lava.")]
    public float heatThreshold = 0.3f;

    [Tooltip("El color incandescente que tomarán los cristales en las zonas bajas.")]
    public Color magmaGlowColor = Color.red;

    [Tooltip("Distancia mínima para alejar los cristales del camino.")]
    public int pathClearance = 4;

    [Tooltip("Distancia mínima entre un cristal y otro.")]
    public float minCrystalDistance = 8f;

    // Listas para guardar las referencias y poder borrarlas al regenerar
    private List<GameObject> spawnedCrystals = new List<GameObject>();
    private List<GameObject> spawnedLava = new List<GameObject>();

    // =========================================================================
    // FUNCIÓN PRINCIPAL DE GENERACIÓN
    // =========================================================================
    public void GenerateWorld()
    {
        ClearWorld();
        terrainGenerator.GenerateTerrain();

        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();
        if (terrain == null) return;

        TerrainData tData = terrain.terrainData;
        int res = tData.heightmapResolution;
        float[,] heights = tData.GetHeights(0, 0, res, res);

        // 1. TÉCNICA CONSTRUCTIVA (Random Walk): Hundimos el relieve para el camino
        if (randomWalkGenerator != null)
        {
            randomWalkGenerator.CarvePath(heights);
        }

        tData.SetHeights(0, 0, heights);

        // 2. PINTURA DEL CAMINO
        if (randomWalkGenerator != null)
        {
            PaintPathOnTerrain(terrain);
        }

        // 3. LAGOS DE LAVA (Cellular Automata)
        if (lavaGenerator != null)
        {
            SpawnLavaPools(terrain, res);
        }

        // 4. GRAMÁTICAS (L-System): Esparcimos cristales
        if (crystalGeneratorReference != null)
        {
            ScatterCrystals(terrain, res);
        }
    }

    // =========================================================================
    // GENERACIÓN DE LAVA (EVITANDO EL CAMINO)
    // =========================================================================
    private void SpawnLavaPools(Terrain terrain, int resolution)
    {
        lavaGenerator.Generate(); // Ejecutamos el autómata lógico

        // Asumimos que lavaGenerator.width coincide con la resolución del terreno
        for (int x = 0; x < lavaGenerator.width; x++)
        {
            for (int y = 0; y < lavaGenerator.height; y++)
            {
                // Si el autómata decidió que es lava (roca en tu script original)
                // y NO estamos pisando el camino del RandomWalk
                if (lavaGenerator.IsRock(x, y) && !randomWalkGenerator.pathPositions.Contains(new Vector2Int(x, y)))
                {
                    float worldX = (x / (float)(resolution - 1)) * terrain.terrainData.size.x;
                    float worldZ = (y / (float)(resolution - 1)) * terrain.terrainData.size.z;
                    Vector3 worldPos = new Vector3(worldX, 0, worldZ) + terrain.transform.position;
                    worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

                    // Instanciamos el prefab de lava
                    if (lavaGenerator.rockPrefab != null)
                    {
                        GameObject lavaTile = Instantiate(lavaGenerator.rockPrefab, worldPos, Quaternion.identity, this.transform);
                        spawnedLava.Add(lavaTile);
                    }
                }
            }
        }
    }

    private void PaintPathOnTerrain(Terrain terrain)
    {
        // (Mismo código exacto que tenías en tu script original)
        TerrainData tData = terrain.terrainData;
        int alphaRes = tData.alphamapResolution;
        int heightRes = tData.heightmapResolution;
        float[,,] alphamaps = tData.GetAlphamaps(0, 0, alphaRes, alphaRes);
        int paintBrush = 2;

        foreach (Vector2Int pos in randomWalkGenerator.pathPositions)
        {
            int pX = Mathf.RoundToInt((pos.x / (float)(heightRes - 1)) * (alphaRes - 1));
            int pY = Mathf.RoundToInt((pos.y / (float)(heightRes - 1)) * (alphaRes - 1));

            for (int bY = -paintBrush; bY <= paintBrush; bY++)
            {
                for (int bX = -paintBrush; bX <= paintBrush; bX++)
                {
                    int finalX = pX + bX;
                    int finalY = pY + bY;

                    if (finalX >= 0 && finalX < alphaRes && finalY >= 0 && finalY < alphaRes)
                    {
                        if (Vector2.Distance(Vector2.zero, new Vector2(bX, bY)) <= paintBrush)
                        {
                            alphamaps[finalY, finalX, 0] = 1f; // Camino (Ceniza/Piedra)
                            alphamaps[finalY, finalX, 1] = 0f; // Terreno (Obsidiana)
                            alphamaps[finalY, finalX, 2] = 0f;
                        }
                    }
                }
            }
        }
        tData.SetAlphamaps(0, 0, alphamaps);
    }

    // =========================================================================
    // FUNCIÓN PARA ESPARCIR CRISTALES (L-SYSTEM)
    // =========================================================================
    private void ScatterCrystals(Terrain terrain, int resolution)
    {
        int crystalsPlaced = 0;
        int attempts = 0;
        List<Vector3> placedPositions = new List<Vector3>();

        while (crystalsPlaced < numberOfCrystals && attempts < numberOfCrystals * 30)
        {
            attempts++;
            int gridX = Random.Range(10, resolution - 10);
            int gridY = Random.Range(10, resolution - 10);

            // VALIDACIÓN 1: Distancia respecto al camino
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

            float worldX = (gridX / (float)(resolution - 1)) * terrain.terrainData.size.x;
            float worldZ = (gridY / (float)(resolution - 1)) * terrain.terrainData.size.z;
            Vector3 worldPos = new Vector3(worldX, 0, worldZ) + terrain.transform.position;
            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;

            // VALIDACIÓN 2: Distancia respecto a otros cristales
            bool tooClose = false;
            foreach (Vector3 existingPos in placedPositions)
            {
                if (Vector3.Distance(worldPos, existingPos) < minCrystalDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            GameObject newCrystal = Instantiate(crystalGeneratorReference.gameObject, Vector3.zero, Quaternion.identity, this.transform);
            newCrystal.name = "Cristal_Procedural_" + crystalsPlaced;

            LSystemTreeGenerator lSystem = newCrystal.GetComponent<LSystemTreeGenerator>();
            if (lSystem != null) lSystem.GenerateTree();

            // --- INVERSIÓN: PINTAR DE ROJO SI ESTÁ CERCA DE LA LAVA ---
            float normalizedHeight = (worldPos.y - terrain.transform.position.y) / terrain.terrainData.size.y;

            // Si está por debajo del umbral, se tiñe del color del magma
            if (normalizedHeight <= heatThreshold)
            {
                Renderer[] renderers = newCrystal.GetComponentsInChildren<Renderer>();
                MaterialPropertyBlock block = new MaterialPropertyBlock();

                foreach (Renderer r in renderers)
                {
                    r.GetPropertyBlock(block);
                    block.SetColor("_BaseColor", magmaGlowColor);
                    block.SetColor("_Color", magmaGlowColor);
                    r.SetPropertyBlock(block);
                }
            }

            newCrystal.transform.position = worldPos;
            spawnedCrystals.Add(newCrystal);
            placedPositions.Add(worldPos);
            crystalsPlaced++;
        }
    }

    public void ClearWorld()
    {
        foreach (GameObject crystal in spawnedCrystals)
        {
            if (crystal != null) DestroyImmediate(crystal);
        }
        spawnedCrystals.Clear();

        foreach (GameObject lava in spawnedLava)
        {
            if (lava != null) DestroyImmediate(lava);
        }
        spawnedLava.Clear();

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.Contains("Cristal_Procedural") || child.name.Contains("Generated Tree") || child.name.Contains("Lava"))
            {
                DestroyImmediate(child.gameObject);
            }
        }

        if (terrainGenerator != null) terrainGenerator.DeleteTerrain();
    }
}