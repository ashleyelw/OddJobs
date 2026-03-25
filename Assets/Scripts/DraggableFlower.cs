using UnityEngine;

public class DraggableFlower : MonoBehaviour
{
   private Vector3 offset;
   private bool dragging;

   private void OnMouseDown()
   {
    Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mousePos.z=0f;
    offset=transform.position-mousePos;
    dragging=true;
   }

   private void OnMouseUp()
   {
    dragging=false;
   }

   private void Update()
   {
    if(dragging)
    {
        Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z=0f;
        transform.position=mousePos+offset;
    }
   }
}
