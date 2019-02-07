using UnityEngine;

// This is one of the core features of the game.
// Each one acts like a hub for all things that transpire
// over the course of the game.
// The script must be on a gameobject with a collider and
// an event trigger.  The event trigger should tell the
// player to approach the interactionLocation and the 
// player should call the Interact function when they arrive.
public class Interactable : MonoBehaviour
{
    public Transform interactionLocation;                   // The position and rotation the player should go to in order to interact with this Interactable.
    public ConditionCollection[] conditionCollections = new ConditionCollection[0];
                                                            // All the different Conditions and relevant Reactions that can happen based on them.
    public ReactionCollection defaultReactionCollection;    // If none of the ConditionCollections are reacted to, this one is used.


    // This is called when the player arrives at the interactionLocation.
    public void Interact ()
    {
        // Go through all the ConditionCollections...
        for (int i = 0; i < conditionCollections.Length; i++)
        {
            // ... then check and potentially react to each.  If the reaction happens, exit the function.
            if (conditionCollections[i].CheckAndReact ())
                return;
        }

        // If none of the reactions happened, use the default ReactionCollection.
        defaultReactionCollection.React ();
    }
}
