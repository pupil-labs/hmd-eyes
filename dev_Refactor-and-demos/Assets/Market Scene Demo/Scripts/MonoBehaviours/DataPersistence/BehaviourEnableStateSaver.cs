using UnityEngine;

public class BehaviourEnableStateSaver : Saver
{
    public Behaviour behaviourToSave;   // Reference to the Behaviour that will have its enabled state saved from and loaded to.


    protected override string SetKey ()
    {
        // Here the key will be based on the name of the behaviour, the behaviour's type and a unique identifier.
        return behaviourToSave.name + behaviourToSave.GetType().FullName + uniqueIdentifier;
    }


    protected override void Save ()
    {
        saveData.Save (key, behaviourToSave.enabled);
    }


    protected override void Load ()
    {
        // Create a variable to be passed by reference to the Load function.
        bool enabledState = false;

        // If the load function returns true then the enabled state can be set.
        if (saveData.Load(key, ref enabledState))
            behaviourToSave.enabled = enabledState;
    }
}
