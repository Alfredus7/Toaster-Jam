using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Configuración de Disparo")]
    [SerializeField, Tooltip("Prefab de la bala (debe tener Bullet, Rigidbody y Collider).")]
    GameObject bulletPrefab;

    [SerializeField, Tooltip("Punta del arma (Transform hijo que marca el inicio del disparo).")]
    Transform muzzlePoint;

    [SerializeField, Tooltip("Efecto visual o de partículas al disparar (Fogonazo/Muzzle Flash).")]
    GameObject muzzleEffectPrefab;

    [SerializeField, Tooltip("Tiempo de espera entre disparos (Cadencia).")]
    float fireRate = 0.2f;

    [Header("Configuración de Retroceso (Recoil)")]
    [SerializeField, Tooltip("Intensidad general del retroceso (desplazamiento e inclinación).")]
    float recoilStrength = 1f;

    [SerializeField, Tooltip("Ajuste de velocidad de recuperación ligado a la cadencia. Mayor valor = el arma vuelve más rápido a su posición.")]
    float recoilRecoveryMultiplier = 5f;

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
    Collider[] playerColliders;
    float nextFireTime = 0f;

    void Awake()
    {
        playerColliders = GetComponentsInParent<Collider>();

        // Guardamos la posición y rotación local inicial del arma
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
    }

    void Update()
    {
        // Cálculo dinámico de la velocidad de recuperación basado en la cadencia
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

        // Aplicar el impulso brusco del retroceso
        Vector3 basePositionKick = new Vector3(0f, 0f, -0.05f);
        Vector3 baseRotationKick = new Vector3(-10f, 0f, 0f);

        currentRecoilPosition += basePositionKick * recoilStrength;
        currentRecoilRotation *= Quaternion.Euler(baseRotationKick * recoilStrength);

        if (bulletPrefab != null && muzzlePoint != null)
        {
            // 1. Instanciar el efecto de fogonazo COMO HIJO del muzzlePoint
            if (muzzleEffectPrefab != null)
            {
                // El cuarto parámetro (muzzlePoint) asigna este objeto directamente como su hijo
                GameObject muzzleEffect = Instantiate(muzzleEffectPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            }
            //aqui quiero que la bala use el forward del arma como direcion
            // 2. Instanciar la bala
            GameObject bulletObj = Instantiate(bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

            // 3. Ignorar colisión inicial entre la bala y los colliders del jugador
            Collider bulletCollider = bulletObj.GetComponent<Collider>();
            if (bulletCollider != null && playerColliders != null)
            {
                foreach (Collider playerCol in playerColliders)
                {
                    Physics.IgnoreCollision(bulletCollider, playerCol, true);
                }
            }

            // 4. Inicializar el movimiento independiente de la bala
            Bullet bulletScript = bulletObj.GetComponent<Bullet>();
            if (bulletScript != null)
            {
                bulletScript.Initialize(muzzlePoint.forward);
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