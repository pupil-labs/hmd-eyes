using UnityEngine;

// This class is used to determine whether or not Reactions
// should happen.  Instances of Condition exist in two places:
// as assets which are part of the AllConditions asset and as
// part of ConditionCollections.  The Conditions that are part
// of the AllConditions asset are those that are set by
// Reactions and reflect the state of the game.  Those that
// are on ConditionCollections are compared to the
// AllConditions asset to determine whether other Reactions
// should happen.
public class Condition : ScriptableObject
{
    public string description;      // A description of the Condition, for example 'BeamsOff'.
    public bool satisfied;          // Whether or not the Condition has been satisfied, for example are the beams off?
    public int hash;                // A number which represents the description.  This is used to compare ConditionCollection Conditions to AllConditions Conditions.
}