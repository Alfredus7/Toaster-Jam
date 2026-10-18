using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Disparo")]
    [SerializeField] Transform muzzlePoint;
    [SerializeField] Transform cameraTransform;
    [SerializeField] GameObject muzzleEffectPrefab;
    [SerializeField, Tooltip("Segundos entre disparos.")] float fireRate = 0.2f;
    [SerializeField, Tooltip("Alcance del raycast de aim (crosshair).")] float aimRange = 200f;
    [SerializeField] LayerMask aimMask = ~0;
    [SerializeField, Tooltip("Lifetime del muzzle flash.")] float muzzleFxLifetime = 1f;

    [Header("Hitscan")]
    [SerializeField, Tooltip("Alcance máximo del disparo.")] float shootRange = 200f;
    [SerializeField, Tooltip("Capas que el disparo puede impactar.")] LayerMask shootMask = ~0;
    [SerializeField] float damage = 10f;
    [SerializeField] GameObject impactEffectPrefab;
    [SerializeField] float impactFxLifetime = 2f;
    [SerializeField] bool drawDebugRay = true;

    [Header("Recoil")]
    [SerializeField] float recoilStrength = 1f;
    [SerializeField] float recoilRecoveryMultiplier = 5f;
    [SerializeField] float maxPitch = 20f;

    [Header("Gizmos")]
    [SerializeField] bool showGizmos = true;
    [SerializeField] float gizmoLength = 2f;

    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;
    Vector3 currentRecoilPosition;
    float currentRecoilPitch;

    float nextFireTime;

    void Awake()
    {
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        float k = (1f / Mathf.Max(fireRate, 0.001f)) * recoilRecoveryMultiplier;
        float t = 1f - Mathf.Exp(-k * Time.deltaTime);
        currentRecoilPosition = Vector3.Lerp(currentRecoilPosition, Vector3.zero, t);
        currentRecoilPitch = Mathf.Lerp(currentRecoilPitch, 0f, t);

        transform.localPosition = originalLocalPosition + currentRecoilPosition;
        transform.localRotation = originalLocalRotation * Quaternion.Euler(currentRecoilPitch, 0f, 0f);
    }

    public void OnAttack(InputValue value) => TryShoot();

    void TryShoot()
    {
        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        // Kick de recoil
        currentRecoilPosition += new Vector3(0f, 0f, -0.05f) * recoilStrength;
        currentRecoilPitch = Mathf.Max(currentRecoilPitch - 10f * recoilStrength, -maxPitch);
        currentRecoilPosition = Vector3.ClampMagnitude(currentRecoilPosition, 0.2f);

        if (muzzlePoint == null)
        {
            Debug.LogWarning("Falta MuzzlePoint.");
            return;
        }

        // Muzzle flash (autodestruible)
        if (muzzleEffectPrefab != null)
        {
            var fx = Instantiate(muzzleEffectPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(fx, muzzleFxLifetime);
        }

        // Dirección: cámara → crosshair → punto → desde el cañón
        Vector3 shootDir = GetAimDirection();
        Vector3 origin = muzzlePoint.position;

        // Hitscan: un solo raycast desde el cañón
        if (Physics.Raycast(origin, shootDir, out RaycastHit hit,
                            shootRange, shootMask, QueryTriggerInteraction.Ignore))
        {
            if (drawDebugRay)
                Debug.DrawLine(origin, hit.point, Color.yellow, 0.5f);

            // Daño
            if (hit.collider.TryGetComponent(out IDamageable dmg))
                dmg.TakeDamage(damage, hit.point, hit.normal);

            // Efecto de impacto SOLO donde choca el rayo
            if (impactEffectPrefab != null)
            {
                var fx = Instantiate(impactEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                Destroy(fx, impactFxLifetime);
            }
        }
        else if (drawDebugRay)
        {
            Debug.DrawLine(origin, origin + shootDir * shootRange, Color.red, 0.5f);
        }
    }

    Vector3 GetAimDirection()
    {
        if (cameraTransform == null) return muzzlePoint.forward;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        Vector3 aimPoint = Physics.Raycast(ray, out RaycastHit hit, aimRange, aimMask)
            ? hit.point
            : ray.GetPoint(aimRange);

        Vector3 dir = aimPoint - muzzlePoint.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : muzzlePoint.forward;
    }

    void OnDrawGizmos()
    {
        if (!showGizmos || muzzlePoint == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawRay(muzzlePoint.position, muzzlePoint.forward * gizmoLength);
        Gizmos.DrawWireSphere(muzzlePoint.position, 0.05f);
    }
}