using UnityEngine;
using UnityEngine.UI;

public class Inventory : MonoBehaviour
{
    public Image[] itemImages = new Image[numItemSlots];    // The Image components that display the Items.
    public Item[] items = new Item[numItemSlots];           // The Items that are carried by the player.


    public const int numItemSlots = 4;                      // The number of items that can be carried.  This is a constant so that the number of Images and Items are always the same.


    // This function is called by the PickedUpItemReaction in order to add an item to the inventory.
    public void AddItem(Item itemToAdd)
    {
        // Go through all the item slots...
        for (int i = 0; i < items.Length; i++)
        {
            // ... if the item slot is empty...
            if (items[i] == null)
            {
                // ... set it to the picked up item and set the image component to display the item's sprite.
                items[i] = itemToAdd;
                itemImages[i].sprite = itemToAdd.sprite;
                itemImages[i].enabled = true;
                return;
            }
        }
    }


    // This function is called by the LostItemReaction in order to remove an item from the inventory.
    public void RemoveItem (Item itemToRemove)
    {
        // Go through all the item slots...
        for (int i = 0; i < items.Length; i++)
        {
            // ... if the item slot has the item to be removed...
            if (items[i] == itemToRemove)
            {
                // ... set the item slot to null and set the image component to display nothing.
                items[i] = null;
                itemImages[i].sprite = null;
                itemImages[i].enabled = false;
                return;
            }
        }
    }
}
