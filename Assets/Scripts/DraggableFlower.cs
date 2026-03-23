using UnityEngine;

public class DraggableFlower : MonoBehaviour
{
   private Vector3 offset;
   private bool dragging = false;

void OnMouseDown()
{
    offset=transform.position-GetMouseWorldPos();
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
        transform.position=GetMouseWorldPos()+offset;
    }
}

Vector3 GetMouseWorldPos()
{
    Vector3 mousePoint=Input.mousePosition;
    mousePoint.z=10f;
    return Camera.main.ScreenToWorldPoint(mousePoint);
}
}
