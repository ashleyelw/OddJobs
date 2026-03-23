using UnityEngine;

public class MouseArrow : MonoBehaviour
{
    void Start()
    {
        Cursor.visible=false;
    }

    void Update()
    {
        Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z=0;
        transform.position=mousePos;
    }
}
