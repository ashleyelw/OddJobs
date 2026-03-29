using UnityEngine;

public class RibbonSpawner : MonoBehaviour
{
  public Transform bouquetPoint;

   public void SpawnRibbon(GameObject ribbonPrefab)
   {
    if(ribbonPrefab!=null)
    {
        Instantiate(ribbonPrefab,bouquetPoint.position,Quaternion.identity);
    }
   }
}
