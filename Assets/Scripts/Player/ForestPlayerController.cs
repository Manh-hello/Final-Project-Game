using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Keyboard/gamepad platform movement for trying the forest map.</summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public sealed class ForestPlayerController : MonoBehaviour
{
    public float moveSpeed = 5.5f;
    public float sprintSpeed = 8.5f;
    public float jumpSpeed = 11.5f;
    public float acceleration = 65f;
    public float airAcceleration = 35f;
    public float coyoteTime = 0.12f;
    public float jumpBuffer = 0.12f;
    public float fallLimit = -8f;
    public Transform spawnPoint;
    public Transform[] checkpoints;
    public SpriteRenderer visual;
    public Animator animator;

    public bool IsGrounded { get; private set; }
    Rigidbody2D body;
    CapsuleCollider2D capsule;
    ForestCameraFollow cameraFollow;
    readonly RaycastHit2D[] groundHits = new RaycastHit2D[12];
    ContactFilter2D groundFilter;
    Vector2 respawnPosition;
    float horizontal;
    bool sprint;
    bool cutJump;
    float lastGrounded = float.NegativeInfinity;
    float jumpUntil = float.NegativeInfinity;
    int checkpointIndex = -1;

    void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        capsule = GetComponent<CapsuleCollider2D>();
        groundFilter = new ContactFilter2D { useTriggers = false };
        groundFilter.SetLayerMask(Physics2D.GetLayerCollisionMask(gameObject.layer));
        respawnPosition = spawnPoint ? (Vector2)spawnPoint.position : body.position;
        if (!visual) visual = GetComponentInChildren<SpriteRenderer>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Start()
    {
        if (Camera.main) cameraFollow = Camera.main.GetComponent<ForestCameraFollow>();
        if (cameraFollow) cameraFollow.SnapToTarget();
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var pad = Gamepad.current;
        horizontal = 0;
        sprint = false;
        bool jumpPressed = false;
        bool jumpReleased = false;
        bool restart = false;
        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontal -= 1;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontal += 1;
            sprint = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed;
            jumpPressed = keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame;
            jumpReleased = keyboard.spaceKey.wasReleasedThisFrame || keyboard.wKey.wasReleasedThisFrame || keyboard.upArrowKey.wasReleasedThisFrame;
            restart = keyboard.rKey.wasPressedThisFrame;
        }
        if (pad != null)
        {
            float stick = pad.leftStick.x.ReadValue();
            if (Mathf.Abs(stick) > 0.15f) horizontal = stick;
            else if (pad.dpad.x.ReadValue() != 0) horizontal = pad.dpad.x.ReadValue();
            sprint |= pad.leftShoulder.isPressed;
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            jumpReleased |= pad.buttonSouth.wasReleasedThisFrame;
            restart |= pad.selectButton.wasPressedThisFrame;
        }
        horizontal = Mathf.Clamp(horizontal, -1, 1);
        if (jumpPressed) jumpUntil = Time.time + jumpBuffer;
        if (jumpReleased) cutJump = true;
        if (restart || body.position.y < fallLimit) Respawn();
        if (visual && Mathf.Abs(horizontal) > 0.01f) visual.flipX = horizontal < 0;
        if (animator) animator.SetFloat("MoveSpeed", Mathf.Abs(body.linearVelocity.x));
    }

    void FixedUpdate()
    {
        IsGrounded = false;
        if (body.linearVelocity.y <= 0.1f)
        {
            int count = capsule.Cast(Vector2.down, groundFilter, groundHits, 0.08f);
            for (int i = 0; i < count; i++)
                if (groundHits[i].collider && groundHits[i].normal.y > 0.6f) { IsGrounded = true; break; }
        }
        if (IsGrounded) lastGrounded = Time.time;
        var velocity = body.linearVelocity;
        velocity.x = Mathf.MoveTowards(velocity.x, horizontal * (sprint ? sprintSpeed : moveSpeed), (IsGrounded ? acceleration : airAcceleration) * Time.fixedDeltaTime);
        if (Time.time <= jumpUntil && Time.time - lastGrounded <= coyoteTime)
        {
            velocity.y = jumpSpeed;
            jumpUntil = float.NegativeInfinity;
            lastGrounded = float.NegativeInfinity;
            IsGrounded = false;
        }
        if (cutJump && velocity.y > 0) velocity.y *= 0.5f;
        cutJump = false;
        velocity.y = Mathf.Max(velocity.y, -22);
        body.linearVelocity = velocity;
        if (IsGrounded && checkpoints != null)
        {
            for (int i = checkpointIndex + 1; i < checkpoints.Length; i++)
                if (checkpoints[i] && body.position.x >= checkpoints[i].position.x)
                { checkpointIndex = i; respawnPosition = checkpoints[i].position; }
        }
    }

    public void Respawn()
    {
        body.position = respawnPosition;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0;
        transform.position = new Vector3(respawnPosition.x, respawnPosition.y, transform.position.z);
        jumpUntil = lastGrounded = float.NegativeInfinity;
        cutJump = false;
        IsGrounded = false;
        if (cameraFollow) cameraFollow.SnapToTarget();
    }

    void OnDisable()
    {
        horizontal = 0;
        jumpUntil = lastGrounded = float.NegativeInfinity;
        if (body) body.linearVelocity = Vector2.zero;
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        horizontal = 0;
        jumpUntil = float.NegativeInfinity;
    }
}
