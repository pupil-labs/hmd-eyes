using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Inventory))]
public class InventoryEditor : Editor
{
    private bool[] showItemSlots = new bool[Inventory.numItemSlots];    // Whether the GUI for each Item slot is expanded.
    private SerializedProperty itemImagesProperty;                      // Represents the array of Image components to display the Items.
    private SerializedProperty itemsProperty;                           // Represents the array of Items.


    private const string inventoryPropItemImagesName = "itemImages";    // The name of the field that is an array of Image components.
    private const string inventoryPropItemsName = "items";              // The name of the field that is an array of Items.


    private void OnEnable ()
    {
        // Cache the SerializedProperties.
        itemImagesProperty = serializedObject.FindProperty (inventoryPropItemImagesName);
        itemsProperty = serializedObject.FindProperty (inventoryPropItemsName);
    }


    public override void OnInspectorGUI ()
    {
        // Pull all the information from the target into the serializedObject.
        serializedObject.Update ();

        // Display GUI for each Item slot.
        for (int i = 0; i < Inventory.numItemSlots; i++)
        {
            ItemSlotGUI (i);
        }

        // Push all the information from the serializedObject back into the target.
        serializedObject.ApplyModifiedProperties ();
    }


    private void ItemSlotGUI (int index)
    {
        EditorGUILayout.BeginVertical (GUI.skin.box);
        EditorGUI.indentLevel++;
        
        // Display a foldout to determine whether the GUI should be shown or not.
        showItemSlots[index] = EditorGUILayout.Foldout (showItemSlots[index], "Item slot " + index);

        // If the foldout is open then display default GUI for the specific elements in each array.
        if (showItemSlots[index])
        {
            EditorGUILayout.PropertyField (itemImagesProperty.GetArrayElementAtIndex (index));
            EditorGUILayout.PropertyField (itemsProperty.GetArrayElementAtIndex (index));
        }

        EditorGUI.indentLevel--;
        EditorGUILayout.EndVertical ();
    }
}
