using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(TextReaction))]
public class TextReactionEditor : ReactionEditor
{
    private SerializedProperty messageProperty;         // Represents the string field which is the message to be displayed.
    private SerializedProperty textColorProperty;       // Represents the color field which is the color of the message to be displayed.
    private SerializedProperty delayProperty;           // Represents the float field which is the delay before the messaage is displayed


    private const float messageGUILines = 3f;           // How many lines tall the GUI for the message field should be.
    private const float areaWidthOffset = 19f;          // Offset to account for the message GUI being made of two GUI calls.  It makes the GUI line up.
    private const string textReactionPropMessageName = "message";
                                                        // The name of the field which is the message to be written to the screen.
    private const string textReactionPropTextColorName = "textColor";
                                                        // The name of the field which is the color of the message to be written to the screen.
    private const string textReactionPropDelayName = "delay";
                                                        // The name of the field which is the delay before the message is written to the screen.


    protected override void Init ()
    {
        // Cache all the SerializedProperties.
        messageProperty = serializedObject.FindProperty (textReactionPropMessageName);
        textColorProperty = serializedObject.FindProperty (textReactionPropTextColorName);
        delayProperty = serializedObject.FindProperty (textReactionPropDelayName);
    }


    protected override void DrawReaction ()
    {
        EditorGUILayout.BeginHorizontal ();
        
        // Display a label whose width is offset such that the TextArea lines up with the rest of the GUI.
        EditorGUILayout.LabelField ("Message", GUILayout.Width (EditorGUIUtility.labelWidth - areaWidthOffset));

        // Display an interactable GUI element for the text of the message to be displayed over several lines.
        messageProperty.stringValue = EditorGUILayout.TextArea (messageProperty.stringValue, GUILayout.Height (EditorGUIUtility.singleLineHeight * messageGUILines));
        EditorGUILayout.EndHorizontal ();

        // Display default GUI for the text color and the delay.
        EditorGUILayout.PropertyField (textColorProperty);
        EditorGUILayout.PropertyField (delayProperty);
    }


    protected override string GetFoldoutLabel ()
    {
        return "Text Reaction";
    }
}
