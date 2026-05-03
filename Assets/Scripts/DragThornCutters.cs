using UnityEngine;
public class DragThornCutters : MonoBehaviour
{
    private bool isHolding = true;
    private bool justDropped = false;

    void Update()
    {
        if (isHolding)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0f;
            transform.position = mousePos;
        }

        if (Input.GetMouseButtonDown(0))
        {
            if (isHolding)
            {
                isHolding = false;
                justDropped = true;
            }
            else if (!justDropped && IsMouseOverCutters())
            {
                isHolding = true;
            }
        }

        if (justDropped && !Input.GetMouseButtonDown(0))
        {
            justDropped = false;
        }
    }

    private bool IsMouseOverCutters()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Collider2D[] hits = Physics2D.OverlapPointAll(mousePos);
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject)
                return true;
        }
        return false;
    }
}