using UnityEditor;

[CustomEditor(typeof(LostItemReaction))]
public class LostItemReactionEditor : ReactionEditor
{
    protected override string GetFoldoutLabel ()
    {
        return "Lost Item Reaction";
    }
}
