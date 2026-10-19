using UnityEngine;

/// <summary>
/// Nave que transporta enemigos dormidos como hijos.
/// Espera 'setSailDelay', vuela en LÍNEA RECTA hacia un punto fijo (target)
/// y, al llegar, suelta los enemigos con Deploy().
/// Tras soltar todo, sigue volando hacia adelante y se destruye cuando
/// ya pasó un tiempo mínimo y quedó fuera de vista del jugador.
/// Los enemigos ya existen en la escena desde el frame 0 → sin conflictos con PlayerUIManager.
/// </summary>
public class EnemyShip : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Punto fijo en el mundo donde la nave debe llegar para soltar los enemigos.")]
    [SerializeField] Transform target;

    [Header("Zarpe")]
    [Tooltip("Segundos desde el inicio de la escena hasta que esta nave zarpa. " +
             "Ej: 2 / 20 / 30 / 45 para oleadas escalonadas.")]
    [SerializeField] float setSailDelay = 2f;

    [Header("Movimiento")]
    [Tooltip("Velocidad constante hacia el target (m/s).")]
    [SerializeField] float moveSpeed = 8f;

    [Tooltip("Distancia horizontal al target que cuenta como 'llegó'.")]
    [SerializeField] float arriveDistance = 0.5f;

    [Tooltip("Si true, la nave gira para mirar hacia el target al arrancar (una sola vez).")]
    [SerializeField] bool faceTargetOnStart = true;

    [Header("Despliegue")]
    [Tooltip("Enemigos hijos. Si se deja vacío, se autocompleta con GetComponentsInChildren.")]
    [SerializeField] EnemyBase[] enemiesOnBoard;

    [Tooltip("0 = todos se sueltan el mismo frame. >0 = uno cada N segundos.")]
    [SerializeField] float dropInterval = 0.4f;

    [Header("Post-Despliegue (huida)")]
    [Tooltip("Velocidad a la que sigue volando hacia adelante tras soltar los enemigos (m/s).")]
    [SerializeField] float postDeploySpeed = 15f;

    [Tooltip("Segundos mínimos volando tras soltar antes de poder destruirse.")]
    [SerializeField] float minPostDeployTime = 3f;

    [Tooltip("Tiempo máximo tras el cual se destruye aunque siga visible (segundos). 0 = sin límite.")]
    [SerializeField] float maxPostDeployTime = 30f;

    // --- Estado interno ---
    bool launched;
    bool arrived;
    bool finishedDeploy;
    float sailTimer;
    float dropTimer;
    float postDeployTimer;
    int nextEnemyIndex;

    public int EnemiesOnBoardCount =>
        enemiesOnBoard != null ? enemiesOnBoard.Length : 0;

    void Awake()
    {
        if (enemiesOnBoard == null || enemiesOnBoard.Length == 0)
            enemiesOnBoard = GetComponentsInChildren<EnemyBase>(true);
    }

    void Start()
    {
        sailTimer = setSailDelay;

        if (faceTargetOnStart && target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    void Update()
    {
        // 1) Post-despliegue: seguir hacia adelante hasta salir de vista
        if (finishedDeploy)
        {
            UpdatePostDeploy();
            return;
        }

        // 2) Espera antes de zarpar
        if (!launched)
        {
            sailTimer -= Time.deltaTime;
            if (sailTimer > 0f) return;
            launched = true;
        }

        // 3) Vuelo o despliegue
        if (!arrived) MoveTowardsTarget();
        else HandleDropping();
    }

    void MoveTowardsTarget()
    {
        if (target == null) return;

        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f;
        float dist = toTarget.magnitude;

        if (dist <= arriveDistance)
        {
            arrived = true;
            return;
        }

        transform.position += toTarget.normalized * moveSpeed * Time.deltaTime;
    }

    void HandleDropping()
    {
        if (nextEnemyIndex >= enemiesOnBoard.Length)
        {
            FinishDeploy();
            return;
        }

        if (dropInterval > 0f)
        {
            dropTimer -= Time.deltaTime;
            if (dropTimer > 0f) return;
            dropTimer = dropInterval;
        }

        var enemy = enemiesOnBoard[nextEnemyIndex];
        nextEnemyIndex++;

        if (enemy != null && enemy.IsDormant)
            enemy.Deploy();
    }

    void FinishDeploy()
    {
        finishedDeploy = true;
        postDeployTimer = 0f;
    }

    void UpdatePostDeploy()
    {
        postDeployTimer += Time.deltaTime;

        // Sigue volando hacia adelante (en horizontal) en la misma dirección
        Vector3 forward = transform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;

        transform.position += forward.normalized * postDeploySpeed * Time.deltaTime;

        // Todavía no pasó el tiempo mínimo → sigue volando
        if (postDeployTimer < minPostDeployTime) return;

        

        // ¿Se pasó del tiempo máximo? (fallback)
        bool reachedMax = maxPostDeployTime > 0f && postDeployTimer >= maxPostDeployTime;

        if (reachedMax)
            Destroy(gameObject);
    }
}