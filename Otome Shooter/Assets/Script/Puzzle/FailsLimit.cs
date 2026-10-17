using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class FailsLimit : MonoBehaviour
{
    [Header("Configuración de Fails")]
    [SerializeField] private int maxFails = 3;
    [SerializeField] private Image[] failLights; // Array de los 3 focos verdes
    [SerializeField] private Color greenColor = Color.green;
    [SerializeField] private Color redColor = Color.red;

    [Header("Eventos")]
    public UnityEvent onMaxFailsReached; // Se invoca cuando se alcanza el máximo de fails

    private int currentFails;
    private bool isDefeated = false;

    void Start()
    {
        InitializeFails();
    }

    private void InitializeFails()
    {
        currentFails = 0;
        isDefeated = false;

        // Inicializar todos los focos en verde
        foreach (Image light in failLights)
        {
            if (light != null)
                light.color = greenColor;
        }
    }

    // Método para reducir fails (llamar cuando el jugador falle)
    public void ReduceFail()
    {
        if (isDefeated) return;

        currentFails++;

        // Actualizar focos visualmente
        UpdateFailLights();

        // Verificar si se alcanzó el límite de fails
        if (currentFails >= maxFails)
        {
            OnDefeat();
        }
    }

    private void UpdateFailLights()
    {
        for (int i = 0; i < failLights.Length; i++)
        {
            if (failLights[i] != null)
            {
                // Los focos se vuelven rojos desde la izquierda
                if (i < currentFails)
                {
                    failLights[i].color = redColor;
                }
                else
                {
                    failLights[i].color = greenColor;
                }
            }
        }
    }

    private void OnDefeat()
    {
        isDefeated = true;

        // Invocar el evento de máximo fails alcanzado
        onMaxFailsReached?.Invoke();
        Debug.Log("❌ Derrota - Límite de fails alcanzado");
        InitializeFails();
        gameObject.SetActive(false);
    }

    // Método para obtener fails actuales
    public int GetCurrentFails()
    {
        return currentFails;
    }

    // Método para obtener si está derrotado
    public bool IsDefeated()
    {
        return isDefeated;
    }
}