using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
    // Enum para elegir el modo del timer en el inspector
    public enum TimerMode { CountUp, CountDown }

    [Header("Referencias (auto-detectadas si las dejás vacías)")]
    [SerializeField] FirstPersonController playerController;
    [SerializeField] PlayerHealth playerHealth;

    [Header("Vida (Slider)")]
    [SerializeField] Slider healthSlider;

    [Header("Cargas de Dash (3 imágenes)")]
    [SerializeField] Image[] dashChargeImages = new Image[3];
    [SerializeField] Color chargedColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] Color emptyColor = new Color(0.25f, 0.25f, 0.25f, 1f);

    [Header("Temporizador")]
    [Tooltip("Text con el tiempo en formato MM:SS.")]
    [SerializeField] Text timerText;
    [SerializeField] TimerMode timerMode = TimerMode.CountUp;
    [Tooltip("Duración en SEGUNDOS. Solo se usa en CountDown. 120 = 02:00.")]
    [SerializeField] float countdownSeconds = 120f;
    [Tooltip("Si está en CountDown y llega a 0, dispara derrota.")]
    [SerializeField] bool defeatOnTimeout = true;

    [Header("Paneles de Fin de Juego")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;
    [SerializeField] bool pauseOnGameOver = true;
    [SerializeField] bool showCursorOnGameOver = true;

    int remainingEnemies;
    bool gameEnded;

    // --- Estado interno del timer ---
    float timerAccumulated;   // segundos transcurridos desde el inicio
    bool timerRunning;

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

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (playerHealth != null) playerHealth.HealthChanged += OnHealthChanged;
        if (playerController != null) playerController.DashChargesChanged += OnDashChargesChanged;
    }

    void Start()
    {
        // Sincronización inicial de UI
        if (playerHealth != null)
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        if (playerController != null)
            OnDashChargesChanged(playerController.CurrentDashCharges, playerController.MaxDashCharges);

        // Suscribirse a muerte del jugador
        if (playerHealth != null)
            playerHealth.Died += OnPlayerDied;

        // Contar enemigos vivos
        var enemies = FindObjectsOfType<EnemyHealth>();
        remainingEnemies = enemies.Length;
        foreach (var e in enemies) e.Died += OnEnemyDied;

        Debug.Log($"[PlayerUIManager] Enemigos iniciales: {remainingEnemies}");

        // --- Arrancar temporizador ---
        timerAccumulated = 0f;
        timerRunning = true;
        UpdateTimerText(GetDisplaySeconds());
    }

    void Update()
    {
        if (!timerRunning || gameEnded) return;

        timerAccumulated += Time.deltaTime;

        float display = GetDisplaySeconds();
        UpdateTimerText(display);

        // CountDown agotado → derrota (opcional)
        if (timerMode == TimerMode.CountDown && display <= 0f && defeatOnTimeout)
        {
            ShowDefeat();
        }
    }

    void OnDisable()
    {
        if (playerHealth != null) playerHealth.HealthChanged -= OnHealthChanged;
        if (playerController != null) playerController.DashChargesChanged -= OnDashChargesChanged;
        if (playerHealth != null) playerHealth.Died -= OnPlayerDied;
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
    // DASH
    // ---------------------------------------------
    void OnDashChargesChanged(int current, int max)
    {
        for (int i = 0; i < dashChargeImages.Length; i++)
        {
            if (dashChargeImages[i] == null) continue;

            bool shouldShow = i < max;
            dashChargeImages[i].gameObject.SetActive(shouldShow);
            if (!shouldShow) continue;

            dashChargeImages[i].color = i < current ? chargedColor : emptyColor;
        }
    }

    // ---------------------------------------------
    // TIMER
    // ---------------------------------------------
    float GetDisplaySeconds()
    {
        return timerMode == TimerMode.CountUp
            ? timerAccumulated
            : Mathf.Max(0f, countdownSeconds - timerAccumulated);
    }

    void UpdateTimerText(float seconds)
    {
        if (timerText == null) return;

        int totalSecs = Mathf.CeilToInt(seconds);      // redondeo hacia arriba
        int mins = totalSecs / 60;
        int secs = totalSecs % 60;

        // Formato MM:SS (con ceros a la izquierda)
        timerText.text = $"{mins:00}:{secs:00}";
    }

    // ---------------------------------------------
    // FIN DEL JUEGO
    // ---------------------------------------------
    void OnEnemyDied(EnemyHealth enemy)
    {
        if (gameEnded) return;

        if (enemy != null) enemy.Died -= OnEnemyDied;
        remainingEnemies--;
        Debug.Log($"[PlayerUIManager] Enemigos restantes: {remainingEnemies}");

        if (remainingEnemies <= 0)
            ShowVictory();
    }

    void OnPlayerDied()
    {
        if (gameEnded) return;
        ShowDefeat();
    }

    void ShowVictory()
    {
        if (gameEnded) return;
        gameEnded = true;
        timerRunning = false;

        if (victoryPanel != null) victoryPanel.SetActive(true);
        ApplyGameOverState();
        Debug.Log("[PlayerUIManager] VICTORIA");
    }

    void ShowDefeat()
    {
        if (gameEnded) return;
        gameEnded = true;
        timerRunning = false;

        if (defeatPanel != null) defeatPanel.SetActive(true);
        ApplyGameOverState();
        Debug.Log("[PlayerUIManager] DERROTA");
    }

    void ApplyGameOverState()
    {
        if (pauseOnGameOver) Time.timeScale = 0f;
        if (showCursorOnGameOver)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}