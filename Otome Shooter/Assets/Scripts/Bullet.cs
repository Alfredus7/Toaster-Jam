using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("Configuración de la Bala")]
    [SerializeField, Tooltip("Velocidad con la que sale disparada la bala.")]
    float bulletSpeed = 30f;

    [SerializeField, Tooltip("Tiempo en segundos antes de que la bala se destruya automáticamente si no choca con nada.")]
    float bulletLifetime = 3f;

    [Header("Efecto de Impacto")]
    [SerializeField, Tooltip("Efecto visual o de partículas opcional al impactar.")]
    GameObject impactEffectPrefab;

    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        // Destruir la bala automáticamente después del tiempo de vida
        Destroy(gameObject, bulletLifetime);
    }

    /// <summary>
    /// Inicializa el movimiento de la bala. Llamado desde el WeaponController.
    /// </summary>
    public void Initialize(Vector3 shootDirection)
    {
        if (rb != null)
        {
            // Velocidad pura basada únicamente en la dirección y la velocidad configurada
            rb.linearVelocity = shootDirection * bulletSpeed;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("Bala impactó contra: " + collision.gameObject.name);

        // Instanciar efecto de impacto si está asignado
        if (impactEffectPrefab != null)
        {
            ContactPoint contact = collision.contacts[0];
            Instantiate(impactEffectPrefab, contact.point, Quaternion.LookRotation(contact.normal));
        }

        // Destruir la bala al chocar
        Destroy(gameObject);
    }
}