using UnityEditor;

[CustomEditor(typeof(AudioReaction))]
public class AudioReactionEditor : ReactionEditor
{
    protected override string GetFoldoutLabel ()
    {
        return "Audio Reaction";
    }
}
