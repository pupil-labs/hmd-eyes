using System;
using UnityEngine;
using UnityEditor;

public abstract class ReactionEditor : Editor
{
    public bool showReaction;                       // Is the Reaction editor expanded?
    public SerializedProperty reactionsProperty;    // Represents the SerializedProperty of the array the target belongs to.


    private Reaction reaction;                      // The target Reaction.


    private const float buttonWidth = 30f;          // Width in pixels of the button to remove this Reaction from the ReactionCollection array.


    private void OnEnable ()
    {
        // Cache the target reference.
        reaction = (Reaction)target;

        // Call an initialisation method for inheriting classes.
        Init ();
    }


    // This function should be overridden by inheriting classes that need initialisation.
    protected virtual void Init () {}


    public override void OnInspectorGUI ()
    {
        // Pull data from the target into the serializedObject.
        serializedObject.Update ();

        EditorGUILayout.BeginVertical (GUI.skin.box);
        EditorGUI.indentLevel++;

        EditorGUILayout.BeginHorizontal ();
        
        // Display a foldout for the Reaction with a custom label.
        showReaction = EditorGUILayout.Foldout (showReaction, GetFoldoutLabel ());
        
        // Show a button which, if clicked, will remove this Reaction from the ReactionCollection.
        if (GUILayout.Button ("-", GUILayout.Width (buttonWidth)))
        {
            reactionsProperty.RemoveFromObjectArray (reaction);
        }
        EditorGUILayout.EndHorizontal ();
        
        // If the foldout is open, draw the GUI specific to the inheriting ReactionEditor.
        if (showReaction)
        {
            DrawReaction ();
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical ();

        // Push data back from the serializedObject to the target.
        serializedObject.ApplyModifiedProperties ();
    }


    public static Reaction CreateReaction (Type reactionType)
    {
        // Create a reaction of a given type.
        return (Reaction)CreateInstance (reactionType);
    }


    protected virtual void DrawReaction ()
    {
        // This function can overridden by inheriting classes, but if it isn't, draw the default for it's properties.
        DrawDefaultInspector ();
    }


    // The inheriting class must override this function to create the label of the foldout.
    protected abstract string GetFoldoutLabel ();
}
