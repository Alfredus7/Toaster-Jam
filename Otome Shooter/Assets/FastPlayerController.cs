using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem; // Necesario para usar InputValue

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float jumpHeight = 1.9f;
    public float gravityScale = -20f;

    private Vector2 moveInputVal;
    private bool isSprintPressed;
    private float verticalVelocity;
    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        ApplyMovement();
    }

    // --- MÉTODOS DEL NUEVO INPUT SYSTEM (Send Messages) ---

    // Se ejecuta automáticamente cuando cambian los ejes de movimiento (WASD / Joystick)
    public void OnMove(InputValue value)
    {
        moveInputVal = value.Get<Vector2>();
    }

    // Se ejecuta al presionar o soltar el botón de correr
    public void OnSprint(InputValue value)
    {
        isSprintPressed = value.isPressed;
    }

    // Se ejecuta al presionar el botón de salto
    public void OnJump(InputValue value)
    {
        if (value.isPressed && characterController.isGrounded)
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravityScale);
        }
    }

    // --- LÓGICA DE MOVIMIENTO ---

    private void ApplyMovement()
    {
        // Manejo de la gravedad en el eje Y
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Pequeño impulso hacia abajo para mantenerlo pegado al suelo
        }
        else
        {
            verticalVelocity += gravityScale * Time.deltaTime;
        }

        // Calcular dirección de movimiento horizontal
        Vector3 moveDir = new Vector3(moveInputVal.x, 0f, moveInputVal.y);
        moveDir = Vector3.ClampMagnitude(moveDir, 1f);
        moveDir = transform.TransformDirection(moveDir);

        // Elegir velocidad (Caminar o Correr)
        float currentSpeed = isSprintPressed ? runSpeed : walkSpeed;

        // Combinar velocidad horizontal y vertical
        Vector3 velocity = moveDir * currentSpeed;
        velocity.y = verticalVelocity;

        // Mover el personaje
        characterController.Move(velocity * Time.deltaTime);
    }
}