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

    public float snowThreshold = 0.7f; // Pon el mismo valor que tiene el High Threshold de tu TerrainGenerator



    [Tooltip("El color que tomarán los árboles en la cima.")]

    public Color snowTreeColor = Color.white;



    [Tooltip("Distancia mínima (en coordenadas de grilla matemática) para alejar los árboles del camino.")]

    public int pathClearance = 4;



    [Tooltip("Distancia mínima (en metros 3D de Unity) entre un árbol y otro para evitar que se superpongan.")]

    public float minTreeDistance = 8f;



    // Lista para guardar las referencias de los árboles instanciados y poder borrarlos al regenerar

    private List<GameObject> spawnedTrees = new List<GameObject>();



    // =========================================================================

    // FUNCIÓN PRINCIPAL DE GENERACIÓN

    // =========================================================================

    public void GenerateWorld()

    {

        // 1. LIMPIEZA INICIAL: Borramos árboles viejos y el terreno anterior

        ClearWorld();



        // 2. TERRENO FRACTAL: Llamamos a Diamond-Square para hacer las montañas

        terrainGenerator.GenerateTerrain();



        Terrain terrain = terrainGenerator.GetComponentInChildren<Terrain>();

        if (terrain == null)

        {

            Debug.LogError("No se encontró el Terreno generado.");

            return;

        }



        // Extraemos la matriz matemática de alturas del terreno

        TerrainData tData = terrain.terrainData;

        int res = tData.heightmapResolution;

        float[,] heights = tData.GetHeights(0, 0, res, res);



        // 3. TÉCNICA CONSTRUCTIVA (Random Walk): Hundimos el relieve para el camino

        if (randomWalkGenerator != null)

        {

            randomWalkGenerator.CarvePath(heights);

        }



        // Devolvemos la matriz modificada al objeto Terrain de Unity

        tData.SetHeights(0, 0, heights);



        // 4. PINTURA DEL CAMINO: Coloreamos el sendero que trazó el Random Walk

        if (randomWalkGenerator != null)

        {

            PaintPathOnTerrain(terrain);

        }



        // 5. GRAMÁTICAS (L-System): Esparcimos la vegetación validando colisiones

        if (treeGeneratorReference != null)

        {

            ScatterTrees(terrain, res);

        }

    }



    // =========================================================================

    // FUNCIÓN PARA PINTAR EL SENDERO (LÍNEA CONTINUA Y BORDES SUAVES)

    // =========================================================================

    private void PaintPathOnTerrain(Terrain terrain)
    {
        TerrainData tData = terrain.terrainData;
        int alphaRes = tData.alphamapResolution;
        int heightRes = tData.heightmapResolution;
        float[,,] alphamaps = tData.GetAlphamaps(0, 0, alphaRes, alphaRes);

        int paintBrush = 2;
        // Calculamos el radio de la brocha circular basándonos en la diferencia de resolución
        float brushRadius = Mathf.CeilToInt((float)alphaRes / heightRes) * 1.5f;

        foreach (Vector2Int pos in randomWalkGenerator.pathPositions)
        {
            // Convertimos la coordenada del mapa de alturas al mapa de texturas
            int pX = Mathf.RoundToInt((pos.x / (float)(heightRes - 1)) * (alphaRes - 1));
            int pY = Mathf.RoundToInt((pos.y / (float)(heightRes - 1)) * (alphaRes - 1));

            // Definimos el área cuadrada máxima a evaluar
            int startX = Mathf.Max(0, pX - Mathf.CeilToInt(brushRadius));
            int endX = Mathf.Min(alphaRes - 1, pX + Mathf.CeilToInt(brushRadius));
            int startY = Mathf.Max(0, pY - Mathf.CeilToInt(brushRadius));
            int endY = Mathf.Min(alphaRes - 1, pY + Mathf.CeilToInt(brushRadius));

            for (int y = startY; y <= endY; y++)
            {
                for (int x = startX; x <= endX; x++)
                {
                    // LA MAGIA: Calculamos la distancia circular desde el centro de la brocha
                    float distance = Vector2.Distance(new Vector2(pX, pY), new Vector2(x, y));

                    // Solo pintamos si el píxel cae dentro del círculo perfecto
                    if (distance <= brushRadius)
                    {
                        // Asigna todo el peso al color de la lava (Capa 0) y borra la roca/nieve
                        alphamaps[y, x, 0] = 1f;
                        alphamaps[y, x, 1] = 0f;
                        alphamaps[y, x, 2] = 0f;
                    }
                }
            }
        }

        tData.SetAlphamaps(0, 0, alphamaps);
    }


    // =========================================================================

    // FUNCIÓN PARA ESPARCIR ÁRBOLES

    // =========================================================================

    private void ScatterTrees(Terrain terrain, int resolution)

    {

        int treesPlaced = 0;

        int attempts = 0;



        // Lista temporal para registrar dónde pusimos árboles y medir distancias

        List<Vector3> placedTreePositions = new List<Vector3>();



        // Intentamos plantar hasta alcanzar el número deseado, con límite de intentos

        while (treesPlaced < numberOfTrees && attempts < numberOfTrees * 30)

        {

            attempts++;



            // Elegimos una coordenada aleatoria, dejando un margen en los bordes del mapa

            int gridX = Random.Range(10, resolution - 10);

            int gridY = Random.Range(10, resolution - 10);



            // VALIDACIÓN 1: Distancia respecto al camino

            bool tooCloseToPath = false;

            // Revisamos un cuadrante alrededor del punto. Si el camino pasa por ahí, cancelamos.

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

            if (tooCloseToPath) continue; // Descartamos la posición y volvemos a intentar



            // Convertimos las coordenadas matemáticas a posiciones 3D reales de Unity

            float worldX = (gridX / (float)(resolution - 1)) * terrain.terrainData.size.x;

            float worldZ = (gridY / (float)(resolution - 1)) * terrain.terrainData.size.z;



            Vector3 worldPos = new Vector3(worldX, 0, worldZ) + terrain.transform.position;

            // Medimos la altura exacta de la montaña en esa coordenada para que el árbol toque el suelo

            worldPos.y = terrain.SampleHeight(worldPos) + terrain.transform.position.y;



            // VALIDACIÓN 2: Distancia respecto a otros árboles

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



            // TRUCO DE TELETRANSPORTACIÓN (Para evitar el doble offset)

            // 1. Instanciamos el árbol en el origen absoluto (0,0,0)

            GameObject newTree = Instantiate(treeGeneratorReference.gameObject, Vector3.zero, Quaternion.identity, this.transform);

            newTree.name = "Arbol_Procedural_" + treesPlaced;



            LSystemTreeGenerator lSystem = newTree.GetComponent<LSystemTreeGenerator>();

            if (lSystem != null)

            {

                // 2. Generamos el L-System (como está en el origen, no hay errores matemáticos)

                lSystem.GenerateTree();

            }



            // --- NUEVO: PINTAR DE BLANCO SI ESTÁ EN LA NIEVE ---

            // Calculamos qué tan alto está el árbol respecto al máximo del terreno (0 a 1)

            float normalizedHeight = (worldPos.y - terrain.transform.position.y) / terrain.terrainData.size.y;



            if (normalizedHeight >= snowThreshold)

            {

                // Buscamos todas las ramas que el L-System acaba de crear

                Renderer[] renderers = newTree.GetComponentsInChildren<Renderer>();

                MaterialPropertyBlock block = new MaterialPropertyBlock();



                foreach (Renderer r in renderers)

                {

                    // Sobrescribimos el color nativo del árbol por el color de la nieve

                    r.GetPropertyBlock(block);

                    block.SetColor("_BaseColor", snowTreeColor);

                    block.SetColor("_Color", snowTreeColor);

                    r.SetPropertyBlock(block);

                }

            }

            // ----------------------------------------------------



            // 3. Lo movemos a su posición final definitiva en el terreno

            newTree.transform.position = worldPos;



            // Registramos el árbol

            spawnedTrees.Add(newTree);

            placedTreePositions.Add(worldPos);

            treesPlaced++;

        }

    }



    // =========================================================================

    // FUNCIÓN DE LIMPIEZA

    // =========================================================================

    public void ClearWorld()
    {
        // 1. Borramos todos los árboles de la lista registrada
        foreach (GameObject tree in spawnedTrees)
        {
            if (tree != null)
            {
                if (Application.isPlaying) Destroy(tree);
                else DestroyImmediate(tree);
            }
        }
        spawnedTrees.Clear();

        // 2. BARRIDO DE SEGURIDAD: Eliminamos cualquier residuo que haya quedado colgado en este transform
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            // Si el objeto hijo se llama como los árboles generados o contiene "Arbol", lo borramos
            if (child.name.Contains("Arbol_Procedural") || child.name.Contains("Generated Tree"))
            {
                if (Application.isPlaying) Destroy(child.gameObject);
                else DestroyImmediate(child.gameObject);
            }
        }

        // 3. Borramos el terreno anterior
        if (terrainGenerator != null)
        {
            terrainGenerator.DeleteTerrain();
        }
    }

}