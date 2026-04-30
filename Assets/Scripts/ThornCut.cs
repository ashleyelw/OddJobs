using UnityEngine;

public class ThornCut : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Thorn"))
        {
            Destroy(other.gameObject);
        }
    }
}
