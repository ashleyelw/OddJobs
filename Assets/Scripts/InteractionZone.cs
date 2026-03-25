using UnityEngine;
using UnityEngine.Events;

public class InteractionZone : MonoBehaviour
{
    [Header("UI Settings")]
    public GameObject interactionUI;
    public string promptText = "Press E to interact";

    [Header("Events")]
    public UnityEvent onPlayerEnter;
    public UnityEvent onPlayerExit;

    private bool isPlayerInside = false;

    void Start()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            ShowUI();
            onPlayerEnter?.Invoke();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            HideUI();
            onPlayerExit?.Invoke();
        }
    }

    void Update()
    {
        if (isPlayerInside && Input.GetKeyDown(KeyCode.E))
        {
            Interact();
        }
    }

    public void ShowUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(true);
        }
    }

    public void HideUI()
    {
        if (interactionUI != null)
        {
            interactionUI.SetActive(false);
        }
    }

    protected virtual void Interact()
    {
        Debug.Log($"Player interacted with: {gameObject.name}");
    }
}
