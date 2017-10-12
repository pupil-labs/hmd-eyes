using UnityEngine;
using System.Collections;

// This is a base class for Reactions that need to have a delay.
public abstract class DelayedReaction : Reaction
{
    public float delay;             // All DelayedReactions need to have an time that they are delayed by.


    protected WaitForSeconds wait;  // Storing the wait created from the delay so it doesn't need to be created each time.


    // This function 'hides' the Init function from the Reaction class.
    // Hiding generally happens when the original function doesn't meet
    // the requirements for the function in the inheriting class.
    // Previously it was assumed that all Reactions just needed to call
    // SpecificInit but with DelayedReactions, wait needs to be set too.
    public new void Init ()
    {
        wait = new WaitForSeconds (delay);

        SpecificInit ();
    }


    // This function 'hides' the React function from the Reaction class.
    // It replaces the functionality with starting a coroutine instead.
    public new void React (MonoBehaviour monoBehaviour)
    {
        monoBehaviour.StartCoroutine (ReactCoroutine ());
    }


    protected IEnumerator ReactCoroutine ()
    {
        // Wait for the specified time.
        yield return wait;

        // Then call the ImmediateReaction function which must be defined in inherting classes.
        ImmediateReaction ();
    }
}
