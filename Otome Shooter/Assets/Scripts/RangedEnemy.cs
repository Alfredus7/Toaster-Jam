using UnityEngine;

public class RangedEnemy : EnemyBase
{
    [Header("Disparo")]
    [SerializeField] float minShootRange = 4f;
    [SerializeField] float maxShootRange = 15f;
    [SerializeField] float fireCooldown = 1.2f;
    [SerializeField] float aimTime = 0.4f;
    [SerializeField] float inaccuracyDegrees = 2f;
    [SerializeField] GameObject projectilePrefab;
    [SerializeField] Transform muzzle;
    [SerializeField] float projectileSpeed = 20f;
    [SerializeField] float projectileDamage = 15f;
    [SerializeField] LayerMask lineOfSightMask = 1;

    [Header("Debug")]
    [SerializeField] bool debugLogs = true;

    float nextFireTime;

    protected override bool CanAttack(float dist)
    {
        if (player == null)
        {
            if (debugLogs) Debug.LogWarning($"[{name}] CanAttack: player == null");
            return false;
        }

        if (dist < minShootRange)
        {
            if (debugLogs) Debug.Log($"[{name}] CanAttack=false: demasiado cerca ({dist:F1} < {minShootRange})");
            return false;
        }

        if (dist > maxShootRange)
        {
            if (debugLogs) Debug.Log($"[{name}] CanAttack=false: demasiado lejos ({dist:F1} > {maxShootRange})");
            return false;
        }

        if (!HasLineOfSight())
        {
            if (debugLogs) Debug.Log($"[{name}] CanAttack=false: sin línea de visión");
            return false;
        }

        if (debugLogs) Debug.Log($"[{name}] CanAttack=true (dist={dist:F1})");
        return true;
    }

    protected override void OnAttackStart()
    {
        nextFireTime = Time.time + aimTime;
        if (debugLogs) Debug.Log($"[{name}] OnAttackStart — apuntando {aimTime}s");
    }

    protected override void UpdateAttacking()
    {
        if (player == null) { ReturnToChasing(); return; }

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist > maxShootRange + 2f)
        {
            if (debugLogs) Debug.Log($"[{name}] UpdateAttacking → sale: fuera de rango extendido ({dist:F1})");
            ReturnToChasing();
            return;
        }

        if (!HasLineOfSight())
        {
            if (debugLogs) Debug.Log($"[{name}] UpdateAttacking → sale: perdió LOS");
            ReturnToChasing();
            return;
        }

        FacePlayer(Time.deltaTime * 8f);

        if (dist < minShootRange)
        {
            if (debugLogs) Debug.Log($"[{name}] UpdateAttacking: dentro de minRange ({dist:F1}), no dispara");
            return;
        }

        if (Time.time < nextFireTime)
        {
            if (debugLogs) Debug.Log($"[{name}] UpdateAttacking: enfriando, faltan {nextFireTime - Time.time:F2}s");
            return;
        }

        if (debugLogs) Debug.Log($"[{name}] ¡DISPARANDO! dist={dist:F1}");
        FireProjectile();
        nextFireTime = Time.time + fireCooldown;
    }

    protected override void HandleChaseMovement(float dist)
    {
        if (dist < minShootRange && player != null)
        {
            Vector3 away = transform.position - player.position;
            away.y = 0f;
            if (away.sqrMagnitude < 0.001f) away = -transform.forward;

            Vector3 retreatTarget = transform.position + away.normalized * (minShootRange + 1f);

            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(retreatTarget);

            if (debugLogs) Debug.Log($"[{name}] Retrocediendo a {retreatTarget}");
            return;
        }

        base.HandleChaseMovement(dist);
    }

    bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector3 from = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;
        Vector3 to = player.position + Vector3.up * 1f;

        bool blocked = Physics.Linecast(from, to, out RaycastHit hit,
                                        lineOfSightMask, QueryTriggerInteraction.Ignore);

        if (blocked && debugLogs)
            Debug.Log($"[{name}] LOS bloqueado por '{hit.collider.name}' en capa '{LayerMask.LayerToName(hit.collider.gameObject.layer)}'");

        return !blocked;
    }

    void FireProjectile()
    {
        if (projectilePrefab == null)
        {
            Debug.LogError($"[{name}] ¡projectilePrefab es NULL! Asignalo en el inspector.");
            return;
        }

        Vector3 spawnPos = muzzle != null ? muzzle.position : transform.position + Vector3.up * 1.5f;
        Vector3 targetPos = player.position + Vector3.up * 1f;
        Vector3 dir = (targetPos - spawnPos).normalized;

        if (inaccuracyDegrees > 0f)
        {
            dir = Quaternion.Euler(
                Random.Range(-inaccuracyDegrees, inaccuracyDegrees),
                Random.Range(-inaccuracyDegrees, inaccuracyDegrees),
                0f) * dir;
        }

        if (debugLogs) Debug.Log($"[{name}] Instanciando proyectil en {spawnPos}, dir={dir}");

        GameObject go = Instantiate(projectilePrefab, spawnPos, Quaternion.LookRotation(dir));

        if (go.TryGetComponent<Projectile>(out var proj))
        {
            proj.Init(dir * projectileSpeed, projectileDamage, gameObject);
            if (debugLogs) Debug.Log($"[{name}] Proyectil inicializado OK.");
        }
        else
        {
            Debug.LogError($"[{name}] ¡El prefab '{projectilePrefab.name}' NO tiene el componente 'Projectile'!");
        }
    }

    void FacePlayer(float speed)
    {
        if (player == null) return;
        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(dir), speed);
    }


    // -----------------------------------------------------------------
    // GIZMOS
    // -----------------------------------------------------------------
    void OnDrawGizmosSelected()
    {
        // --- Radios de disparo ---
        // Min: por dentro de este radio retrocede
        Gizmos.color = new Color(0f, 1f, 1f, 0.9f);   // cyan
        Gizmos.DrawWireSphere(transform.position, minShootRange);

        // Max: fuera de este radio deja de disparar y persigue
        Gizmos.color = new Color(1f, 1f, 0f, 0.9f);   // amarillo
        Gizmos.DrawWireSphere(transform.position, maxShootRange);

        // --- Zona efectiva de disparo (anillo entre min y max) ---
        // La pintamos con un disco translúcido para que se lea rápido
        Gizmos.color = new Color(0f, 1f, 0f, 0.05f);
        Gizmos.DrawSphere(transform.position, maxShootRange);

        // --- Origen real del disparo (muzzle o fallback) ---
        Vector3 origin = muzzle != null
            ? muzzle.position
            : transform.position + Vector3.up * 1.5f;

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(origin, 0.08f);

        // --- Dirección de disparo (forward del muzzle) ---
        Vector3 shootDir = muzzle != null ? muzzle.forward : transform.forward;

        Gizmos.color = Color.red;
        Gizmos.DrawRay(origin, shootDir * maxShootRange);

        // --- Cono de imprecisión en el extremo del alcance ---
        if (inaccuracyDegrees > 0f)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // naranja translúcido
            Vector3 coneTip = origin + shootDir * maxShootRange;
            float radius = Mathf.Tan(inaccuracyDegrees * Mathf.Deg2Rad) * maxShootRange;
            DrawGizmoCircle(coneTip, shootDir, radius);

            // Líneas laterales del cono
            Gizmos.DrawLine(origin, coneTip + Vector3.up * radius);
            Gizmos.DrawLine(origin, coneTip - Vector3.up * radius);
        }

        // --- Línea al jugador (LOS en vivo) ---
        if (player != null)
        {
            Vector3 target = player.position + Vector3.up * 1f;
            bool los = !Physics.Linecast(origin, target,
                                         lineOfSightMask,
                                         QueryTriggerInteraction.Ignore);

            Gizmos.color = los ? Color.green : Color.red;
            Gizmos.DrawLine(origin, target);

            // Marcar la cabeza del jugador para saber dónde apuntás
            Gizmos.color = los ? Color.green : Color.red;
            Gizmos.DrawWireSphere(target, 0.2f);

            // Distancia al jugador (línea de referencia)
            float dist = Vector3.Distance(transform.position, player.position);
            bool inRange = dist >= minShootRange && dist <= maxShootRange;
            Gizmos.color = inRange ? Color.green : new Color(1f, 0.3f, 0.3f);
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    // Helper: dibuja un círculo 3D orientado con 'normal' alrededor de 'center'
    static void DrawGizmoCircle(Vector3 center, Vector3 normal, float radius)
    {
        Vector3 right = Vector3.Cross(normal, Vector3.up);
        if (right.sqrMagnitude < 0.001f)
            right = Vector3.right;
        right.Normalize();
        Vector3 up = Vector3.Cross(right, normal).normalized;

        const int segments = 32;
        Vector3 prev = center + right * radius;
        for (int i = 1; i <= segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}