using UnityEngine;

// This class represents a single outcome from clicking
// on an interactable.  It has an array of Conditions
// and if they are all met an ReactionCollection that
// will happen.
public class ConditionCollection : ScriptableObject
{
    public string description;                                  // Description of the ConditionCollection.  This is used purely for identification in the inspector.
    public Condition[] requiredConditions = new Condition[0];   // The Conditions that need to be met in order for the ReactionCollection to React.
    public ReactionCollection reactionCollection;               // Reference to the ReactionCollection that will React should all the Conditions be met.


    // This is called by the Interactable one at a time for each of its ConditionCollections until one returns true.
    public bool CheckAndReact()
    {
        // Go through all Conditions...
        for (int i = 0; i < requiredConditions.Length; i++)
        {
            // ... and check them against the AllConditions version of the Condition.  If they don't have the same satisfied flag, return false.
            if (!AllConditions.CheckCondition (requiredConditions[i]))
                return false;
        }

        // If there is an ReactionCollection assigned, call its React function.
        if(reactionCollection)
            reactionCollection.React();

        // A Reaction happened so return true.
        return true;
    }
}