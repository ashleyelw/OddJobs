using UnityEngine;

public class FlowerSelect : MonoBehaviour
{
    public GameObject stemPrefab;
    public GameObject flowerBudPrefab;
    public Transform spawnPoint;

    void OnMouseDown()
    {
        SpawnFlower();
    }

    public void SpawnFlower()
    {
        GameObject stem = Instantiate(stemPrefab, spawnPoint.position,Quaternion.identity);
        stem.tag="Flower";

        GameObject bud = Instantiate(flowerBudPrefab,spawnPoint.position,Quaternion.identity);

        bud.transform.SetParent(stem.transform);
        bud.transform.localPosition=new Vector3(0,1f,0);

        if(stem.GetComponent<DraggableFlower>()!=null)
        {
            Destroy(stem.GetComponent<DraggableFlower>());
        }
    }
}
