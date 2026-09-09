using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WorldManager))]
public class WorldManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        WorldManager manager = (WorldManager)target;

        EditorGUILayout.Space(15);

        if (GUILayout.Button("Generar Mundo Completo", GUILayout.Height(40)))
        {
            manager.GenerateWorld();
            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Limpiar Mundo", GUILayout.Height(25)))
        {
            manager.ClearWorld();
            EditorUtility.SetDirty(manager);
        }
    }
}