using UnityEngine;

/// <summary>
/// PlayerController — handles movement, sprint, dodge.
/// Requires: CharacterController component on the player GameObject.
/// Setup: Attach to player root. Set mainCamera in inspector or it auto-finds.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 7f;
    public float sprintSpeed = 11f;
    public float rotationSpeed = 15f;
    public float gravity = -20f;

    [Header("Dodge")]
    public float dodgeDistance = 5f;
    public float dodgeDuration = 0.25f;
    public float dodgeCooldown = 0.8f;
    public float iFrameDuration = 0.2f;

    [Header("References")]
    public Transform mainCamera;

    // State
    public bool IsDodging { get; private set; }
    public bool IsInvincible { get; private set; }
    public bool CanMove { get; set; } = true;
    public Vector3 MoveDirection { get; private set; }

    private CharacterController controller;
    private Animator animator;
    private Vector3 velocity;
    private float dodgeTimer;
    private float dodgeCooldownTimer;
    private Vector3 dodgeDirection;
    private bool isSprinting;

    // Animator hashes (set these to match your Mixamo animator parameters)
    private static readonly int AnimSpeed = Animator.StringToHash("Speed");
    private static readonly int AnimDodge = Animator.StringToHash("Dodge");

    void Start()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (mainCamera == null)
            mainCamera = Camera.main?.transform;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.CurrentState != GameManager.GameState.Playing)
            return;

        HandleDodgeCooldown();

        if (IsDodging)
        {
            ProcessDodge();
            return;
        }

        HandleMovement();
        HandleDodgeInput();
        ApplyGravity();
    }

    void HandleMovement()
    {
        if (!CanMove) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        isSprinting = Input.GetKey(KeyCode.LeftShift);

        Vector3 inputDir = new Vector3(h, 0f, v).normalized;

        if (inputDir.magnitude >= 0.1f)
        {
            // Camera-relative movement
            float targetAngle = Mathf.Atan2(inputDir.x, inputDir.z) * Mathf.Rad2Deg;
            if (mainCamera != null)
                targetAngle += mainCamera.eulerAngles.y;

            float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);

            MoveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
            float speed = isSprinting ? sprintSpeed : walkSpeed;
            controller.Move(MoveDirection * speed * Time.deltaTime);
        }
        else
        {
            MoveDirection = Vector3.zero;
        }

        // Animator
        float animSpeed = inputDir.magnitude * (isSprinting ? 2f : 1f);
        animator?.SetFloat(AnimSpeed, animSpeed, 0.1f, Time.deltaTime);
    }

    void HandleDodgeInput()
    {
        if (Input.GetKeyDown(KeyCode.Space) && dodgeCooldownTimer <= 0f)
        {
            StartDodge();
        }
    }

    void StartDodge()
    {
        IsDodging = true;
        IsInvincible = true;
        dodgeTimer = dodgeDuration;
        dodgeCooldownTimer = dodgeCooldown;

        // Dodge in move direction, or forward if standing still
        dodgeDirection = MoveDirection.magnitude > 0.1f ? MoveDirection.normalized : transform.forward;

        animator?.SetTrigger(AnimDodge);
    }

    void ProcessDodge()
    {
        float speed = dodgeDistance / dodgeDuration;
        controller.Move(dodgeDirection * speed * Time.deltaTime);

        dodgeTimer -= Time.deltaTime;

        // i-frames end before dodge ends
        if (dodgeTimer <= dodgeDuration - iFrameDuration)
            IsInvincible = false;

        if (dodgeTimer <= 0f)
            IsDodging = false;
    }

    void HandleDodgeCooldown()
    {
        if (dodgeCooldownTimer > 0f)
            dodgeCooldownTimer -= Time.deltaTime;
    }

    void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
