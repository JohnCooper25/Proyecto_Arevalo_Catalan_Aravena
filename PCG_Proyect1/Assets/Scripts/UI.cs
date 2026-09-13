using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("Conexiones a los Terrenos")]
    public WorldManager managerMontana;
    public WorldManager managerVolcan;

    [Header("Cajas de Texto (Escribir números)")]
    public TMP_InputField inputSeed;
    public TMP_InputField inputPasosCamino;
    public TMP_InputField inputCantidadCaminos;

    [Header("Deslizadores (Sliders)")]
    public Slider sliderObjetos;
    public Slider sliderRugosidad;
    public Slider sliderIteracionesTerreno;

    private bool iniciando = true;

    void Start()
    {
        WorldManager activo = ObtenerMundoActivo();
        if (activo != null)
        {
            if (inputSeed != null)
                inputSeed.text = activo.terrainGenerator.seed.ToString();

            if (sliderObjetos != null)
                sliderObjetos.value = activo.numberOfTrees;

            if (sliderRugosidad != null && activo.terrainGenerator != null)
                sliderRugosidad.value = activo.terrainGenerator.diamondRoughness;

            if (sliderIteracionesTerreno != null && activo.terrainGenerator != null)
                sliderIteracionesTerreno.value = activo.terrainGenerator.diamondIterations;

            if (activo.randomWalkGenerator != null)
            {
                if (inputPasosCamino != null)
                    inputPasosCamino.text = activo.randomWalkGenerator.maxSteps.ToString();

                if (inputCantidadCaminos != null)
                    inputCantidadCaminos.text = activo.randomWalkGenerator.numberOfPaths.ToString();
            }
        }
        iniciando = false;
        GenerarMontana();
    }

    public void GenerarMontana()
    {
        if (managerVolcan != null) { managerVolcan.ClearWorld(); managerVolcan.gameObject.SetActive(false); }
        if (managerMontana != null) { managerMontana.gameObject.SetActive(true); AplicarParametros(managerMontana); managerMontana.GenerateWorld(); }
    }

    public void GenerarVolcan()
    {
        if (managerMontana != null) { managerMontana.ClearWorld(); managerMontana.gameObject.SetActive(false); }
        if (managerVolcan != null) { managerVolcan.gameObject.SetActive(true); AplicarParametros(managerVolcan); managerVolcan.GenerateWorld(); }
    }

    public void AlCambiarParametro()
    {
        if (iniciando) return;
        CancelInvoke("ReconstruirMundoActivo");
        Invoke("ReconstruirMundoActivo", 0.15f);
    }

    private void ReconstruirMundoActivo()
    {
        WorldManager activo = ObtenerMundoActivo();
        if (activo != null)
        {
            AplicarParametros(activo);
            activo.GenerateWorld();
        }
    }

    private WorldManager ObtenerMundoActivo()
    {
        if (managerVolcan != null && managerVolcan.gameObject.activeInHierarchy) return managerVolcan;
        return managerMontana; // Por defecto o si está activa la montaña
    }

    private void AplicarParametros(WorldManager manager)
    {
        // 1. Semilla
        if (inputSeed != null && !string.IsNullOrEmpty(inputSeed.text))
            if (int.TryParse(inputSeed.text, out int nuevaSemilla))
                manager.terrainGenerator.seed = nuevaSemilla;

        // 2. Parámetros del Camino leídos DESDE la UI hacia el generador
        if (manager.randomWalkGenerator != null)
        {
            if (inputPasosCamino != null && !string.IsNullOrEmpty(inputPasosCamino.text))
            {
                if (int.TryParse(inputPasosCamino.text, out int pasos))
                    manager.randomWalkGenerator.maxSteps = pasos;
            }

            if (inputCantidadCaminos != null && !string.IsNullOrEmpty(inputCantidadCaminos.text))
            {
                if (int.TryParse(inputCantidadCaminos.text, out int caminos))
                    manager.randomWalkGenerator.numberOfPaths = caminos;
            }
        }

        // 3. Otros (Árboles y Terreno)
        if (sliderObjetos != null)
            manager.numberOfTrees = (int)sliderObjetos.value;

        if (manager.terrainGenerator != null)
        {
            if (sliderRugosidad != null)
                manager.terrainGenerator.diamondRoughness = sliderRugosidad.value;

            if (sliderIteracionesTerreno != null)
                manager.terrainGenerator.diamondIterations = (int)sliderIteracionesTerreno.value;
        }
    }
}