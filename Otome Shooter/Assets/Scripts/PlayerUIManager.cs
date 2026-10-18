using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    [Header("Referencias (auto-detectadas si las dejás vacías)")]
    [SerializeField] FirstPersonController playerController;
    [SerializeField] PlayerHealth playerHealth;

    [Header("Vida (Slider)")]
    [Tooltip("Slider 0..1. Value = currentHealth / maxHealth.")]
    [SerializeField] Slider healthSlider;

    [Header("Cargas de Dash (3 imágenes)")]
    [SerializeField] Image[] dashChargeImages = new Image[3];
    [SerializeField] Color chargedColor = new Color(0.2f, 0.8f, 1f, 1f);      // cyan (cargada)
    [SerializeField] Color emptyColor = new Color(0.25f, 0.25f, 0.25f, 1f); // gris (vacía)

    void Awake()
    {
        if (playerController == null) playerController = FindObjectOfType<FirstPersonController>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerController == null)
            Debug.LogError("[PlayerUIManager] No encontró FirstPersonController en la escena.");
        if (playerHealth == null)
            Debug.LogError("[PlayerUIManager] No encontró PlayerHealth en la escena.");
        if (healthSlider == null)
            Debug.LogError("[PlayerUIManager] healthSlider sin asignar.");

        // El slider solo muestra, no interactúa
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += OnHealthChanged;
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth); // inicial
        }

        if (playerController != null)
        {
            playerController.DashChargesChanged += OnDashChargesChanged;
            OnDashChargesChanged(playerController.CurrentDashCharges, playerController.MaxDashCharges); // inicial
        }
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnHealthChanged;
        if (playerController != null) playerController.DashChargesChanged -= OnDashChargesChanged;
    }

    // ---------------------------------------------
    // VIDA
    // ---------------------------------------------
    void OnHealthChanged(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = max > 0f ? current / max : 0f;
    }

    // ---------------------------------------------
    // DASH (3 imágenes)
    // ---------------------------------------------
    void OnDashChargesChanged(int current, int max)
    {
        for (int i = 0; i < dashChargeImages.Length; i++)
        {
            if (dashChargeImages[i] == null) continue;

            bool shouldShow = i < max;
            dashChargeImages[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;

            // 2/3 → imágenes 0 y 1 encendidas, imagen 2 apagada
            dashChargeImages[i].color = i < current ? chargedColor : emptyColor;
        }
    }
}