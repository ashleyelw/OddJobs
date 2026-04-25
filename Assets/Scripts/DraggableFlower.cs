using UnityEngine;

public class DraggableFlower : MonoBehaviour
{
    private Vector3 offset;
    private bool dragging;
    private bool isSnapped = false;

    private void OnMouseDown()
    {
        if (isSnapped) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        offset = transform.position - mousePos;
        dragging = true;
    }

    private void OnMouseUp()
    {
        dragging = false;

        TrySnapToSlot();
    }

    private void Update()
    {
        if (dragging && !isSnapped)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;

            transform.position = mousePos + offset;
        }
    }

    void TrySnapToSlot()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, 0.5f);

        foreach (Collider2D hit in hits)
        {
            FlowerSlot slot = hit.GetComponent<FlowerSlot>();

            if (slot != null && !slot.isOccupied)
            {
                SnapToSlot(slot.transform);
                slot.isOccupied = true;
                return;
            }
        }
    }

    public void SnapToSlot(Transform slot)
    {
        transform.position = slot.position;
        isSnapped = true;
    }
}
