using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public Animator anim;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 input;
    private Vector2 lastMoveDir;
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

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        GetInput();
        Animate();
    }

    void FixedUpdate()
    {
        if (!canMove) return;

        rb.linearVelocity = input * moveSpeed;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "FlowerGarden" && GardenEntrance.GetSpawnPosition() != Vector3.zero)
        {
            transform.position = GardenEntrance.GetSpawnPosition();
        }
    }

    void GetInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        input = new Vector2(horizontal, vertical).normalized;

        if (input.magnitude > 0.1f)
        {
            lastMoveDir = input;
        }
    }

    void Animate()
    {
        if (input.magnitude > 0.1f)
        {
            anim.SetFloat("MoveX", input.x);
            anim.SetFloat("MoveY", input.y);
        }
        else
        {
            anim.SetFloat("MoveX", lastMoveDir.x);
            anim.SetFloat("MoveY", lastMoveDir.y);
        }
        anim.SetFloat("Speed", input.magnitude);
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