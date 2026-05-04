using UnityEngine;

public class HoverZone : MonoBehaviour
{
    public GameObject silhouette;
    public FlowerSlot slot;

    private void Start()
    {
        if (silhouette != null)
            silhouette.SetActive(false);
    }

    private void OnMouseEnter()
    {
        if (slot.isOccupied) return;

        if (silhouette != null)
            silhouette.SetActive(true);
    }

    private void OnMouseExit()
    {
        if (silhouette != null)
            silhouette.SetActive(false);
    }
}
