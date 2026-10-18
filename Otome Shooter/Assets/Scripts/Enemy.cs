using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class Enemy : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] float maxHealth = 100f;
    [SerializeField] bool destroyOnDeath = true;

    [Header("Popup de daño")]
    [SerializeField] DamagePopup popupPrefab;
    [SerializeField, Tooltip("Ancla opcional. Si está vacío, usa el punto de impacto.")]
    Transform popupAnchor;
    [SerializeField] float popupHeightOffset = 0.5f;
    [SerializeField, Range(0f, 1f)] float critChance = 0.15f;
    [SerializeField] float critMultiplier = 2f;

    [Header("Muerte")]
    [SerializeField] GameObject deathEffectPrefab;
    [SerializeField] float deathFxLifetime = 2f;
    [SerializeField] UnityEvent onDeath;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;

    float currentHealth;

    void Awake() => currentHealth = maxHealth;

    public void TakeDamage(float amount, Vector3 point, Vector3 normal)
    {
        if (currentHealth <= 0f) return;

        bool isCrit = Random.value < critChance;
        float finalDamage = isCrit ? amount * critMultiplier : amount;
        currentHealth -= finalDamage;

        SpawnPopup(finalDamage, isCrit, point);

        // Hook para flash de material, sonido, knockback, IA, etc.
        // Ejemplo: GetComponent<HitFlash>()?.Flash();

        if (currentHealth <= 0f) Die();
    }

    void SpawnPopup(float dmg, bool isCrit, Vector3 hitPoint)
    {
        if (popupPrefab == null) return;

        Vector3 basePos = popupAnchor != null ? popupAnchor.position : hitPoint;
        Vector3 pos = basePos + Vector3.up * popupHeightOffset;

        var popup = Instantiate(popupPrefab, pos, Quaternion.identity);
        popup.Setup(dmg, isCrit);
    }

    void Die()
    {
        onDeath?.Invoke();

        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            Destroy(fx, deathFxLifetime);
        }

        if (destroyOnDeath)
        {
            Destroy(gameObject);
        }
        else
        {
            // Desactivar sin destruir (útil si usas pooling)
            foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = false;
            enabled = false;
        }
    }
}