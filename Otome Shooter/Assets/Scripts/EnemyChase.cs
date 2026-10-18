using UnityEngine;
using UnityEngine.AI;

public abstract class EnemyBase : MonoBehaviour
{
    protected enum State { Falling, Waking, Chasing, Attacking }

    [Header("Objetivo")]
    [SerializeField] protected Transform player;

    [Header("Spawn / Caída")]
    [SerializeField] protected LayerMask groundMask = ~0;
    [SerializeField] protected float groundRayDistance = 1.2f;
    [SerializeField] protected float wakeUpDelay = 2f;

    [Header("Persecución")]
    [SerializeField] protected float stopDistance = 1.5f;
    [SerializeField] protected float repathInterval = 0.2f;
    [SerializeField] protected float maxChaseRange = 50f;

    protected NavMeshAgent agent;
    protected Rigidbody rb;
    protected State state;
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

        agent.enabled = false;
        rb.isKinematic = false;
        rb.useGravity = true;
        state = State.Falling;
    }

    protected virtual void Update()
    {
        switch (state)
        {
            case State.Falling: CheckLanding(); break;
            case State.Waking: UpdateWaking(); break;
            case State.Chasing: UpdateChasing(); break;
            case State.Attacking: UpdateAttacking(); break;
        }
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

        rb.isKinematic = true;
        agent.enabled = true;
        agent.Warp(transform.position);
        agent.stoppingDistance = stopDistance;
        agent.updateRotation = true;

        state = State.Waking;
        wakeUpTimer = wakeUpDelay;
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

        // La subclase decide si es momento de atacar
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

    // ---------- HOOKS para las subclases ----------
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