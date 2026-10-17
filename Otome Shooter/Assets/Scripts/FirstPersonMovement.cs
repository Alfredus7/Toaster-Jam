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

    [Header("Dash")]
    public float dashSpeed = 25f;
    public float dashDuration = 0.2f;
    public float dashCooldown = 1f;
    [SerializeField] Transform cameraTransform;

    [Header("UI Estado")]
    [SerializeField] Text stateText; // Cambiar por TextMeshProUGUI si usas TextMeshPro
    public CharacterState CurrentState { get; private set; } = CharacterState.Idle;

    // Variables internas
    Rigidbody rb;
    Vector2 moveInput;
    bool isDashing;
    bool canDash = true;

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
    }

    void Update()
    {
        UpdateCharacterState();
        UpdateUI();
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
        if (value.isPressed && canDash && !isDashing)
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
        canDash = false;
        isDashing = true;

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

        yield return new WaitForSeconds(dashCooldown);
        canDash = true;
    }
}