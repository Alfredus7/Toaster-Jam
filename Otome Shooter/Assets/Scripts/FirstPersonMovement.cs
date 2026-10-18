using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI; // Necesario si usas el Text clásico de UI
// using TMPro; // Descomenta esta línea si usas TextMeshPro

public enum CharacterState
{
    Idle,
    Walk,
    Dash,
    Jump
}

[RequireComponent(typeof(Rigidbody))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;

    [Header("Salto")]
    public float jumpStrength = 2f;
    public event System.Action Jumped;
    [SerializeField, Tooltip("Evita saltar en el aire.")]
    GroundCheck groundCheck;

    [Header("Dash (Estilo ULTRAKILL)")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    [SerializeField, Tooltip("Número máximo de cargas de dash acumulables.")]
    public int maxDashCharges = 3;
    [SerializeField, Tooltip("Tiempo en segundos que tarda en recargar UNA carga.")]
    public float dashRechargeTime = 1.2f;
    [SerializeField] Transform cameraTransform;

    [Header("UI Estado y Dash")]
    [SerializeField] Text stateText; // Cambiar por TextMeshProUGUI si usas TextMeshPro
    [SerializeField] Text dashText;  // Texto dedicado para mostrar las cargas (ej. Dash: 3/3)
    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    // Variables internas
    Rigidbody rb;
    Vector2 moveInput;
    bool isDashing;

    // Cargas y temporizadores de dash
    int currentDashCharges;
    float dashRechargeTimer;

    // Para sobrescribir velocidad externamente si se necesita
    public List<System.Func<float>> speedOverrides = new List<System.Func<float>>();

    void Reset()
    {
        groundCheck = GetComponentInChildren<GroundCheck>();
        if (Camera.main != null)
            cameraTransform = Camera.main.transform;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (groundCheck == null)
            groundCheck = GetComponentInChildren<GroundCheck>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Inicializar cargas al máximo al arrancar
        currentDashCharges = maxDashCharges;
    }

    void Update()
    {
        HandleDashRecharge();
        UpdateCharacterState();
        UpdateUI();
    }

    void HandleDashRecharge()
    {
        // Si no tenemos todas las cargas completas, recargamos con el tiempo
        if (currentDashCharges < maxDashCharges)
        {
            dashRechargeTimer += Time.deltaTime;

            if (dashRechargeTimer >= dashRechargeTime)
            {
                currentDashCharges++;
                dashRechargeTimer = 0f; // Reiniciamos el temporizador para la siguiente carga
            }
        }
        else
        {
            // Si estamos al máximo, reseteamos el temporizador
            dashRechargeTimer = 0f;
        }
    }

    void UpdateCharacterState()
    {
        // 1. Prioridad: Dash
        if (isDashing)
        {
            CurrentState = CharacterState.Dash;
            return;
        }

        // 2. Prioridad: Salto / En el aire
        if (groundCheck != null && !groundCheck.isGrounded)
        {
            CurrentState = CharacterState.Jump;
            return;
        }

        // 3. Estado en tierra: Movimiento o Reposo
        if (moveInput.sqrMagnitude > 0.01f)
        {
            CurrentState = CharacterState.Walk;
        }
        else
        {
            CurrentState = CharacterState.Idle;
        }
    }

    void UpdateUI()
    {
        if (stateText != null)
        {
            stateText.text = $"state: {CurrentState.ToString().ToLower()}";
        }

        if (dashText != null)
        {
            dashText.text = $"Dash: {currentDashCharges}/{maxDashCharges}";
        }
    }

    // --- MÉTODOS DE ENTRADA (New Input System - Send Messages) ---

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (value.isPressed && (!groundCheck || groundCheck.isGrounded))
        {
            rb.AddForce(Vector3.up * 100 * jumpStrength);
            Jumped?.Invoke();
        }
    }

    public void OnDash(InputValue value)
    {
        // Solo puede dashear si presiona la tecla y tiene al menos 1 carga disponible
        if (value.isPressed && currentDashCharges > 0 && !isDashing)
        {
            StartCoroutine(PerformDash());
        }
    }

    // -------------------------------------------------------------

    void FixedUpdate()
    {
        if (isDashing) return;

        float targetMovingSpeed = speed;

        if (speedOverrides.Count > 0)
        {
            targetMovingSpeed = speedOverrides[speedOverrides.Count - 1]();
        }

        Vector2 targetVelocity = new Vector2(moveInput.x * targetMovingSpeed, moveInput.y * targetMovingSpeed);
        rb.linearVelocity = transform.rotation * new Vector3(targetVelocity.x, rb.linearVelocity.y, targetVelocity.y);
    }

    System.Collections.IEnumerator PerformDash()
    {
        isDashing = true;
        currentDashCharges--; // Consumimos una carga

        Vector3 dashDirection;
        if (cameraTransform != null)
        {
            dashDirection = cameraTransform.forward;
            dashDirection.y = 0f;
            dashDirection.Normalize();
        }
        else
        {
            dashDirection = transform.forward;
            dashDirection.y = 0f;
            dashDirection.Normalize();
        }

        if (dashDirection == Vector3.zero)
        {
            dashDirection = transform.forward;
        }

        float currentVerticalVelocity = rb.linearVelocity.y;
        rb.linearVelocity = new Vector3(dashDirection.x * dashSpeed, currentVerticalVelocity, dashDirection.z * dashSpeed);

        yield return new WaitForSeconds(dashDuration);

        isDashing = false;
    }
}