using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveDirection;
    private bool canMove = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        rb.freezeRotation = true;
        rb.gravityScale = 0f;
    }

    void Update()
    {
        if (!canMove) return;

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector2(horizontal, vertical).normalized;

        if (horizontal != 0)
        {
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * horizontal, 1f, 1f);
        }
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        rb.linearVelocity = moveDirection * moveSpeed;
    }

    public void EnableMovement(bool enable)
    {
        canMove = enable;
        if (!enable)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }
}
