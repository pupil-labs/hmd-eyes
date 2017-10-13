using System.Collections.Generic;
using UnityEngine;

// This script is used to mark a Transform as potential starting point for a scene.
public class StartingPosition : MonoBehaviour
{
    public string startingPointName;        // The name that identifies this starting point in the scene.


    private static List<StartingPosition> allStartingPositions =  new List<StartingPosition> ();
                                            // This list contains all the StartingPositions that are currently active.


    private void OnEnable ()
    {
        // When this is activated, add it to the list that contains all active StartingPositions.
        allStartingPositions.Add (this);
    }


    private void OnDisable ()
    {
        // When this is deactivated, remove it from the list that contains all the active StartingPositions.
        allStartingPositions.Remove (this);
    }


    public static Transform FindStartingPosition (string pointName)
    {
        // Go through all the currently active StartingPositions and return the one with the matching name.
        for (int i = 0; i < allStartingPositions.Count; i++)
        {
            if (allStartingPositions[i].startingPointName == pointName)
                return allStartingPositions[i].transform;
        }

        // If a matching StartingPosition couldn't be found, return null.
        return null;
    }
}
