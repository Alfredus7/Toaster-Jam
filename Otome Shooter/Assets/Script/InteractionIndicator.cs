using UnityEngine;
using TMPro;

public class InteractionIndicator : MonoBehaviour
{
    [Header("Referencia UI")]
    [SerializeField] private TextMeshProUGUI interactionText;

    [Header("Configuración")]
    [SerializeField] private string interactKey = "E";

    void Start()
    {
        // Ocultar texto al inicio
        HideInteractionPrompt();
    }

    public void ShowInteractionPrompt()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(true);
            interactionText.text = $"[{interactKey}] Interactuar";
        }
    }

    public void ShowCooldown(string objectName, float remainingTime)
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(true);
            interactionText.text = $"{objectName} - Reactivando: {remainingTime:F1}s";
        }
    }

    public void UpdateCooldown(float remainingTime)
    {
        if (interactionText != null)
        {
            interactionText.text = $"Reactivando: {remainingTime:F1}s";
        }
    }

    public void HideInteractionPrompt()
    {
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}