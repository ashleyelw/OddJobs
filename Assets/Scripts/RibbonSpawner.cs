using UnityEngine;

public class RibbonSpawner : MonoBehaviour
{
   public Transform bouquetPoint;

   void Start()
   {
    if(RibbonManager.Instance.selectedRibbonPrefab!=null)
    {
        Instantiate(RibbonManager.Instance.selectedRibbonPrefab,bouquetPoint.position,Quaternion.identity);
    }
   }
}
