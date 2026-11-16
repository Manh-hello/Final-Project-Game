using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController2D : MonoBehaviour
{
    [SerializeField] float speed = 6f;          // tốc độ ngang
    [SerializeField] float verticalSpeed = 5f;  // tốc độ lên xuống
    [SerializeField] float acceleration = 30f;  // càng lớn càng "gắt", nhỏ thì trượt mượt
    [SerializeField] SpriteRenderer sprite;
    [SerializeField] Animator anim;

    Rigidbody2D rb;
    Vector2 input;
    static readonly int IsMoving = Animator.StringToHash("IsMoving");

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f; // bay tự do
        if (!sprite) sprite = GetComponent<SpriteRenderer>();
        if (!anim) anim = GetComponent<Animator>();
    }

    void Update()
    {
        var k = Keyboard.current;
        if (k == null) return;

        float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f)
                - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
        float y = (k.wKey.isPressed || k.upArrowKey.isPressed || k.spaceKey.isPressed ? 1f : 0f)
                - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);

        input = new Vector2(x, y);

        if (sprite && x != 0f) sprite.flipX = x < 0f;
        if (anim) anim.SetBool(IsMoving, input != Vector2.zero);

        // bool attackPressed = k.jKey.wasPressedThisFrame
        //                 || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        // if (attackPressed && anim) anim.SetTrigger("Attack");
    }

    void FixedUpdate()
    {
        Vector2 target = new Vector2(input.x * speed, input.y * verticalSpeed);
        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, target, acceleration * Time.fixedDeltaTime);
    }
}