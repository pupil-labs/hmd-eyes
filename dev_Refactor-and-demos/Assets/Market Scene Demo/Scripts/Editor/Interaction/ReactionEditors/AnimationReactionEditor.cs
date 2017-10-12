using UnityEditor;

[CustomEditor(typeof(AnimationReaction))]
public class AnimationReactionEditor : ReactionEditor
{
    protected override string GetFoldoutLabel ()
    {
        return "Animation Reaction";
    }
}
