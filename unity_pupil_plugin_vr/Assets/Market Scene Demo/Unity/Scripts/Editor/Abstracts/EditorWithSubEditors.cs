using UnityEngine;
using UnityEditor;

// This class acts as a base class for Editors that have Editors
// nested within them.  For example, the InteractableEditor has
// an array of ConditionCollectionEditors.
// It's generic types represent the type of Editor array that are
// nested within this Editor and the target type of those Editors.
public abstract class EditorWithSubEditors<TEditor, TTarget> : Editor
    where TEditor : Editor
    where TTarget : Object
{
    protected TEditor[] subEditors;         // Array of Editors nested within this Editor.

    
    // This should be called in OnEnable and at the start of OnInspectorGUI.
    protected void CheckAndCreateSubEditors (TTarget[] subEditorTargets)
    {
        // If there are the correct number of subEditors then do nothing.
        if (subEditors != null && subEditors.Length == subEditorTargets.Length)
            return;

        // Otherwise get rid of the editors.
        CleanupEditors ();

        // Create an array of the subEditor type that is the right length for the targets.
        subEditors = new TEditor[subEditorTargets.Length];

        // Populate the array and setup each Editor.
        for (int i = 0; i < subEditors.Length; i++)
        {
            subEditors[i] = CreateEditor (subEditorTargets[i]) as TEditor;
            SubEditorSetup (subEditors[i]);
        }
    }


    // This should be called in OnDisable.
    protected void CleanupEditors ()
    {
        // If there are no subEditors do nothing.
        if (subEditors == null)
            return;

        // Otherwise destroy all the subEditors.
        for (int i = 0; i < subEditors.Length; i++)
        {
            DestroyImmediate (subEditors[i]);
        }

        // Null the array so it's GCed.
        subEditors = null;
    }


    // This must be overridden to provide any setup the subEditor needs when it is first created.
    protected abstract void SubEditorSetup (TEditor editor);
}
