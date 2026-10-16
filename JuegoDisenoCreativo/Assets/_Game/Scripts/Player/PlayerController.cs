using UnityEngine;
using UnityEngine.InputSystem;

// Movimiento en tercera persona: caminar (WASD), correr (Shift), saltar (Espacio),
// atacar (clic izquierdo) e interactuar (E). El movimiento es relativo a la cámara.
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    public float walkSpeed = 1.6f;
    public float sprintSpeed = 3.2f;
    public float turnSpeed = 14f;
    public float jumpHeight = 0.6f;
    public float gravity = -15f;

    [Header("Combate / interacción")]
    public float attackDuration = 0.5f;
    public float interactRadius = 0.6f;

    [Header("Referencias")]
    public Transform cameraTransform;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int JumpHash = Animator.StringToHash("Jump");
    static readonly int AttackHash = Animator.StringToHash("Attack");
    static readonly int InteractHash = Animator.StringToHash("Interact");
    static readonly int GroundedHash = Animator.StringToHash("Grounded");

    CharacterController controller;
    Animator animator;
    InputAction moveAction, jumpAction, sprintAction, attackAction, interactAction;
    float verticalVelocity;
    float actionLockTimer;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        var actions = InputSystem.actions;
        moveAction = actions.FindAction("Player/Move", true);
        jumpAction = actions.FindAction("Player/Jump", true);
        sprintAction = actions.FindAction("Player/Sprint", true);
        attackAction = actions.FindAction("Player/Attack", true);
        interactAction = actions.FindAction("Player/Interact", true);
    }

    void Update()
    {
        float dt = Time.deltaTime;
        bool grounded = controller.isGrounded;

        // Dirección según la cámara, aplanada al suelo.
        Vector2 input = moveAction.ReadValue<Vector2>();
        Vector3 forward = Vector3.forward, right = Vector3.right;
        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
        }
        Vector3 direction = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1f);

        float speed = (sprintAction.IsPressed() ? sprintSpeed : walkSpeed) * direction.magnitude;
        if (actionLockTimer > 0f)
        {
            actionLockTimer -= dt;
            speed = 0f;
        }

        if (direction.sqrMagnitude > 0.001f && actionLockTimer <= 0f)
        {
            Quaternion target = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, turnSpeed * dt);
        }

        // Gravedad y salto.
        if (grounded && verticalVelocity < 0f) verticalVelocity = -2f;
        if (grounded && actionLockTimer <= 0f && jumpAction.WasPressedThisFrame())
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator?.SetTrigger(JumpHash);
        }
        verticalVelocity += gravity * dt;

        Vector3 horizontal = direction.sqrMagnitude > 0.001f ? direction.normalized * speed : Vector3.zero;
        controller.Move((horizontal + Vector3.up * verticalVelocity) * dt);

        if (grounded && actionLockTimer <= 0f)
        {
            if (attackAction.WasPressedThisFrame())
            {
                animator?.SetTrigger(AttackHash);
                actionLockTimer = attackDuration;
            }
            else if (interactAction.WasPressedThisFrame())
            {
                TryInteract();
            }
        }

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, speed, 0.1f, dt);
            animator.SetBool(GroundedHash, grounded);
        }
    }

    void TryInteract()
    {
        Vector3 center = transform.position + transform.forward * interactRadius + Vector3.up * 0.3f;
        foreach (var hit in Physics.OverlapSphere(center, interactRadius))
        {
            var openable = hit.GetComponentInParent<Openable>();
            if (openable == null) continue;
            openable.Toggle();
            animator?.SetTrigger(InteractHash);
            actionLockTimer = attackDuration;
            return;
        }
    }
}
