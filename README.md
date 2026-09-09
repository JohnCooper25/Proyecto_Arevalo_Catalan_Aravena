# Proyecto Unidad 1 Generación de paisajes

## Descripción breve del proyecto
Este proyecto es un desarrollado en Unity 3D, integra tres técnicas de Generación Procedural de Contenido para crear un entorno natural dinámico. El sistema genera un terreno montañoso con Diamond-Square, talla y pinta un sendero continuo a través de Random Walk, y distribuye vegetación tridimensional creada con L-System que evita bloquear el camino y reacciona a la altura del terreno (cubriéndose de nieve en las cimas).

## Integrantes
* Fabián Arévalo
* Jonathan Catalán
* Rafael Aravena

## Técnicas PCG utilizadas
* **Técnica de Fractal (Diamond-Square):** Utilizada para generar el mapa de alturas del terreno en 3D, creando montañas y valles mediante subdivisiones sucesivas y perturbación aleatoria.
* **Técnica Constructiva (Random Walk):** Un agente estocástico modificado (sin giros en U) recorre la grilla matemática del terreno hundiendo sutilmente la altura y aplicando una textura sólida para crear un sendero continuo.
* **Gramáticas (L-Systems):** Utilizado mediante Turtle Graphics en 3D para generar árboles procedimentales. Incluye validación de colisiones para evitar que se generen sobre el sendero o muy cerca unos de otros.

## Instrucciones básicas de ejecución
1. Clonar el repositorio y abrir el proyecto en Unity.
2. Abrir la escena principal del proyecto.
3. En la jerarquía, seleccionar el GameObject llamado `GeneradorDeMundo`.
4. En el Inspector, ubicar el script `WorldManager`.
5. Presionar el botón "Generar Mundo Completo" para ejecutar todas las técnicas en cadena.
6. Presionar el botón "Limpiar Mundo" antes de modificar parámetros estructurales para volver a generar.

## Principales parámetros configurables
El sistema es altamente flexible desde el Inspector del "WorldManager" y sus scripts conectados:
* TerrainGenerator: `Roughness` (escarpadura de las montañas), `Low/Middle/High Color` (colores del bioma), `Low Threshold` (límite del sendero).
* RandomWalkGenerator: `Max Steps` (longitud del camino), `Path Width` (grosor del sendero).
* WorldManager: `Number of Trees` (densidad del bosque), `Path Clearance` (distancia de los árboles al sendero), `Min Tree Distance` (separación entre árboles), `Snow Threshold` y `Snow Tree Color` (adaptación climática de la vegetación en altura).


