using UnityEngine;
using UnityEngine.UI;

public class PlayerUIManager : MonoBehaviour
{
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
    [SerializeField] Text timerText;
    [SerializeField] TimerMode timerMode = TimerMode.CountUp;
    [Tooltip("Duración en SEGUNDOS. Solo se usa en CountDown. 120 = 02:00.")]
    [SerializeField] float countdownSeconds = 120f;
    [SerializeField] bool defeatOnTimeout = true;

    [Header("Paneles de Fin de Juego")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] GameObject defeatPanel;
    [SerializeField] bool pauseOnGameOver = true;
    [SerializeField] bool showCursorOnGameOver = true;

    [Header("Derribo (feedback opcional)")]
    [SerializeField] GameObject downedPanel; // opcional: "Te están ayudando..."

    int remainingEnemies;
    bool gameEnded;

    float timerAccumulated;
    bool timerRunning;

    void Awake()
    {
        if (playerController == null) playerController = FindObjectOfType<FirstPersonController>();
        if (playerHealth == null) playerHealth = FindObjectOfType<PlayerHealth>();

        if (playerController == null) Debug.LogError("[PlayerUIManager] No encontró FirstPersonController.");
        if (playerHealth == null) Debug.LogError("[PlayerUIManager] No encontró PlayerHealth.");
        if (healthSlider == null) Debug.LogError("[PlayerUIManager] healthSlider sin asignar.");

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }

        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(false);
        if (downedPanel != null) downedPanel.SetActive(false);
    }

    void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged += OnHealthChanged;
            playerHealth.Downed += OnPlayerDowned;      // nuevo
            playerHealth.Recovered += OnPlayerRecovered; // nuevo
        }
        if (playerController != null) playerController.DashChargesChanged += OnDashChargesChanged;
    }

    void Start()
    {
        if (playerHealth != null)
            OnHealthChanged(playerHealth.CurrentHealth, playerHealth.MaxHealth);

        if (playerController != null)
            OnDashChargesChanged(playerController.CurrentDashCharges, playerController.MaxDashCharges);

        // --- OJO: ya NO nos suscribimos a playerHealth.Died ---
        // (si querés, podés eliminar la línea antigua `playerHealth.Died += OnPlayerDied;`)

        var enemies = FindObjectsOfType<EnemyHealth>();
        remainingEnemies = enemies.Length;
        foreach (var e in enemies) e.Died += OnEnemyDied;

        Debug.Log($"[PlayerUIManager] Enemigos iniciales: {remainingEnemies}");

        timerAccumulated = 0f;
        timerRunning = true;
        UpdateTimerText(GetDisplaySeconds());
    }

    void Update()
    {
        if (!timerRunning || gameEnded) return;

        // *** CAMBIO CLAVE: unscaledDeltaTime para que el timer siga
        //     avanzando aunque Time.timeScale = 0 durante el derribo ***
        timerAccumulated += Time.unscaledDeltaTime;

        float display = GetDisplaySeconds();
        UpdateTimerText(display);

        if (timerMode == TimerMode.CountDown && display <= 0f && defeatOnTimeout)
        {
            ShowDefeat(); // única forma de perder
        }
    }

    void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.HealthChanged -= OnHealthChanged;
            playerHealth.Downed -= OnPlayerDowned;
            playerHealth.Recovered -= OnPlayerRecovered;
            // playerHealth.Died -= OnPlayerDied;  // quitado
        }
        if (playerController != null) playerController.DashChargesChanged -= OnDashChargesChanged;
    }

    void OnHealthChanged(float current, float max)
    {
        if (healthSlider != null)
            healthSlider.value = max > 0f ? current / max : 0f;
    }

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

    float GetDisplaySeconds()
    {
        return timerMode == TimerMode.CountUp
            ? timerAccumulated
            : Mathf.Max(0f, countdownSeconds - timerAccumulated);
    }

    void UpdateTimerText(float seconds)
    {
        if (timerText == null) return;
        int totalSecs = Mathf.CeilToInt(seconds);
        int mins = totalSecs / 60;
        int secs = totalSecs % 60;
        timerText.text = $"{mins:00}:{secs:00}";
    }

    // ---------------------------------------------
    // DERRIBO / RECUPERACIÓN
    // ---------------------------------------------
    void OnPlayerDowned()
    {
        if (gameEnded) return;

        // Congela el mundo (jugador + enemigos + físicas),
        // pero el timer sigue porque usa unscaledDeltaTime.
        Time.timeScale = 0f;

        if (downedPanel != null) downedPanel.SetActive(true);
        Debug.Log("[PlayerUIManager] Jugador derribado. Esperando recuperación...");
    }

    void OnPlayerRecovered()
    {
        if (downedPanel != null) downedPanel.SetActive(false);

        // Solo restauramos el tiempo si el juego no terminó.
        if (!gameEnded) Time.timeScale = 1f;

        Debug.Log("[PlayerUIManager] Jugador recuperado.");
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
        if (remainingEnemies <= 0) ShowVictory();
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
        if (downedPanel != null) downedPanel.SetActive(false);
        if (defeatPanel != null) defeatPanel.SetActive(true);
        ApplyGameOverState();
        Debug.Log("[PlayerUIManager] DERROTA (tiempo agotado)");
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