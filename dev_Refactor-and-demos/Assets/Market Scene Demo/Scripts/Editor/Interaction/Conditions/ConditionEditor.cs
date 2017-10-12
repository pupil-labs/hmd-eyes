using UnityEngine;
using UnityEditor;

// This class controls all the GUI for Conditions
// in all the places they are found.
[CustomEditor(typeof(Condition))]
public class ConditionEditor : Editor
{
    // This enum is used to represent where the Condition is being seen in the inspector.
    // ConditionAsset is for when a single Condition asset is selected as a child of the AllConditions asset.
    // AllConditionAsset is when the AllConditions asset is selected and this is a nested Editor.
    // ConditionCollection is when an Interactable is selected and this is a nested Editor within a ConditionCollection.
    public enum EditorType
    {
        ConditionAsset, AllConditionAsset, ConditionCollection
    }


    public EditorType editorType;                       // The type of this Editor.
    public SerializedProperty conditionsProperty;       // The SerializedProperty representing an array of Conditions on a ConditionCollection.


    private SerializedProperty descriptionProperty;     // Represents a string description of this Editor's target.
    private SerializedProperty satisfiedProperty;       // Represents a bool of whether this Editor's target is satisfied.
    private SerializedProperty hashProperty;            // Represents the number that identified this Editor's target.
    private Condition condition;                        // Reference to the target.


    private const float conditionButtonWidth = 30f;                     // Width in pixels of the button to remove this Condition from it's array.
    private const float toggleOffset = 30f;                             // Offset to line up the satisfied toggle with its label.
    private const string conditionPropDescriptionName = "description";  // Name of the field that represents the description.
    private const string conditionPropSatisfiedName = "satisfied";      // Name of the field that represents whether or not the Condition is satisfied.
    private const string conditionPropHashName = "hash";                // Name of the field that represents the Condition's identifier.
    private const string blankDescription = "No conditions set.";       // Description to use in case no Conditions have been created yet.


    private void OnEnable()
    {
        // Cache the target.
        condition = (Condition)target;

        // If this Editor has persisted through the destruction of it's target then destroy it.
        if (target == null)
        {
            DestroyImmediate(this);
            return;
        }

        // Cache the SerializedProperties.
        descriptionProperty = serializedObject.FindProperty(conditionPropDescriptionName);
        satisfiedProperty = serializedObject.FindProperty(conditionPropSatisfiedName);
        hashProperty = serializedObject.FindProperty(conditionPropHashName);
    }


    public override void OnInspectorGUI()
    {
        // Call different GUI depending where the Condition is.
        switch (editorType)
        {
            case EditorType.AllConditionAsset:
                AllConditionsAssetGUI();
                break;
            case EditorType.ConditionAsset:
                ConditionAssetGUI();
                break;
            case EditorType.ConditionCollection:
                InteractableGUI();
                break;
            default:
                throw new UnityException("Unknown ConditionEditor.EditorType.");
        }
    }


    // This is displayed for each Condition when the AllConditions asset is selected.
    private void AllConditionsAssetGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUI.indentLevel++;

        // Display the description of the Condition.
        EditorGUILayout.LabelField(condition.description);

        // Display a button showing a '-' that if clicked removes this Condition from the AllConditions asset.
        if (GUILayout.Button("-", GUILayout.Width(conditionButtonWidth)))
            AllConditionsEditor.RemoveCondition(condition);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndHorizontal();
    }


    // This is displayed when a single Condition asset is selected as a child of the AllConditions asset.
    private void ConditionAssetGUI()
    {
        EditorGUILayout.BeginHorizontal(GUI.skin.box);
        EditorGUI.indentLevel++;

        // Display the description of the Condition.
        EditorGUILayout.LabelField(condition.description);

        EditorGUI.indentLevel--;
        EditorGUILayout.EndHorizontal();
    }


    private void InteractableGUI()
    {
        // Pull the information from the target into the serializedObject.
        serializedObject.Update();

        // The width for the Popup, Toggle and remove Button.
        float width = EditorGUIUtility.currentViewWidth / 3f;

        EditorGUILayout.BeginHorizontal();

        // Find the index for the target based on the AllConditions array.
        int conditionIndex = AllConditionsEditor.TryGetConditionIndex(condition);

        // If the target can't be found in the AllConditions array use the first condition.
        if (conditionIndex == -1)
            conditionIndex = 0;

        // Set the index based on the user selection of the condition by the user.
        conditionIndex = EditorGUILayout.Popup(conditionIndex, AllConditionsEditor.AllConditionDescriptions,
            GUILayout.Width(width));

        // Find the equivalent condition in the AllConditions array.
        Condition globalCondition = AllConditionsEditor.TryGetConditionAt(conditionIndex);

        // Set the description based on the globalCondition's description.
        descriptionProperty.stringValue = globalCondition != null ? globalCondition.description : blankDescription;

        // Set the hash based on the description.
        hashProperty.intValue = Animator.StringToHash(descriptionProperty.stringValue);

        // Display the toggle for the satisfied bool.
        EditorGUILayout.PropertyField(satisfiedProperty, GUIContent.none, GUILayout.Width(width + toggleOffset));

        // Display a button with a '-' that when clicked removes the target from the ConditionCollection's conditions array.
        if (GUILayout.Button("-", GUILayout.Width(conditionButtonWidth)))
        {
            conditionsProperty.RemoveFromObjectArray(condition);
        }

        EditorGUILayout.EndHorizontal();

        // Push all changes made on the serializedObject back to the target.
        serializedObject.ApplyModifiedProperties();
    }


    // This function is static such that it can be called without an editor being instanced.
    public static Condition CreateCondition()
    {
        // Create a new instance of Condition.
        Condition newCondition = CreateInstance<Condition>();

        string blankDescription = "No conditions set.";
        
        // Try and set the new condition's description to the first condition in the AllConditions array.
        Condition globalCondition = AllConditionsEditor.TryGetConditionAt(0);
        newCondition.description = globalCondition != null ? globalCondition.description : blankDescription;

        // Set the hash based on this description.
        SetHash(newCondition);
        return newCondition;
    }


    public static Condition CreateCondition(string description)
    {
        // Create a new instance of the Condition.
        Condition newCondition = CreateInstance<Condition>();

        // Set the description and the hash based on it.
        newCondition.description = description;
        SetHash(newCondition);
        return newCondition;
    }


    private static void SetHash(Condition condition)
    {
        condition.hash = Animator.StringToHash(condition.description);
    }
}
