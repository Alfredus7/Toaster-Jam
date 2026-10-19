using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    // Dormant  = sobre la nave, sin IA ni gravedad, esperando Deploy()
    // Spawning = espera inicial antes de empezar a caer
    // Falling  = cae por gravedad
    // Waking   = ya aterrizó, esperando tiempo fijo de reacción (1 s)
    // Chasing  = IA activa
    // Attacking= subclase decide
    protected enum State { Dormant, Spawning, Falling, Waking, Chasing, Attacking } // *** NUEVO: Dormant ***

    [Header("Objetivo")]
    [SerializeField] protected Transform player;

    [Header("Spawn / Caída")]
    [Tooltip("Tiempo desde el spawn (o Deploy) hasta empezar a caer. Sobrescribible con SetSpawnDelay().")]
    [SerializeField] protected float spawnDelay = 0f;

    [SerializeField] protected LayerMask groundMask = ~0;
    [SerializeField] protected float groundRayDistance = 1.2f;

    [Tooltip("Tiempo de reacción tras aterrizar. Por diseño = 1 s (fijo).")]
    [SerializeField] protected float wakeUpDelay = 1f;

    [Header("Persecución")]
    [SerializeField] protected float stopDistance = 1.5f;
    [SerializeField] protected float repathInterval = 0.2f;
    [SerializeField] protected float maxChaseRange = 50f;

    [Header("Modo Nave (Dormant)")] // *** NUEVO ***
    [Tooltip("Si true, el enemigo arranca dormido (sobre la nave) y no hace nada hasta que la EnemyShip llame a Deploy().")]
    [SerializeField] protected bool startDormant = false;

    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected State state;
    protected float spawnTimer;
    protected float wakeUpTimer;
    protected float repathTimer;

    /// <summary>True mientras espera ser desplegado por una EnemyShip.</summary>
    public bool IsDormant => state == State.Dormant; // *** NUEVO ***

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
            if (startDormant) rb.detectCollisions = false; // *** NUEVO: evita empujones mientras viaja con la nave ***
        }

        // *** CAMBIO: estado inicial según startDormant ***
        if (startDormant)
        {
            state = State.Dormant;
        }
        else
        {
            state = State.Spawning;
            spawnTimer = spawnDelay;
        }
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

    /// <summary>
    /// La EnemyShip llama esto para "soltar" al enemigo.
    /// Se desparenta y arranca el ciclo normal: Spawning → Falling → Waking → Chasing.
    /// Si el enemigo no estaba dormido, no hace nada.
    /// </summary>
    public void Deploy() // *** NUEVO ***
    {
        if (state != State.Dormant) return;

        if (rb != null)
            rb.detectCollisions = true;

        if (transform.parent != null)
            transform.SetParent(null, true);

        state = State.Spawning;
        spawnTimer = spawnDelay;
    }

    protected virtual void Update()
    {
        switch (state)
        {
            case State.Dormant:  /* no hace nada */ break; // *** NUEVO ***
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