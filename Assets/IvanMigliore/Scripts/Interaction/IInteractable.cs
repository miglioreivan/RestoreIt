public interface IInteractable
{
    // Qualsiasi classe che usa questa interfaccia deve contenere questo metodo
    void StartInteraction();
    string GetInteractionText();
    bool canInteract();
}