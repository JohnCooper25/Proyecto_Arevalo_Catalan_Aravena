using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[Serializable]
public class LSystemRule
{
    [Tooltip("Símbolo que será reemplazado durante la expansión.")]
    public string predecessor = "A";

    [Tooltip("Cadena que reemplazará al símbolo.")]
    public string successor = "AB";
}


public class ParallelGrammarGenerator : MonoBehaviour
{
    [Header("Parallel Grammar")]

    [SerializeField]
    private bool autoUpdate = true;


    [Tooltip("Cadena inicial de la gramática.")]
    [SerializeField]
    private string axiom = "A";


    [Tooltip(
        "Reglas de producción utilizadas durante la expansión.\n\n" +
        "Configuración base:\n" +
        "A -> AB\n" +
        "B -> A"
    )]
    [SerializeField]
    private List<LSystemRule> rules =
        new List<LSystemRule>()
        {
            new LSystemRule()
            {
                predecessor = "A",
                successor = "AB"
            },

            new LSystemRule()
            {
                predecessor = "B",
                successor = "A"
            }
        };


    [Tooltip("Cantidad de iteraciones de expansión.")]
    [Range(0, 10)]
    [SerializeField]
    private int iterations = 4;


    [SerializeField]
    private bool showDerivation = true;


    // -------------------------------------------------------------------------
    // GENERACIÓN DE LA EXPANSIÓN
    // -------------------------------------------------------------------------
    //
    // Una gramática define reglas que permiten reemplazar símbolos por nuevas
    // cadenas.
    //
    // Para la configuración inicial del laboratorio:
    //
    //      Axiom: A
    //
    //      A -> AB
    //      B -> A
    //
    // la expansión esperada es:
    //
    //      Iteración 0: A
    //      Iteración 1: AB
    //      Iteración 2: ABA
    //      Iteración 3: ABAAB
    //      Iteración 4: ABAABABA
    //
    //
    // -------------------------------------------------------------------------
    // REESCRITURA PARALELA
    // -------------------------------------------------------------------------
    //
    // La característica importante de esta primera implementación es que las
    // reglas se aplican de manera PARALELA.
    //
    // Esto significa que todos los símbolos de una iteración son evaluados
    // utilizando la misma cadena de origen.
    //
    // Por ejemplo:
    //
    //      cadena actual:
    //
    //          ABA
    //
    //      reglas:
    //
    //          A -> AB
    //          B -> A
    //
    // Durante esa iteración se evalúa:
    //
    //          A      B      A
    //          |      |      |
    //          AB     A      AB
    //
    // y solamente después de procesar toda la cadena se obtiene:
    //
    //          ABAAB
    //
    // No se debe utilizar el resultado parcial de una sustitución para decidir
    // las sustituciones restantes de la misma iteración.
    //
    //
    // -------------------------------------------------------------------------
    // SÍMBOLOS SIN REGLA
    // -------------------------------------------------------------------------
    //
    // No todos los símbolos necesitan poseer una regla de producción.
    //
    // Si un símbolo no posee una regla asociada, debe conservarse sin cambios.
    //
    // Esto será especialmente importante posteriormente en los L-Systems,
    // donde símbolos como:
    //
    //      +  -  [  ]  &  ^  \  /
    //
    // pueden formar parte de la cadena sin ser necesariamente reemplazados.
    //
    //
    // -------------------------------------------------------------------------
    // DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Además de obtener la cadena final, se almacenará el resultado de cada
    // iteración.
    //
    // Esto permite observar en la Console cómo evoluciona la gramática y
    // verificar que las reglas están siendo aplicadas correctamente.
    //

    public static string Generate(
     string axiom,
     List<LSystemRule> rules,
     int iterations,
     List<string> derivation = null)
    {
        // 1. Comenzar desde el axioma
        string currentString = axiom;

        // Registrar el estado inicial si se solicitó el historial de derivación
        if (derivation != null)
        {
            derivation.Add(currentString);
        }

        // 2. Aplicar las reglas durante la cantidad indicada de iteraciones
        for (int i = 0; i < iterations; i++)
        {
            // Usamos StringBuilder por rendimiento, ya que los L-systems crecen exponencialmente
            StringBuilder nextString = new StringBuilder();

            // 3. Evaluar todos los símbolos de cada iteración
            foreach (char symbol in currentString)
            {
                bool ruleApplied = false;

                // 4. Buscar si existe una regla para este símbolo
                foreach (LSystemRule rule in rules)
                {
                    // Convertimos el char a string para que coincida con el tipo de 'predecessor'
                    if (rule.predecessor == symbol.ToString())
                    {
                        nextString.Append(rule.successor);
                        ruleApplied = true;
                        break; // Regla encontrada y aplicada, pasamos al siguiente símbolo
                    }
                }

                // 5. Mantener sin cambios los símbolos que no posean una regla
                if (!ruleApplied)
                {
                    nextString.Append(symbol);
                }
            }

            // Reemplazar la cadena actual con la nueva generación
            currentString = nextString.ToString();

            // 6. Registrar el resultado de esta iteración
            if (derivation != null)
            {
                derivation.Add(currentString);
            }
        }

        // 7. Retornar la cadena obtenida al finalizar
        return currentString;
    }


    // -------------------------------------------------------------------------
    // EJECUCIÓN DESDE EL INSPECTOR
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //
    // Su función es utilizar los parámetros configurados en el Inspector,
    // ejecutar Generate() y mostrar posteriormente la derivación.
    //

    public void GenerateExpansion()
    {
        List<string> derivation =
            new List<string>();


        string finalSequence =
            Generate(
                axiom,
                rules,
                iterations,
                derivation
            );


        if (showDerivation)
        {
            PrintDerivation(
                derivation,
                finalSequence
            );
        }
    }


    // -------------------------------------------------------------------------
    // VISUALIZACIÓN DE LA DERIVACIÓN
    // -------------------------------------------------------------------------
    //
    // Esta sección se entrega implementada.
    //

    private void PrintDerivation(
        List<string> derivation,
        string finalSequence)
    {
        StringBuilder output =
            new StringBuilder();


        output.AppendLine(
            "===== PARALLEL GRAMMAR ====="
        );


        output.AppendLine();


        output.AppendLine(
            "AXIOM:"
        );


        output.AppendLine(
            axiom
        );


        output.AppendLine();


        output.AppendLine(
            "RULES:"
        );


        if (rules == null ||
            rules.Count == 0)
        {
            output.AppendLine(
                "(sin reglas)"
            );
        }
        else
        {
            foreach (LSystemRule rule in rules)
            {
                if (rule == null)
                {
                    continue;
                }


                output.AppendLine(
                    rule.predecessor +
                    " -> " +
                    rule.successor
                );
            }
        }


        output.AppendLine();


        output.AppendLine(
            "DERIVATION:"
        );


        for (int i = 0;
             i < derivation.Count;
             i++)
        {
            output.AppendLine(
                "Iteration " +
                i +
                ": " +
                derivation[i]
            );
        }


        output.AppendLine();


        output.AppendLine(
            "FINAL STRING:"
        );


        output.AppendLine(
            finalSequence
        );


        Debug.Log(
            output.ToString(),
            this
        );
    }
}