using UnityEngine;

public class FlowerSelect : MonoBehaviour
{
    public GameObject stemPrefab;
    public GameObject flowerBudPrefab;
    public Transform spawnPoint;

    public void SpawnFlower()
    {
        GameObject stem = Instantiate(stemPrefab, spawnPoint.position,Quaternion.identity);
        GameObject bud = Instantiate(flowerBudPrefab, spawnPoint.position,Quaternion.identity);

        bud.transform.SetParent(stem.transform);
        bud.transform.localPosition=new Vector3(0,1f,0);

        stem.tag="Flower";
        stem.AddComponent<DraggableFlower>();
    }
}
