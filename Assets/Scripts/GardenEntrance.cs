using UnityEngine;
using UnityEngine.SceneManagement;

public class GardenEntrance : MonoBehaviour
{
    [Header("目标场景")]
    public string targetScene = "FlowerGarden";

    [Header("玩家在目标场景的出生点")]
    public Transform spawnPointInGarden;

    private static Vector3 savedSpawnPosition;

    void OnMouseDown()
    {
        if (spawnPointInGarden != null)
        {
            savedSpawnPosition = spawnPointInGarden.position;
        }
        SceneManager.LoadScene(targetScene);
    }

    public static Vector3 GetSpawnPosition()
    {
        return savedSpawnPosition;
    }
}
