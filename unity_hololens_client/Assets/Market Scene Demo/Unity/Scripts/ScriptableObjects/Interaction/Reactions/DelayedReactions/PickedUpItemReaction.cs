public class PickedUpItemReaction : DelayedReaction
{
    public Item item;               // The item asset to be added to the Inventory.


    private Inventory inventory;    // Reference to the Inventory component.


    protected override void SpecificInit()
    {
        inventory = FindObjectOfType<Inventory>();
    }


    protected override void ImmediateReaction()
    {
        inventory.AddItem(item);
    }
}
