using UnityEngine;

public class Scissors : MonoBehaviour
{
   private bool dragging=false;
   private Vector3 offset;

   void OnMouseDown()
   {
    Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mousePos.z=0;
    offset=transform.position-mousePos;
    dragging=true;
   }

   void OnMouseUp()
   {
    dragging=false;
   }

   void Update()
   {
    if(dragging)
    {
        Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z=0;
        transform.position=mousePos+offset;
    }
   }

   private void OnTriggerEnter2D(Collider2D other)
   {
    if(!dragging) return;
    if(other.CompareTag("Flower"))
    {
        Transform bud=other.transform.GetChild(0);
        bud.SetParent(null);

        GameManager.Instance.collectedFlowers.Add(bud.gameObject);
        if(bud.GetComponent<DraggableFlower>()==null)
        bud.gameObject.AddComponent<DraggableFlower>();

        Destroy(other.gameObject);
    }
   }
}
