using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    [SerializeField] float lifetime = 5f;
    [SerializeField] bool debugLogs = true;

    Vector3 direction;
    float speed;
    float damage;
    GameObject owner;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        GetComponent<Collider>().isTrigger = true;

        if (debugLogs) Debug.Log($"[Projectile] Awake en {name}");
    }

    public void Init(Vector3 velocity, float dmg, GameObject shooter)
    {
        direction = velocity.normalized;
        speed = velocity.magnitude;
        damage = dmg;
        owner = shooter;
        Destroy(gameObject, lifetime);

        if (debugLogs)
            Debug.Log($"[Projectile] Init: dir={direction}, speed={speed}, dmg={damage}, owner={owner?.name}");
    }

    void FixedUpdate()
    {
        rb.linearVelocity = direction * speed;
    }

    void OnTriggerEnter(Collider other)
    {
        if (owner != null &&
            (other.gameObject == owner || other.transform.IsChildOf(owner.transform)))
        {
            if (debugLogs) Debug.Log($"[Projectile] ignorando al dueño ({other.name})");
            return;
        }

        if (debugLogs) Debug.Log($"[Projectile] impactó '{other.name}' (capa: {LayerMask.LayerToName(other.gameObject.layer)})");

        if (other.TryGetComponent<IDamageable>(out var dmg) && dmg.IsAlive)
            dmg.TakeDamage(damage, transform.position, -direction);

        Destroy(gameObject);
    }
}