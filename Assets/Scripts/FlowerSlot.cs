using UnityEngine;

public class FlowerSlot : MonoBehaviour
{
    public bool isOccupied = false;

    // Optional: for debugging in editor
    private void OnDrawGizmos()
    {
        Gizmos.color = isOccupied ? Color.red : Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
