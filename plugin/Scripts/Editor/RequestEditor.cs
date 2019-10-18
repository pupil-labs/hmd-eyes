using UnityEngine;
using UnityEditor;

using PupilLabs;

[CustomEditor(typeof(RequestController))]
public class RequestEditor : Editor
{
    private SerializedProperty ipProp;
    private SerializedProperty portProp;
    private SerializedProperty versionProp;
    private SerializedProperty isConnectingProb;

    public void OnEnable()
    {
        SerializedProperty requestProp = serializedObject.FindProperty("request");
        ipProp = requestProp.FindPropertyRelative("IP");
        portProp = requestProp.FindPropertyRelative("PORT");

        isConnectingProb = serializedObject.FindProperty("isConnecting");
        versionProp = serializedObject.FindProperty("PupilVersion");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        RequestController ctrl = serializedObject.targetObject as RequestController;
     
        DrawDefaultInspector();

        // request
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Connection", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ipProp,new GUIContent("IP"));
        EditorGUILayout.PropertyField(portProp,new GUIContent("PORT"));
        EditorGUILayout.LabelField("Pupil Version",versionProp.stringValue);

        GUILayout.BeginHorizontal();
        
        string connectLabel = "Connect";
        GUI.enabled = !ctrl.IsConnected && Application.isPlaying;
        if (isConnectingProb.boolValue)
        {
            connectLabel = "Connecting ...";
            GUI.enabled = false;
        }
        if (GUILayout.Button(connectLabel))
        {
            ctrl.RunConnect();
        }

        GUI.enabled = ctrl.IsConnected;
        if (GUILayout.Button("Disconnect"))
        {
            ctrl.Disconnect();
        }

        GUI.enabled = true;
        GUILayout.EndHorizontal();
        
        serializedObject.ApplyModifiedProperties();
    }
}
