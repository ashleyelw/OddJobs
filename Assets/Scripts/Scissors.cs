using UnityEngine;

public class Scissors : MonoBehaviour
{
   private bool dragging=false;
   private Vector3 offset;

   private void Update()
   {
    Vector3 mousePos=Camera.main.ScreenToWorldPoint(Input.mousePosition);
    mousePos.z=0f;

    if(Input.GetMouseButtonDown(0))
    {
        if(GetComponent<SpriteRenderer>().bounds.Contains(mousePos))
        {
            dragging=true;
            offset=transform.position-mousePos;
        }
    }

    if(Input.GetMouseButtonUp(0))
    dragging=false;
    if(dragging)
    transform.position=mousePos+offset;
   }

   private void OnTriggerEnter2D(Collider2D other)
   {
    if(!dragging) return;
    if(other.CompareTag("Flower"))
    CutFlower(other.gameObject);
   }

   private void CutFlower(GameObject stem)
   {
    if(stem.transform.childCount ==0) return;

    Transform bud=stem.transform.GetChild(0);
    bud.SetParent(null);
    bud.position+=new Vector3(0.5f,0.5f,0f);

    if(bud.GetComponent<DraggableFlower>()==null)
    bud.gameObject.AddComponent<DraggableFlower>();

    Destroy(stem);

    FlowerData data=bud.GetComponent<FlowerData>();
    if(data!=null)
    {
        if(data.prefabReference!=null)
        {
        FlowerTransferManager.Instance.selectedFlowerPrefabs.Add(data.prefabReference);
    }
   }
}
}
