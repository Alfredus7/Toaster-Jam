using UnityEngine;

public interface IDamageable
{
    void TakeDamage(float amount, Vector3 point, Vector3 normal);
}

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 30f;
    [SerializeField] float bulletLifetime = 3f;
    [SerializeField] float damage = 10f;
    [SerializeField] GameObject impactEffectPrefab;
    [SerializeField] float impactFxLifetime = 2f;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;                                              // ← CRÍTICO
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // ← anti-tunneling
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Start() => Destroy(gameObject, bulletLifetime);

    public void Initialize(Vector3 shootDirection)
    {
        if (rb != null) rb.linearVelocity = shootDirection * bulletSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        ContactPoint cp = collision.contacts[0];

        if (collision.collider.TryGetComponent(out IDamageable dmg))
            dmg.TakeDamage(damage, cp.point, cp.normal);

        if (impactEffectPrefab != null)
        {
            var fx = Instantiate(impactEffectPrefab, cp.point, Quaternion.LookRotation(cp.normal));
            Destroy(fx, impactFxLifetime);   // ← fix fuga
        }

        Destroy(gameObject);
    }
}