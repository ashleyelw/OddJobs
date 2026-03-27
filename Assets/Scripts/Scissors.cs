using UnityEngine;

public class Scissors : MonoBehaviour
{
   private bool dragging=false;
   private Vector3 offset;
   private float cutCooldown=0.2f;
   private float lastCutTime;

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
    if(Time.time - lastCutTime < cutCooldown) return;
    if(!other.CompareTag("Flower")) return;
    lastCutTime=Time.time;
    CutFlower(other.gameObject);
   }

   private void CutFlower(GameObject stem)
   {
    if(stem==null) return;
    if(!stem.activeInHierarchy) return;

    if(stem.transform.childCount==0)
    {
        Debug.Log("No bud found - ignoring cut");
        return;
    }

    Transform bud=stem.transform.GetChild(0);
    FlowerData data = bud.GetComponent<FlowerData>();

    if(data!= null && data.prefabReference != null)
    {
        FlowerTransferManager.Instance.selectedFlowerPrefabs.Add(data.prefabReference);
        Debug.Log("Flower added: " + data.prefabReference.name);
    }

    bud.SetParent(null);
    bud.position += new Vector3(0.5f,0.5f,0f);

    if(bud.GetComponent<DraggableFlower>()==null)
    bud.gameObject.AddComponent<DraggableFlower>();

    Destroy(stem); 
   
}
}
