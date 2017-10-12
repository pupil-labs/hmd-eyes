using UnityEditor;

[CustomEditor(typeof(ConditionReaction))]
public class ConditionReactionEditor : ReactionEditor
{
    private SerializedProperty conditionProperty;       // Represents the Condition that will be changed.
    private SerializedProperty satisfiedProperty;       // Represents the value that the Condition's satifised flag will be set to.


    private const string conditionReactionPropConditionName = "condition";
                                                        // Name of the field which is the Condition that will be changed.
    private const string conditionReactionPropSatisfiedName = "satisfied";
                                                        // Name of the bool field which is the value the Condition will get.


    protected override void Init ()
    {
        // Cache the SerializedProperties.
        conditionProperty = serializedObject.FindProperty (conditionReactionPropConditionName);
        satisfiedProperty = serializedObject.FindProperty (conditionReactionPropSatisfiedName);
    }

	
    protected override void DrawReaction ()
    {
        // If there's isn't a Condition yet, set it to the first Condition from the AllConditions array.
        if (conditionProperty.objectReferenceValue == null)
            conditionProperty.objectReferenceValue = AllConditionsEditor.TryGetConditionAt(0);

        // Get the index of the Condition in the AllConditions array.
        int index = AllConditionsEditor.TryGetConditionIndex ((Condition)conditionProperty.objectReferenceValue);

        // Use and set that index based on a popup of all the descriptions of the Conditions.
        index = EditorGUILayout.Popup (index, AllConditionsEditor.AllConditionDescriptions);

        // Set the Condition based on the new index from the AllConditions array.
        conditionProperty.objectReferenceValue = AllConditionsEditor.TryGetConditionAt(index);

        // Use default toggle GUI for the satisfied field.
        EditorGUILayout.PropertyField (satisfiedProperty);
    }


    protected override string GetFoldoutLabel ()
    {
        return "Condition Reaction";
    }
}
