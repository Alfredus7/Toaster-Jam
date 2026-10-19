using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
// using TMPro; // Descomentá si usás TextMeshPro

public enum CharacterState
{
    Idle,
    Walk,
    Jump,
    Fall,
    Dash
}

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    [SerializeField, Tooltip("Aceleración en suelo. Alto = respuesta instantánea.")]
    float groundAcceleration = 200f;
    [SerializeField, Tooltip("Aceleración en el aire.")]
    float airAcceleration = 25f;
    [SerializeField, Tooltip("Desaceleración cuando vas más rápido que la velocidad objetivo (skid / momentum).")]
    float momentumDeceleration = 35f;

    [Header("Salto")]
    [SerializeField, Tooltip("Velocidad vertical al saltar (unidades/s).")]
    public float jumpVelocity = 8f;
    [SerializeField, Range(0f, 1f), Tooltip("Multiplicador al soltar el salto durante el ascenso.")]
    float jumpCutMultiplier = 0.5f;
    [SerializeField, Tooltip("Tiempo para saltar después de salir de una plataforma.")]
    float coyoteTime = 0.1f;
    [SerializeField, Tooltip("Ventana para guardar un salto pulsado antes de tocar suelo.")]
    float jumpBufferTime = 0.1f;
    public event System.Action Jumped;
    [SerializeField] GroundCheck groundCheck;

    [Header("Dash (Estilo ULTRAKILL)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public int maxDashCharges = 3;
    public float dashRechargeTime = 1.2f;
    [SerializeField, Range(0f, 1f), Tooltip("Porcentaje de velocidad que se conserva al terminar el dash.")]
    float dashExitSpeedMultiplier = 0.7f;
    [SerializeField] Transform cameraTransform;
    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    // Overrides de velocidad (buffs/debuffs)
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    // --- Internos ---
    Rigidbody rb;
    Vector2 moveInput;
    bool isDashing;
    bool isJumping;
    float coyoteTimer;
    float jumpBufferTimer;
    float momentumTimer;

    int currentDashCharges;

    // --- Recarga de dash (secuencial, una carga por vez) ---
    int pendingRechargesCount;
    float nextRechargeTime;

    // Cache UI
    CharacterState _lastState;
    int _lastDashCharges = -1;
    int _lastMaxDashCharges = -1;

    // --- API pública para la UI ---
    public int CurrentDashCharges => currentDashCharges;
    public int MaxDashCharges => maxDashCharges;
    public event System.Action<int, int> DashChargesChanged;   // (current, max)

    void Reset()
    {
        groundCheck = GetComponentInChildren<GroundCheck>();
        if (Camera.main != null) cameraTransform = Camera.main.transform;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (groundCheck == null) groundCheck = GetComponentInChildren<GroundCheck>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        currentDashCharges = maxDashCharges;
    }

    void Start()
    {
        // Emitir estado inicial para que la UI se sincronice sin depender del orden de Awake/OnEnable
        DashChargesChanged?.Invoke(currentDashCharges, maxDashCharges);
    }

    void Update()
    {
        UpdateTimers();
        TryJump();
        UpdateCharacterState();
    }

    bool IsGrounded() => groundCheck == null || groundCheck.isGrounded;

    void UpdateTimers()
    {
        // Coyote time
        if (IsGrounded()) coyoteTimer = coyoteTime;
        else coyoteTimer -= Time.deltaTime;

        // Jump buffer
        if (jumpBufferTimer > 0f) jumpBufferTimer -= Time.deltaTime;

        // Ventana de momentum tras el dash
        if (momentumTimer > 0f) momentumTimer -= Time.deltaTime;

        // Jump cut: dejar de cortar cuando ya no ascendemos
        if (isJumping && rb.linearVelocity.y <= 0f) isJumping = false;

        // --- Recarga de cargas de dash ---
        if (!isDashing &&
            pendingRechargesCount > 0 &&
            Time.time >= nextRechargeTime)
        {
            pendingRechargesCount--;
            currentDashCharges = Mathf.Min(currentDashCharges + 1, maxDashCharges);

            // 🔔 Avisar a la UI que cambió el número de cargas
            DashChargesChanged?.Invoke(currentDashCharges, maxDashCharges);

            if (pendingRechargesCount > 0)
                nextRechargeTime = Time.time + dashRechargeTime;
        }
    }

    void TryJump()
    {
        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            Vector3 v = rb.linearVelocity;
            v.y = jumpVelocity;
            rb.linearVelocity = v;
            isJumping = true;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            Jumped?.Invoke();
        }
    }

    void UpdateCharacterState()
    {
        if (isDashing) { CurrentState = CharacterState.Dash; return; }

        if (!IsGrounded())
        {
            CurrentState = rb.linearVelocity.y > 0.1f
                ? CharacterState.Jump
                : CharacterState.Fall;
            return;
        }

        CurrentState = moveInput.sqrMagnitude > 0.01f
            ? CharacterState.Walk
            : CharacterState.Idle;
    }

    // --- ENTRADA (New Input System) ---

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();

    public void OnJump(InputValue value)
    {
        if (value.isPressed)
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            if (isJumping && rb.linearVelocity.y > 0f)
            {
                Vector3 v = rb.linearVelocity;
                v.y *= jumpCutMultiplier;
                rb.linearVelocity = v;
                isJumping = false;
            }
        }
    }

    public void OnDash(InputValue value)
    {
        if (value.isPressed && currentDashCharges > 0 && !isDashing)
            StartCoroutine(PerformDash());
    }

    // --- FÍSICA ---

    void FixedUpdate()
    {
        if (isDashing) return;

        float targetSpeed = GetTargetSpeed();
        Vector3 targetHoriz = transform.rotation *
                              new Vector3(moveInput.x, 0f, moveInput.y) * targetSpeed;
        Vector3 currentHoriz = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

        float accel;
        if (!IsGrounded())
            accel = airAcceleration;
        else if (momentumTimer > 0f || currentHoriz.magnitude > targetHoriz.magnitude + 0.5f)
            accel = momentumDeceleration;
        else
            accel = groundAcceleration;

        Vector3 newHoriz = Vector3.MoveTowards(
            currentHoriz, targetHoriz, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector3(newHoriz.x, rb.linearVelocity.y, newHoriz.z);
    }

    float GetTargetSpeed()
    {
        if (speedOverrides.Count > 0)
            return speedOverrides[speedOverrides.Count - 1]();
        return speed;
    }

    IEnumerator PerformDash()
    {
        isDashing = true;
        currentDashCharges--;

        // 🔔 Avisar a la UI que se gastó una carga
        DashChargesChanged?.Invoke(currentDashCharges, maxDashCharges);

        // Registrar la carga pendiente
        pendingRechargesCount++;
        if (pendingRechargesCount == 1)
            nextRechargeTime = Time.time + dashRechargeTime;

        // Dirección horizontal desde la cámara
        Vector3 dashDir = cameraTransform != null ? cameraTransform.forward : transform.forward;
        dashDir.y = 0f;
        if (dashDir.sqrMagnitude < 0.001f) dashDir = transform.forward;
        dashDir.Normalize();

        float savedY = rb.linearVelocity.y;
        rb.linearVelocity = dashDir * dashSpeed + Vector3.up * savedY;

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
        momentumTimer = 0.15f;

        Vector3 vel = rb.linearVelocity;
        vel.x *= dashExitSpeedMultiplier;
        vel.z *= dashExitSpeedMultiplier;
        rb.linearVelocity = vel;
    }
}