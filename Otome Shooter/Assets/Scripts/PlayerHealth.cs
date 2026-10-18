using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] float maxHealth = 100f;
    float current;

    public bool IsAlive => current > 0f;
    public float CurrentHealth => current;
    public float MaxHealth => maxHealth;
    public float HealthPercent => maxHealth > 0f ? current / maxHealth : 0f;

    public event System.Action<float, float> HealthChanged; // (current, max)
    public event System.Action Died;                        

    void Awake() => current = maxHealth;

    void Start() => HealthChanged?.Invoke(current, maxHealth);

    public void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal)
    {
        if (!IsAlive) return;
        current = Mathf.Max(0f, current - amount);
        HealthChanged?.Invoke(current, maxHealth);

        if (current <= 0f)
            Died?.Invoke();          
    }

    public void Heal(float amount)
    {
        if (!IsAlive) return;
        current = Mathf.Min(maxHealth, current + amount);
        HealthChanged?.Invoke(current, maxHealth);
    }
}

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 hitPoint, Vector3 hitNormal);
    bool IsAlive { get; }
}