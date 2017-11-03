using UnityEngine;

// This Reaction is for turning Behaviours on and
// off.  Behaviours are a subset of Components
// which have the enabled property, for example
// all MonoBehaviours are Behaviours as well as
// Animators, AudioSources and many more.
public class BehaviourReaction : DelayedReaction
{
    public Behaviour behaviour;     // The Behaviour to be turned on or off.
    public bool enabledState;       // The state the Behaviour will be in after the Reaction.


    protected override void ImmediateReaction()
    {
        behaviour.enabled = enabledState;
    }
}