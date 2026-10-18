using System.Collections.Generic;
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
    [SerializeField, Tooltip("Radio de la esfera del disparo. 0 = Raycast fino. 0.05–0.15 es lo típico.")]
    float hitRadius = 0.1f;
    [SerializeField] GameObject impactEffectPrefab;
    [SerializeField] float impactFxLifetime = 2f;
    [SerializeField] bool drawDebugRay = true;

    [Header("Multi-hit")]
    [SerializeField, Tooltip("0 = sin límite. >0 = máximo de enemigos dañados por disparo.")]
    int maxTargetsPerShot = 3;
    [SerializeField, Tooltip("Si está activo, sigue dañando después de atravesar obstáculos.")]
    bool pierceWalls = false;

    [Header("Recoil")]
    [SerializeField] float recoilStrength = 1f;
    [SerializeField] float recoilRecoveryMultiplier = 5f;
    [SerializeField] float maxPitch = 20f;

    [Header("Gizmos")]
    [SerializeField] bool showGizmos = true;
    [SerializeField] float gizmoLength = 2f;
    [SerializeField, Tooltip("Duración en segundos de la traza roja del último disparo.")]
    float shotTraceDuration = 0.5f;

    // ─────────── Estado de recoil ───────────
    Vector3 originalLocalPosition;
    Quaternion originalLocalRotation;
    Vector3 currentRecoilPosition;
    float currentRecoilPitch;

    float nextFireTime;

    // Buffer reutilizable para evitar allocations por disparo
    readonly HashSet<IDamageable> alreadyDamaged = new HashSet<IDamageable>();

    // ─────────── Cache para gizmos (runtime) ───────────
    Vector3 lastShotOrigin;
    Vector3 lastShootDir;
    Vector3 lastDrawEnd;
    Vector3 lastAimPoint;
    bool lastAimHitSomething;
    float lastShotTime = -999f;

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

    // ───────────────────────────────────────────────
    // DISPARO
    // ───────────────────────────────────────────────
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

        // Muzzle flash
        if (muzzleEffectPrefab != null)
        {
            var fx = Instantiate(muzzleEffectPrefab, muzzlePoint.position, muzzlePoint.rotation, muzzlePoint);
            Destroy(fx, muzzleFxLifetime);
        }

        Vector3 shootDir = GetAimDirection();
        Vector3 origin = muzzlePoint.position;

        // Cache para gizmos
        lastShotOrigin = origin;
        lastShootDir = shootDir;
        lastShotTime = Time.time;

        // ─── Recolectar TODOS los hits ───
        RaycastHit[] hits = hitRadius > 0f
            ? Physics.SphereCastAll(origin, hitRadius, shootDir,
                                    shootRange, shootMask, QueryTriggerInteraction.Ignore)
            : Physics.RaycastAll(origin, shootDir,
                                 shootRange, shootMask, QueryTriggerInteraction.Ignore);

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        // ─── Procesar hits en orden ───
        alreadyDamaged.Clear();
        int damagedCount = 0;
        Vector3 drawEnd = origin + shootDir * shootRange;

        foreach (var hit in hits)
        {
            Vector3 point = hit.distance > 0f ? hit.point : origin;

            var dmg = hit.collider.GetComponentInParent<IDamageable>();
            if (dmg != null && dmg.IsAlive)
            {
                if (alreadyDamaged.Add(dmg))
                {
                    dmg.TakeDamage(damage, point, hit.normal);
                    SpawnImpactFx(point, hit.normal);

                    damagedCount++;
                    drawEnd = point;

                    if (maxTargetsPerShot > 0 && damagedCount >= maxTargetsPerShot)
                        break;
                }
                continue; // enemigos no cortan el disparo
            }

            SpawnImpactFx(point, hit.normal);
            drawEnd = point;

            if (!pierceWalls) break;
        }

        lastDrawEnd = drawEnd;

        if (drawDebugRay)
            Debug.DrawLine(origin, drawEnd, Color.yellow, 0.5f);
    }

    // ───────────────────────────────────────────────
    // AIM
    // ───────────────────────────────────────────────
    Vector3 GetAimDirection()
    {
        if (cameraTransform == null)
        {
            lastAimHitSomething = false;
            lastAimPoint = muzzlePoint.position + muzzlePoint.forward * aimRange;
            return muzzlePoint.forward;
        }

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, aimRange, aimMask, QueryTriggerInteraction.Ignore))
        {
            lastAimPoint = hit.point;
            lastAimHitSomething = true;
        }
        else
        {
            lastAimPoint = ray.GetPoint(aimRange);
            lastAimHitSomething = false;
        }

        Vector3 dir = lastAimPoint - muzzlePoint.position;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : muzzlePoint.forward;
    }

    void SpawnImpactFx(Vector3 point, Vector3 normal)
    {
        if (impactEffectPrefab == null) return;
        var fx = Instantiate(impactEffectPrefab, point, Quaternion.LookRotation(normal));
        Destroy(fx, impactFxLifetime);
    }

    // ───────────────────────────────────────────────
    // GIZMOS
    // ───────────────────────────────────────────────
    void OnDrawGizmos()
    {
        if (!showGizmos || muzzlePoint == null) return;

        Vector3 mPos = muzzlePoint.position;
        Vector3 mFwd = muzzlePoint.forward;

        // ── 1) Muzzle (punto de origen) ──
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(mPos, 0.08f);

        // ── 2) Dirección del muzzle SIN aim (cyan) ──
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(mPos, mFwd * gizmoLength);

        // ── 3) Radio del spherecast (naranja translúcido) ──
        if (hitRadius > 0f)
        {
            Vector3 tip = mPos + mFwd * gizmoLength;

            Gizmos.color = new Color(1f, 0.6f, 0f, 0.30f);
            Gizmos.DrawWireSphere(tip, hitRadius);

            Gizmos.DrawLine(mPos + muzzlePoint.up * hitRadius, tip + muzzlePoint.up * hitRadius);
            Gizmos.DrawLine(mPos - muzzlePoint.up * hitRadius, tip - muzzlePoint.up * hitRadius);
        }

        // ── 4) Aim: cámara + aimPoint + convergencia ──
        if (cameraTransform != null)
        {
            Vector3 camPos = cameraTransform.position;
            Vector3 camFwd = cameraTransform.forward;

            // Cámara (cubo blanco) + rayo de vista hasta aimRange
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(camPos, Vector3.one * 0.12f);
            Gizmos.DrawRay(camPos, camFwd * aimRange);

            // Recalcular aimPoint (en runtime usamos el cache; en editor hacemos raycast)
            Vector3 aimPoint;
            bool hitSomething;

            if (Application.isPlaying)
            {
                aimPoint = lastAimPoint;
                hitSomething = lastAimHitSomething;
            }
            else
            {
                Ray camRay = new Ray(camPos, camFwd);
                if (Physics.Raycast(camRay, out RaycastHit h, aimRange, aimMask, QueryTriggerInteraction.Ignore))
                {
                    aimPoint = h.point;
                    hitSomething = true;
                }
                else
                {
                    aimPoint = camRay.GetPoint(aimRange);
                    hitSomething = false;
                }
            }

            // Esfera del aimPoint: verde = impacto real, rojo = punto inventado
            Gizmos.color = hitSomething ? Color.green : Color.red;
            Gizmos.DrawWireSphere(aimPoint, 0.15f);

            // Línea de convergencia: muzzle → aimPoint (verde)
            Gizmos.color = new Color(0f, 1f, 0.5f, 0.85f);
            Gizmos.DrawLine(mPos, aimPoint);

            // Dirección REAL del disparo (amarillo) = normalize(aimPoint - muzzle)
            Vector3 shootDir = (aimPoint - mPos).sqrMagnitude > 0.0001f
                ? (aimPoint - mPos).normalized
                : mFwd;

            Gizmos.color = new Color(1f, 1f, 0f, 0.9f);
            Gizmos.DrawRay(mPos, shootDir * gizmoLength);

            // Si el aim corrige, mostrar el ángulo entre cyan (muzzle) y amarillo (aim)
            float angle = Vector3.Angle(mFwd, shootDir);
            if (angle > 0.25f)
            {
                // Pequeño arco visual: dibujamos un segmento que une los tips de los dos rayos
                Gizmos.color = new Color(1f, 0f, 0.5f, 0.6f);
                Vector3 tipA = mPos + mFwd * gizmoLength;
                Vector3 tipB = mPos + shootDir * gizmoLength;
                Gizmos.DrawLine(tipA, tipB);
            }
        }

        // ── 5) Traza del último disparo (roja, solo en play) ──
        if (Application.isPlaying && Time.time - lastShotTime < shotTraceDuration)
        {
            float a = 1f - Mathf.Clamp01((Time.time - lastShotTime) / shotTraceDuration);

            Gizmos.color = new Color(1f, 0f, 0f, a);
            Gizmos.DrawLine(lastShotOrigin, lastDrawEnd);
            Gizmos.DrawWireSphere(lastDrawEnd, 0.10f);
            Gizmos.DrawWireSphere(lastShotOrigin, 0.06f);
        }
    }
}