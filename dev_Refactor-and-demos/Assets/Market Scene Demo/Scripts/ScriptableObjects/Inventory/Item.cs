using UnityEngine;

// This simple script represents Items that can be picked
// up in the game.  The inventory system is done using
// this script instead of just sprites to ensure that items
// are extensible.
[CreateAssetMenu]
public class Item : ScriptableObject
{
    public Sprite sprite;
}
