using UnityEditor;

[CustomEditor(typeof(GameObjectReaction))]
public class GameObjectReactionEditor : ReactionEditor
{
    protected override string GetFoldoutLabel ()
    {
        return "GameObject Reaction";
    }
}
