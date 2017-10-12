using UnityEngine;
using UnityEditor;

// This class contains extension methods for the SerializedProperty
// class.  Specifically, methods for dealing with object arrays.
public static class SerializedPropertyExtensions
{
    // Use this to add an object to an object array represented by a SerializedProperty.
    public static void AddToObjectArray<T> (this SerializedProperty arrayProperty, T elementToAdd)
        where T : Object
    {
        // If the SerializedProperty this is being called from is not an array, throw an exception.
        if (!arrayProperty.isArray)
            throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

        // Pull all the information from the target of the serializedObject.
        arrayProperty.serializedObject.Update();

        // Add a null array element to the end of the array then populate it with the object parameter.
        arrayProperty.InsertArrayElementAtIndex(arrayProperty.arraySize);
        arrayProperty.GetArrayElementAtIndex(arrayProperty.arraySize - 1).objectReferenceValue = elementToAdd;

        // Push all the information on the serializedObject back to the target.
        arrayProperty.serializedObject.ApplyModifiedProperties();
    }


    // Use this to remove the object at an index from an object array represented by a SerializedProperty.
    public static void RemoveFromObjectArrayAt (this SerializedProperty arrayProperty, int index)
    {
        // If the index is not appropriate or the serializedProperty this is being called from is not an array, throw an exception.
        if(index < 0)
            throw new UnityException("SerializedProperty " + arrayProperty.name + " cannot have negative elements removed.");

        if (!arrayProperty.isArray)
            throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

        if(index > arrayProperty.arraySize - 1)
            throw new UnityException("SerializedProperty " + arrayProperty.name + " has only " + arrayProperty.arraySize + " elements so element " + index + " cannot be removed.");

        // Pull all the information from the target of the serializedObject.
        arrayProperty.serializedObject.Update();

        // If there is a non-null element at the index, null it.
        if (arrayProperty.GetArrayElementAtIndex(index).objectReferenceValue)
            arrayProperty.DeleteArrayElementAtIndex(index);

        // Delete the null element from the array at the index.
        arrayProperty.DeleteArrayElementAtIndex(index);

        // Push all the information on the serializedObject back to the target.
        arrayProperty.serializedObject.ApplyModifiedProperties();
    }


    // Use this to remove an object from an object array represented by a SerializedProperty.
    public static void RemoveFromObjectArray<T> (this SerializedProperty arrayProperty, T elementToRemove)
        where T : Object
    {
        // If either the serializedProperty doesn't represent an array or the element is null, throw an exception.
        if (!arrayProperty.isArray)
            throw new UnityException("SerializedProperty " + arrayProperty.name + " is not an array.");

        if(!elementToRemove)
            throw new UnityException("Removing a null element is not supported using this method.");

        // Pull all the information from the target of the serializedObject.
        arrayProperty.serializedObject.Update();

        // Go through all the elements in the serializedProperty's array...
        for (int i = 0; i < arrayProperty.arraySize; i++)
        {
            SerializedProperty elementProperty = arrayProperty.GetArrayElementAtIndex(i);

            // ... until the element matches the parameter...
            if (elementProperty.objectReferenceValue == elementToRemove)
            {
                // ... then remove it.
                arrayProperty.RemoveFromObjectArrayAt (i);
                return;
            }
        }

        throw new UnityException("Element " + elementToRemove.name + "was not found in property " + arrayProperty.name);
    }
}
