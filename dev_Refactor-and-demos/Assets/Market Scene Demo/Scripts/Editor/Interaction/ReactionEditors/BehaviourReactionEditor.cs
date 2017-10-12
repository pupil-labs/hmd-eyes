using UnityEditor;

[CustomEditor(typeof(BehaviourReaction))]
public class BehaviourReactionEditor : ReactionEditor
{
    protected override string GetFoldoutLabel ()
    {
        return "Behaviour Reaction";
    }
}
