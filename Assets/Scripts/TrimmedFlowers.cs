using UnityEngine;

public class TrimmedFlowers : MonoBehaviour
{
    public Transform spawnArea;

    void Start()
    {
        float offsetX=0;
        foreach(GameObject flower in GameManager.Instance.collectedFlowers)
        {
            GameObject newFlower = Instantiate(flower, spawnArea.position + new Vector3(offsetX, 0,0), Quaternion.identity);
        }

        GameManager.Instance.collectedFlowers.Clear();
    }
}
