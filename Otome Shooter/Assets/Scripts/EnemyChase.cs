using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    // Spawning = espera inicial antes de empezar a caer
    // Falling  = cae por gravedad
    // Waking   = ya aterrizó, esperando tiempo fijo de reacción (1 s)
    // Chasing  = IA activa
    // Attacking= subclase decide
    protected enum State { Spawning, Falling, Waking, Chasing, Attacking }

    [Header("Objetivo")]
    [SerializeField] protected Transform player;

    [Header("Spawn / Caída")]
    [Tooltip("Tiempo desde el spawn hasta empezar a caer. Sobrescribible con SetSpawnDelay().")]
    [SerializeField] protected float spawnDelay = 0f;

    [SerializeField] protected LayerMask groundMask = ~0;
    [SerializeField] protected float groundRayDistance = 1.2f;

    [Tooltip("Tiempo de reacción tras aterrizar. Por diseño = 1 s (fijo).")]
    [SerializeField] protected float wakeUpDelay = 1f;

    [Header("Persecución")]
    [SerializeField] protected float stopDistance = 1.5f;
    [SerializeField] protected float repathInterval = 0.2f;
    [SerializeField] protected float maxChaseRange = 50f;

    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected State state;
    protected float spawnTimer;
    protected float wakeUpTimer;
    protected float repathTimer;

    protected virtual void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        // --- Estado inicial: quieto, sin gravedad, IA desactivada ---
        agent.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;   // congelado durante la espera
            rb.useGravity = false;
        }

        state = State.Spawning;
        spawnTimer = spawnDelay;
    }

    /// <summary>
    /// Permite sobrescribir el delay de spawn justo después de Instantiate,
    /// antes del primer Update. Ej:  enemy.SetSpawnDelay(Random.Range(0.5f, 2f));
    /// </summary>
    public void SetSpawnDelay(float delay)
    {
        spawnDelay = Mathf.Max(0f, delay);
        if (state == State.Spawning)
            spawnTimer = spawnDelay;
    }

    protected virtual void Update()
    {
        switch (state)
        {
            case State.Spawning: UpdateSpawning(); break;
            case State.Falling: CheckLanding(); break;
            case State.Waking: UpdateWaking(); break;
            case State.Chasing: UpdateChasing(); break;
            case State.Attacking: UpdateAttacking(); break;
        }
    }

    // ---------- FASE: SPAWNING (espera inicial) ----------
    void UpdateSpawning()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        // Se terminó la espera → soltar para que caiga
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }
        state = State.Falling;
    }

    // ---------- FASE: CAYENDO ----------
    void CheckLanding()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            Land(hit.point);
    }

    void Land(Vector3 groundPoint)
    {
        if (NavMesh.SamplePosition(groundPoint, out NavMeshHit navHit, 2f, NavMesh.AllAreas))
            transform.position = navHit.position;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        agent.enabled = true;
        agent.Warp(transform.position);
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;
        if (agent.isStopped) agent.isStopped = true; // quieto durante el wake up

        state = State.Waking;
        wakeUpTimer = wakeUpDelay; // ← 1 segundo fijo de reacción
    }

    // ---------- FASE: DESPERTANDO ----------
    void UpdateWaking()
    {
        wakeUpTimer -= Time.deltaTime;
        if (wakeUpTimer > 0f) return;

        if (!agent.isOnNavMesh) ReturnToNavMesh();
        if (agent.isOnNavMesh && agent.isStopped) agent.isStopped = false;

        state = State.Chasing;
        OnAwakened();
    }

    // ---------- FASE: PERSIGUIENDO ----------
    void UpdateChasing()
    {
        if (player == null || !agent.isOnNavMesh) return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (CanAttack(dist))
        {
            state = State.Attacking;
            if (!agent.isStopped) agent.isStopped = true;
            OnAttackStart();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathInterval;

        HandleChaseMovement(dist);
    }

    // ---------- HOOKS para subclases ----------
    protected virtual bool CanAttack(float distanceToPlayer) => false;
    protected virtual void OnAttackStart() { }
    protected virtual void UpdateAttacking() { }
    protected virtual void OnAttackEnd() { }
    protected virtual void OnAwakened() { }

    protected virtual void HandleChaseMovement(float dist)
    {
        if (dist <= maxChaseRange)
        {
            if (agent.isStopped) agent.isStopped = false;
            agent.SetDestination(player.position);
        }
        else if (!agent.isStopped) agent.isStopped = true;
    }

    protected void ReturnToChasing()
    {
        OnAttackEnd();
        state = State.Chasing;
        if (agent.isOnNavMesh && agent.isStopped) agent.isStopped = false;
    }

    void ReturnToNavMesh()
    {
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit navHit, 3f, NavMesh.AllAreas))
            agent.Warp(navHit.position);
    }
}