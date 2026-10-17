using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    public InteractiveObject currentObject;
    private InteractionIndicator uiManager;

    void Start()
    {
        uiManager = FindObjectOfType<InteractionIndicator>();
    }

    // 🔹 Llamado automáticamente por el objeto interactuable (SendMessage)
    public void SetInteractable(InteractiveObject obj)
    {
        currentObject = obj;
    }

    // 🔹 Llamado desde el Input System (acción "Interact")
    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            Interact();
        }
    }

    // Evento externo button UI
    public void Interact()
    {
        if (!currentObject) return;
        currentObject.Interact();
    }

    // Para limpiar la referencia cuando el jugador se aleja
    public void ClearInteractable(InteractiveObject obj)
    {
        if (currentObject == obj)
        {
            currentObject = null;
            if (uiManager != null)
                uiManager.HideInteractionPrompt();
        }
    }
}