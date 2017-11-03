using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DataResetter))]
public class DataResetterEditor : Editor
{
    private DataResetter dataResetter;              // Reference to the target of this Editor.
    private SerializedProperty resettersProperty;   // Represents the only field in the target.


    private const float buttonWidth = 30f;          // Width in pixels of the add and remove buttons
    private const string dataResetterPropResettableScriptableObjectsName = "resettableScriptableObjects";
                                                    // The name of the field to be represented.


    private void OnEnable ()
    {
        // Cache the property and target.
        resettersProperty = serializedObject.FindProperty(dataResetterPropResettableScriptableObjectsName);

        dataResetter = (DataResetter)target;

        // If the array is null, initialise it to prevent null refs.
        if (dataResetter.resettableScriptableObjects == null)
        {
            dataResetter.resettableScriptableObjects = new ResettableScriptableObject[0];
        }
    }


    public override void OnInspectorGUI ()
    {
        // Update the state of the serializedObject to the current values of the target.
        serializedObject.Update();

        // Go through all the resetters and create GUI appropriate for them.
        for (int i = 0; i < resettersProperty.arraySize; i++)
        {
            SerializedProperty resettableProperty = resettersProperty.GetArrayElementAtIndex (i);

            EditorGUILayout.PropertyField (resettableProperty);
        }

        EditorGUILayout.BeginHorizontal ();

        // Create a button with a '+' and if it's clicked, add an element to the end of the array.
        if (GUILayout.Button ("+", GUILayout.Width (buttonWidth)))
        {
            resettersProperty.InsertArrayElementAtIndex (resettersProperty.arraySize);
        }

        // Create a button with a '-' and if it's clicked remove the last element of the array.
        // Note that if the last element is not null calling DeleteArrayElementAtIndex will make it null.
        if (GUILayout.Button("-", GUILayout.Width(buttonWidth)))
        {
            if (resettersProperty.GetArrayElementAtIndex(resettersProperty.arraySize - 1).objectReferenceValue)
                resettersProperty.DeleteArrayElementAtIndex(resettersProperty.arraySize - 1);
            resettersProperty.DeleteArrayElementAtIndex(resettersProperty.arraySize - 1);
        }

        EditorGUILayout.EndHorizontal ();

        // Push the values from the serializedObject back to the target.
        serializedObject.ApplyModifiedProperties();
    }
}
