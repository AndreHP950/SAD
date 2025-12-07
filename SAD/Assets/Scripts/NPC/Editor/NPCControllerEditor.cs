using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NPCController))]
public class NPCControllerEditor : Editor
{
    private NPCController npc;
    private bool editingWaypoints = false;

    void OnEnable()
    {
        npc = (NPCController)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Ferramentas de Waypoint", EditorStyles.boldLabel);

        // Botão para adicionar waypoint na posição atual
        if (GUILayout.Button("Adicionar Waypoint na Posição Atual"))
        {
            Undo.RecordObject(npc, "Add Waypoint");
            
            NPCController.Waypoint wp = new NPCController.Waypoint();
            
            if (npc.useWorldPositions)
            {
                wp.position = npc.transform.position;
            }
            else
            {
                wp.position = Vector3.zero;
            }
            
            npc.waypoints.Add(wp);
            EditorUtility.SetDirty(npc);
        }

        // Botão para limpar todos os waypoints
        if (npc.waypoints.Count > 0)
        {
            EditorGUILayout.Space(5);
            GUI.color = Color.red;
            if (GUILayout.Button("Limpar Todos os Waypoints"))
            {
                if (EditorUtility.DisplayDialog("Confirmar", "Deseja remover todos os waypoints?", "Sim", "Não"))
                {
                    Undo.RecordObject(npc, "Clear Waypoints");
                    npc.waypoints.Clear();
                    EditorUtility.SetDirty(npc);
                }
            }
            GUI.color = Color.white;
        }

        // Toggle para modo de edição de waypoints
        EditorGUILayout.Space(5);
        editingWaypoints = GUILayout.Toggle(editingWaypoints, "Modo de Edição (Shift+Click para adicionar)", "Button");

        if (editingWaypoints)
        {
            EditorGUILayout.HelpBox("Segure SHIFT e clique na Scene View para adicionar waypoints.", MessageType.Info);
        }

        // Lista editável de waypoints
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"Waypoints ({npc.waypoints.Count})", EditorStyles.boldLabel);

        for (int i = 0; i < npc.waypoints.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            EditorGUILayout.LabelField($"WP {i}", GUILayout.Width(40));
            
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = EditorGUILayout.Vector3Field("", npc.waypoints[i].position);
            float newWait = EditorGUILayout.FloatField(npc.waypoints[i].waitTime, GUILayout.Width(50));
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npc, "Edit Waypoint");
                npc.waypoints[i].position = newPos;
                npc.waypoints[i].waitTime = newWait;
                EditorUtility.SetDirty(npc);
            }

            // Botão para mover o NPC para este waypoint (preview)
            if (GUILayout.Button("Ir", GUILayout.Width(30)))
            {
                Vector3 targetPos = npc.useWorldPositions ? npc.waypoints[i].position : npc.transform.position + npc.waypoints[i].position;
                npc.transform.position = targetPos;
                SceneView.RepaintAll();
            }

            // Botão para remover
            GUI.color = Color.red;
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                Undo.RecordObject(npc, "Remove Waypoint");
                npc.waypoints.RemoveAt(i);
                EditorUtility.SetDirty(npc);
                break;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();
        }
    }

    void OnSceneGUI()
    {
        if (npc == null) return;

        // Handles para mover waypoints na cena
        Vector3 basePos = Application.isPlaying ? npc.transform.position : npc.transform.position;
        
        for (int i = 0; i < npc.waypoints.Count; i++)
        {
            Vector3 worldPos = npc.useWorldPositions ? npc.waypoints[i].position : basePos + npc.waypoints[i].position;

            EditorGUI.BeginChangeCheck();
            Vector3 newWorldPos = Handles.PositionHandle(worldPos, Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(npc, "Move Waypoint");
                
                if (npc.useWorldPositions)
                {
                    npc.waypoints[i].position = newWorldPos;
                }
                else
                {
                    npc.waypoints[i].position = newWorldPos - basePos;
                }
                
                EditorUtility.SetDirty(npc);
            }

            // Label do waypoint
            Handles.Label(worldPos + Vector3.up * 0.7f, $"WP {i}", EditorStyles.boldLabel);
        }

        // Modo de edição: Shift+Click para adicionar waypoint
        if (editingWaypoints)
        {
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
            
            Event e = Event.current;
            if (e.type == EventType.MouseDown && e.button == 0 && e.shift)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    Undo.RecordObject(npc, "Add Waypoint");
                    
                    NPCController.Waypoint wp = new NPCController.Waypoint();
                    
                    if (npc.useWorldPositions)
                    {
                        wp.position = hit.point;
                    }
                    else
                    {
                        wp.position = hit.point - npc.transform.position;
                    }
                    
                    npc.waypoints.Add(wp);
                    EditorUtility.SetDirty(npc);
                    
                    e.Use();
                }
            }
        }
    }
}