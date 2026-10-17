using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class InteractiveObject : MonoBehaviour
{
    [Header("Configuración Outline")]
    [SerializeField] private Color highlightColor = Color.yellow;

    [Header("Evento al Interactuar")]
    public UnityEvent OnInteract;

    [Header("Configuración Reactivación")]
    [SerializeField] private float reactivationTime = 10f;

    private Renderer rend;
    private MaterialPropertyBlock propBlock;
    private Color originalColor;
    private bool isPlayerInside = false;
    public bool canInteract = true;
    private bool isRestart;

    // Referencia al manager de UI
    private InteractionIndicator uiManager;
    private string objectName;

    void Awake()
    {
        // Buscar Renderer
        rend = GetComponent<Renderer>() ?? GetComponentInParent<Renderer>();
        if (rend == null)
        {
            Debug.LogWarning($"[InteractiveObject] No se encontró Renderer en {name} ni en sus padres.");
            return;
        }

        propBlock = new MaterialPropertyBlock();
        rend.GetPropertyBlock(propBlock);

        originalColor = rend.sharedMaterial.HasProperty("_OutlineColor")
            ? rend.sharedMaterial.GetColor("_OutlineColor")
            : Color.white;

        // Buscar el UI Manager
        uiManager = FindObjectOfType<InteractionIndicator>();
        objectName = gameObject.name;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            if (canInteract)
            {
                SetOutlineColor(highlightColor);
                if (uiManager != null)
                    uiManager.ShowInteractionPrompt();
            }
            else
            {
                if (isRestart && uiManager != null)
                {
                    uiManager.ShowCooldown(objectName, reactivationTime);
                }
            }
            other.SendMessage("SetInteractable", this, SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            SetOutlineColor(originalColor);
            if (uiManager != null)
                uiManager.HideInteractionPrompt();
            other.SendMessage("ClearInteractable", this, SendMessageOptions.DontRequireReceiver);
        }
    }

    public void Interact()
    {
        if (!isPlayerInside || !canInteract) return;

        // Ejecutar el evento
        canInteract = false;
        SetOutlineColor(originalColor);
        if (uiManager != null)
            uiManager.HideInteractionPrompt();
        OnInteract?.Invoke();
    }

    public void ReactivateObject()
    {
        isRestart = true;
        StartCoroutine(ReactivarObjeto());
    }

    private IEnumerator ReactivarObjeto()
    {
        float timer = reactivationTime;

        // Mostrar cooldown inicial
        if (uiManager != null && isPlayerInside)
            uiManager.ShowCooldown(objectName, timer);

        // Actualizar cooldown en tiempo real
        while (timer > 0)
        {
            if (uiManager != null && isPlayerInside)
                uiManager.UpdateCooldown(timer);

            timer -= Time.deltaTime;
            yield return null;
        }

        isRestart = false;
        // Reactivar objeto
        canInteract = true;
        Debug.Log($"🔄 {gameObject.name} reactivado");

        // Actualizar indicador si el jugador sigue dentro
        if (isPlayerInside)
        {
            SetOutlineColor(highlightColor);
            if (uiManager != null)
                uiManager.ShowInteractionPrompt();
        }
    }

    public void SetCanInteract(bool canInteract)
    {
        this.canInteract = canInteract;
    }

    private void SetOutlineColor(Color color)
    {
        if (rend == null) return;

        rend.GetPropertyBlock(propBlock);
        propBlock.SetColor("_OutlineColor", color);
        rend.SetPropertyBlock(propBlock);
    }
}