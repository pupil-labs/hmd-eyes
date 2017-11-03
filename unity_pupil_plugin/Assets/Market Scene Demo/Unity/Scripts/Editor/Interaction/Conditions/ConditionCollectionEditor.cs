using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ConditionCollection))]
public class ConditionCollectionEditor : EditorWithSubEditors<ConditionEditor, Condition>
{
    public SerializedProperty collectionsProperty;              // Represents the array of ConditionCollections that the target belongs to.


    private ConditionCollection conditionCollection;            // Reference to the target.
    private SerializedProperty descriptionProperty;             // Represents a string description for the target.
    private SerializedProperty conditionsProperty;              // Represents an array of Conditions for the target.
    private SerializedProperty reactionCollectionProperty;      // Represents the ReactionCollection that is referenced by the target.


    private const float conditionButtonWidth = 30f;             // Width of the button for adding a new Condition.
    private const float collectionButtonWidth = 125f;           // Width of the button for removing the target from it's Interactable.
    private const string conditionCollectionPropDescriptionName = "description";
                                                                // Name of the field that represents a string description for the target.
    private const string conditionCollectionPropRequiredConditionsName = "requiredConditions";
                                                                // Name of the field that represents an array of Conditions for the target.
    private const string conditionCollectionPropReactionCollectionName = "reactionCollection";
                                                                // Name of the field that represents the ReactionCollection that is referenced by the target.


    private void OnEnable ()
    {
        // Cache a reference to the target.
        conditionCollection = (ConditionCollection)target;

        // If this Editor exists but isn't targeting anything destroy it.
        if (target == null)
        {
            DestroyImmediate (this);
            return;
        }

        // Cache the SerializedProperties.
        descriptionProperty = serializedObject.FindProperty(conditionCollectionPropDescriptionName);
        conditionsProperty = serializedObject.FindProperty(conditionCollectionPropRequiredConditionsName);
        reactionCollectionProperty = serializedObject.FindProperty(conditionCollectionPropReactionCollectionName);

        // Check if the Editors for the Conditions need creating and optionally create them.
        CheckAndCreateSubEditors (conditionCollection.requiredConditions);
    }


    private void OnDisable ()
    {
        // When this Editor ends, destroy all it's subEditors.
        CleanupEditors ();
    }


    // This is called immediately when a subEditor is created.
    protected override void SubEditorSetup (ConditionEditor editor)
    {
        // Set the editor type so that the correct GUI for Condition is shown.
        editor.editorType = ConditionEditor.EditorType.ConditionCollection;

        // Assign the conditions property so that the ConditionEditor can remove its target if necessary.
        editor.conditionsProperty = conditionsProperty;
    }


    public override void OnInspectorGUI ()
    {
        // Pull the information from the target into the serializedObject.
        serializedObject.Update ();

        // Check if the Editors for the Conditions need creating and optionally create them.
        CheckAndCreateSubEditors(conditionCollection.requiredConditions);
        
        EditorGUILayout.BeginVertical(GUI.skin.box);
        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal();

        // Use the isExpanded bool for the descriptionProperty to store whether the foldout is open or closed.
        descriptionProperty.isExpanded = EditorGUILayout.Foldout(descriptionProperty.isExpanded, descriptionProperty.stringValue);

        // Display a button showing 'Remove Collection' which removes the target from the Interactable when clicked.
        if (GUILayout.Button("Remove Collection", GUILayout.Width(collectionButtonWidth)))
        {
            collectionsProperty.RemoveFromObjectArray (conditionCollection);
        }

        EditorGUILayout.EndHorizontal();
        
        // If the foldout is open show the expanded GUI.
        if (descriptionProperty.isExpanded)
        {
            ExpandedGUI ();
        }
        
        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical();

        // Push all changes made on the serializedObject back to the target.
        serializedObject.ApplyModifiedProperties();
    }


    private void ExpandedGUI ()
    {
        EditorGUILayout.Space();

        // Display the description for editing.
        EditorGUILayout.PropertyField(descriptionProperty);

        EditorGUILayout.Space();

        // Display the Labels for the Conditions evenly split over the width of the inspector.
        float space = EditorGUIUtility.currentViewWidth / 3f;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Condition", GUILayout.Width(space));
        EditorGUILayout.LabelField("Satisfied?", GUILayout.Width(space));
        EditorGUILayout.LabelField("Add/Remove", GUILayout.Width(space));
        EditorGUILayout.EndHorizontal();

        // Display each of the Conditions.
        EditorGUILayout.BeginVertical(GUI.skin.box);
        for (int i = 0; i < subEditors.Length; i++)
        {
            subEditors[i].OnInspectorGUI();
        }
        EditorGUILayout.EndHorizontal();

        // Display a right aligned button which when clicked adds a Condition to the array.
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace ();
        if (GUILayout.Button("+", GUILayout.Width(conditionButtonWidth)))
        {
            Condition newCondition = ConditionEditor.CreateCondition();
            conditionsProperty.AddToObjectArray(newCondition);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Display the reference to the ReactionCollection for editing.
        EditorGUILayout.PropertyField(reactionCollectionProperty);
    }


    // This function is static such that it can be called without an editor being instanced.
    public static ConditionCollection CreateConditionCollection()
    {
        // Create a new instance of ConditionCollection.
        ConditionCollection newConditionCollection = CreateInstance<ConditionCollection>();

        // Give it a default description.
        newConditionCollection.description = "New condition collection";

        // Give it a single default Condition.
        newConditionCollection.requiredConditions = new Condition[1];
        newConditionCollection.requiredConditions[0] = ConditionEditor.CreateCondition();
        return newConditionCollection;
    }
}
