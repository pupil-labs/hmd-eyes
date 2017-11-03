using System;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SaveData))]
public class SaveDataEditor : Editor
{
    private SaveData saveData;                          // Reference to the target.
    private Action<bool> boolSpecificGUI;               // Delegate for the GUI that represents bool values.
    private Action<int> intSpecificGUI;                 // Delegate for the GUI that represents int values.
    private Action<string> stringSpecificGUI;           // Delegate for the GUI that represents string values.
    private Action<Vector3> vector3SpecificGUI;         // Delegate for the GUI that represents Vector3 values.
    private Action<Quaternion> quaternionSpecificGUI;   // Delegate for the GUI that represents Quaternion values.


    private void OnEnable ()
    {
        // Cache the reference to the target.
        saveData = (SaveData)target;

        // Set the values of the delegates to various 'read-only' GUI functions.
        boolSpecificGUI = value => { EditorGUILayout.Toggle(value); };
        intSpecificGUI = value => { EditorGUILayout.LabelField(value.ToString()); };
        stringSpecificGUI = value => { EditorGUILayout.LabelField (value); };
        vector3SpecificGUI = value => { EditorGUILayout.Vector3Field (GUIContent.none, value); };
        quaternionSpecificGUI = value => { EditorGUILayout.Vector3Field (GUIContent.none, value.eulerAngles); };
    }


    public override void OnInspectorGUI ()
    {
        // Display all the values for each data type.
        KeyValuePairListsGUI ("Bools", saveData.boolKeyValuePairLists, boolSpecificGUI);
        KeyValuePairListsGUI ("Integers", saveData.intKeyValuePairLists, intSpecificGUI);
        KeyValuePairListsGUI ("Strings", saveData.stringKeyValuePairLists, stringSpecificGUI);
        KeyValuePairListsGUI ("Vector3s", saveData.vector3KeyValuePairLists, vector3SpecificGUI);
        KeyValuePairListsGUI ("Quaternions", saveData.quaternionKeyValuePairLists, quaternionSpecificGUI);
    }


    private void KeyValuePairListsGUI<T> (string label, SaveData.KeyValuePairLists<T> keyvaluePairList, Action<T> specificGUI)
    {
        // Surround each data type in a box.
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;

        // Display a label for this data type.
        EditorGUILayout.LabelField (label);

        // If there are data elements...
        if (keyvaluePairList.keys.Count > 0)
        {
            // ... go through each of them...
            for (int i = 0; i < keyvaluePairList.keys.Count; i++)
            {
                EditorGUILayout.BeginHorizontal ();

                // ... and display a label for each followed by GUI specific to their type.
                EditorGUILayout.LabelField (keyvaluePairList.keys[i]);
                specificGUI (keyvaluePairList.values[i]);

                EditorGUILayout.EndHorizontal ();
            }
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();
    }
}
