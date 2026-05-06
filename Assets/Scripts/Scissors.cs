using UnityEngine;

public class Scissors : MonoBehaviour
{
   //private bool dragging=false;
   private Vector3 offset;
   private float cutCooldown=0.2f;
   private float lastCutTime;
   private bool isHolding = false;
   private bool justDropped = false;

   private void Update()
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
            else if (!justDropped && IsMouseOverScissors())
            {
                isHolding = true;
            }
        }

        if (justDropped && !Input.GetMouseButtonDown(0))
        {
            justDropped = false;
        }
    }

    private bool IsMouseOverScissors()
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

   private void OnTriggerEnter2D(Collider2D other)
   {
    //if(!dragging) return;
    //if(Time.time - lastCutTime < cutCooldown) return;
    if(!other.CompareTag("Flower")) return;
    {
        //Destroy(other.gameObject);
    }
    //lastCutTime=Time.time;
    CutFlower(other.gameObject);
   }

   private void CutFlower(GameObject bud)
    {
        if (bud == null) return;
        if (!bud.activeInHierarchy) return;

       //if (bud.transform.childCount == 0)
        {
            //Debug.Log("No stem found - ignoring cut");
            //return;
        }

        Transform stem = null;
        foreach (Transform child in bud.transform)
        {
            if (child.CompareTag("Stem"))
            {
                stem = child;
                break;
            }
        }

        if (stem == null)
        {
            Debug.Log("No stem found - ignoring cut");
            return;
        }

        //Transform stem = bud.transform.GetChild(0);
        FlowerData data = bud.GetComponent<FlowerData>();

        if (data != null && data.prefabReference != null)
        {
            FlowerTransferManager.Instance.selectedFlowerPrefabs.Add(data.prefabReference);
            Debug.Log("Flower added: " + data.prefabReference.name);

            // ADD THIS: move from untrimmed → trimmed inventory
            if (GameManager.Instance != null)
            {
                string flowerName = GameManager.NormalizeKey(data.prefabReference.name);
                bool transferred = GameManager.Instance.TransferToTrimmed(flowerName);
                if (!transferred)
                {
                    // Flower wasn't in untrimmed (e.g. picked up in a previous session)
                    // Add directly to trimmed as a fallback
                    GameManager.Instance.AddTrimmedFlower(flowerName);
                    Debug.LogWarning($"[Scissors] {flowerName} wasn't in untrimmed stock, added directly to trimmed.");
                }
                data.MarkAsTrimmed();
            }
        }

        bud.transform.position += new Vector3(0f, 0f, 0f);

        if (bud.GetComponent<DraggableFlower>() == null)
            bud.gameObject.AddComponent<DraggableFlower>();

        Destroy(stem.gameObject);
    }
}
