public interface IInteractable
{
    string GetInteractionHint(ItemType heldItem);
    void Interact(ItemType heldItem);
}
