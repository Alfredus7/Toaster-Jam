using System.Collections;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;

    [Header("Derribo / Recuperación")]
    [Tooltip("Segundos que queda derribado antes de recuperarse (tiempo real).")]
    [SerializeField] float downedDuration = 5f;
    [Tooltip("Porcentaje de vida con el que se recupera (0.5 = 50%).")]
    [Range(0f, 1f)]
    [SerializeField] float reviveHealthPercent = 0.5f;

    float current;
    bool downed;
    Coroutine recoverRoutine;

    public bool IsAlive => current > 0f && !downed;
    public bool IsDowned => downed;
    public float CurrentHealth => current;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? current / maxHealth : 0f;

    public event System.Action<float, float> HealthChanged; // (current, max)
    public event System.Action Died;   // se conserva por compatibilidad (ya no se dispara)
    public event System.Action Downed; // al llegar a 0
    public event System.Action Recovered; // al terminar la recuperación

    void Awake() => current = maxHealth;

    void Start() => HealthChanged?.Invoke(current, maxHealth);

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (downed || current <= 0f) return;

        current = Mathf.Max(0f, current - amount);
        HealthChanged?.Invoke(current, maxHealth);

        if (current <= 0f)
            EnterDowned();
    }

    public void Heal(float amount)
    {
        if (downed) return; // no se cura mientras está derribado
        current = Mathf.Min(maxHealth, current + amount);
        HealthChanged?.Invoke(current, maxHealth);
    }

    void EnterDowned()
    {
        if (downed) return;
        downed = true;
        Downed?.Invoke();

        if (recoverRoutine != null) StopCoroutine(recoverRoutine);
        recoverRoutine = StartCoroutine(RecoverRoutine());
    }

    IEnumerator RecoverRoutine()
    {
        // Tiempo real: sigue funcionando aunque Time.timeScale = 0
        yield return new WaitForSecondsRealtime(downedDuration);

        current = maxHealth * Mathf.Clamp01(reviveHealthPercent);
        downed = false;
        HealthChanged?.Invoke(current, maxHealth);
        Recovered?.Invoke();

        recoverRoutine = null;
    }
}

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    bool IsAlive { get; }
}