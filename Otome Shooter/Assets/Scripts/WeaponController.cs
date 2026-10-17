using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    [SerializeField, Tooltip("Prefab de la bala o proyectil que se va a disparar.")]
    GameObject bulletPrefab;

    [SerializeField, Tooltip("Punta del arma (Transform hijo que marca el inicio del disparo).")]
    Transform muzzlePoint;

    [SerializeField, Tooltip("Velocidad del proyectil.")]
    float bulletSpeed = 30f;

    [SerializeField, Tooltip("Tiempo de espera entre disparos (Cadencia).")]
    float fireRate = 0.2f;

    [SerializeField, Tooltip("Tiempo en segundos antes de que la bala se destruya automáticamente.")]
    float bulletLifetime = 3f;

    [Header("Configuración de Retroceso (Recoil)")]
    [SerializeField, Tooltip("Intensidad general del retroceso (desplazamiento e inclinación).")]
    float recoilStrength = 1f;

    [SerializeField, Tooltip("Ajuste de velocidad de recuperación ligado a la cadencia. Mayor valor = el arma vuelve más rápido a su posición.")]
    float recoilRecoveryMultiplier = 5f;

    [Header("Físicas e Inercia")]
    [SerializeField, Tooltip("Si está activo, la bala sumará la velocidad de movimiento del jugador.")]
    bool inheritPlayerVelocity = true;

    [Header("Gizmos y Visualización")]
    [SerializeField] bool showGizmos = true;
    [SerializeField, Tooltip("Longitud del rayo de guía en la ventana Scene.")]
    float gizmoLength = 2f;

    // Variables internas de retroceso
    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;
    Vector3 currentRecoilPosition;
    Quaternion currentRecoilRotation = Quaternion.identity;

    // Referencias
    Rigidbody playerRb;
    Collider[] playerColliders;
    float nextFireTime = 0f;

    void Awake()
    {
        playerRb = GetComponentInParent<Rigidbody>();
        playerColliders = GetComponentsInParent<Collider>();

        // Guardamos la posición y rotación local inicial del arma
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void Update()
    {
        // Calculamos dinámicamente la velocidad de recuperación en función de la cadencia (fireRate).
        // Si reduces el fireRate (dispara más rápido), la velocidad de recuperación aumenta automáticamente.
        float dynamicReturnSpeed = (1f / Mathf.Max(fireRate, 0.001f)) * recoilRecoveryMultiplier;

        // Interpolar suavemente el retroceso de vuelta a cero
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, dynamicReturnSpeed * Time.deltaTime);
        currentRecoilRotation = Quaternion.Slerp(currentRecoilRotation, Quaternion.identity, dynamicReturnSpeed * Time.deltaTime);

        // Aplicar el resultado al transform local del arma
        transform.localPosition = originalLocalPosition + currentRecoilPosition;
        transform.localRotation = originalLocalRotation * currentRecoilRotation;
    }

    public void OnAttack(InputValue value)
    {
        if (value.isPressed)
        {
            TryShoot();
        }
    }

    void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        // Direcciones base del retroceso escaladas por la fuerza general
        Vector3 basePositionKick = new Vector3(0f, 0f, -0.05f);
        Vector3 baseRotationKick = new Vector3(-10f, 0f, 0f);

        currentRecoilPosition += basePositionKick * recoilStrength;
        currentRecoilRotation *= Quaternion.Euler(baseRotationKick * recoilStrength);

        if (bulletPrefab != null && muzzlePoint != null)
        {
            // Instanciar bala
            GameObject bullet = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);
            Destroy(bullet, bulletLifetime);

            // Físicas de la bala
            Rigidbody rbBullet = bullet.GetComponent<Rigidbody>();
            if (rbBullet != null)
            {
                Vector3 shootVelocity = muzzlePoint.forward * bulletSpeed;

                if (inheritPlayerVelocity && playerRb != null)
                {
                    shootVelocity += playerRb.linearVelocity;
                }

                rbBullet.linearVelocity = shootVelocity;
            }

            // Ignorar colisión con el jugador
            Collider bulletCollider = bullet.GetComponent<Collider>();
            if (bulletCollider != null && playerColliders != null)
            {
                foreach (Collider playerCol in playerColliders)
                {
                    Physics.IgnoreCollision(bulletCollider, playerCol, true);
                }
            }
        }
        else
        {
            Debug.LogWarning("Falta asignar el Prefab de la bala o el Muzzle Point en el WeaponController.");
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || muzzlePoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * gizmoLength);
        Gizmos.DrawWireSphere(muzzlePoint.position, 0.05f);
    }
}