using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class TimerManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;

    [Header("Time Settings")]
    public float timeLimit = 180f; // 3 minutos por defecto

    [Header("Color Settings")]
    public Color runningColor = Color.white;
    public Color pausedColor = Color.yellow;

    [Header("Events")]
    public UnityEvent OnTimeExpired;

    private float timeRemaining;
    private bool isRunning = false;

    void Update()
    {
        if (!isRunning) return;

        // Reduce time
        timeRemaining -= Time.deltaTime;
        UpdateUI();

        // Check if time expired
        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            OnTimeExpired?.Invoke();
        }
    }

    void UpdateUI()
    {
        timerText.color = isRunning ? runningColor : pausedColor;

        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);

        // Sin boxing
        timerText.text = $"{minutes:00}:{seconds:00}";
    }


    // Método para iniciar el temporizador
    public void StartTimer()
    {
        timeRemaining = timeLimit;
        isRunning = true;

        // Activar movimiento del jugador
        if (GamePlayerManager.Instance != null)
            GamePlayerManager.Instance.SetPlayerCanMove(true);

        UpdateUI();
    }

    // Método para pausar el temporizador (usar en puzzles)
    public void StopTimer()
    {
        isRunning = false;

        // Desactivar movimiento del jugador
        if (GamePlayerManager.Instance != null)
            GamePlayerManager.Instance.SetPlayerCanMove(false);

        UpdateUI(); // Actualizar UI para cambiar color
    }

    // Método para reanudar el temporizador (después de puzzles)
    public void PlayTimer()
    {
        isRunning = true;
        Time.timeScale = 1f;
        // Activar movimiento del jugador
        if (GamePlayerManager.Instance != null)
            GamePlayerManager.Instance.SetPlayerCanMove(true);

        UpdateUI(); // Actualizar UI para cambiar color
    }

    public void PauseTimer() 
    { 
        Time.timeScale = 0f;
    }
}