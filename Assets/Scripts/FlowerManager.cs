using UnityEngine;

public class FlowerManager : MonoBehaviour
{
    public GameObject stemPrefab;
    public Transform spawnPoint;

    public void SpawnFlower(GameObject budPrefab)
    {
        GameObject stem=Instantiate(stemPrefab,spawnPoint.position,Quaternion.identity);
        stem.tag="Flower";

        GameObject bud=Instantiate(budPrefab,spawnPoint.position,Quaternion.identity);
        stem.transform.SetParent(bud.transform);
        stem.transform.localPosition=new Vector3(0,-1f,0);
    }
}