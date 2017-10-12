using UnityEngine;

public class GameObjectReaction : DelayedReaction
{
    public GameObject gameObject;       // The gameobject to be turned on or off.
    public bool activeState;            // The state that the gameobject will be in after the Reaction.


    protected override void ImmediateReaction()
    {
        gameObject.SetActive (activeState);
    }
}