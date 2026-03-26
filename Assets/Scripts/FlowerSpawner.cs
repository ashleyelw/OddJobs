using UnityEngine;
using System.Collections.Generic;

public class FlowerSpawner : MonoBehaviour
{
    [Header("花朵预制体")]
    public List<GameObject> flowerPrefabs = new List<GameObject>();

    [Header("生成设置")]
    public float spawnInterval = 3f;
    public int maxSpawnPoints = 6;
    public Vector2 spawnAreaMin = new Vector2(-5f, -3f);
    public Vector2 spawnAreaMax = new Vector2(5f, 3f);
    public float minDistanceBetweenPoints = 1.5f;

    [Header("当前生成点")]
    public List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    private float spawnTimer;
    private Camera mainCamera;
    private int nextFlowerIndex = 0;

    public static FlowerSpawner Instance { get; private set; }

    [System.Serializable]
    public class SpawnPoint
    {
        public Vector2 position;
        public GameObject currentFlower;  // 当前生成的花朵，null表示空闲
        public bool isOccupied => currentFlower != null;
    }

    void Start()
    {
        Instance = this;
        mainCamera = Camera.main;
        InitializeSpawnPoints();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            TrySpawnFlower();
        }
    }

    void InitializeSpawnPoints()
    {
        spawnPoints.Clear();

        for (int i = 0; i < maxSpawnPoints; i++)
        {
            Vector2 pos = GetRandomUniquePosition();
            SpawnPoint sp = new SpawnPoint { position = pos };
            spawnPoints.Add(sp);
            SpawnFlowerAtPoint(sp);
        }
    }

    Vector2 GetRandomUniquePosition()
    {
        Vector2 pos;
        int attempts = 0;
        int maxAttempts = 50;

        do
        {
            pos = new Vector2(
                Random.Range(spawnAreaMin.x, spawnAreaMax.x),
                Random.Range(spawnAreaMin.y, spawnAreaMax.y)
            );
            attempts++;
        }
        while (IsPositionTooClose(pos) && attempts < maxAttempts);

        return pos;
    }

    bool IsPositionTooClose(Vector2 pos)
    {
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (Vector2.Distance(pos, sp.position) < minDistanceBetweenPoints)
                return true;
        }
        return false;
    }

    void TrySpawnFlower()
    {
        List<SpawnPoint> freePoints = spawnPoints.FindAll(sp => !sp.isOccupied);

        if (freePoints.Count == 0 || flowerPrefabs.Count == 0)
            return;

        SpawnPoint targetPoint = freePoints[Random.Range(0, freePoints.Count)];
        SpawnFlowerAtPoint(targetPoint);
    }

    void SpawnFlowerAtPoint(SpawnPoint point)
    {
        if (point.isOccupied || flowerPrefabs.Count == 0)
            return;

        GameObject prefab = flowerPrefabs[nextFlowerIndex];
        nextFlowerIndex = (nextFlowerIndex + 1) % flowerPrefabs.Count;
        GameObject flower = Instantiate(prefab, point.position, Quaternion.identity);
        point.currentFlower = flower;
    }

    public void OnFlowerHarvested(SpawnPoint point)
    {
        point.currentFlower = null;
    }

    public SpawnPoint GetNearestSpawnPoint(Vector2 worldPos, float range)
    {
        SpawnPoint nearest = null;
        float minDist = range;

        foreach (SpawnPoint sp in spawnPoints)
        {
            if (!sp.isOccupied) continue;

            float dist = Vector2.Distance(worldPos, sp.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = sp;
            }
        }

        return nearest;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 绘制生成区域边框
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 center = new Vector3(
            (spawnAreaMin.x + spawnAreaMax.x) / 2f,
            (spawnAreaMin.y + spawnAreaMax.y) / 2f,
            0f
        );
        Vector3 size = new Vector3(
            spawnAreaMax.x - spawnAreaMin.x,
            spawnAreaMax.y - spawnAreaMin.y,
            0f
        );
        Gizmos.DrawWireCube(center, size);
        Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
        Gizmos.DrawCube(center, size);
    }

    void OnDrawGizmosSelected()
    {
        if (spawnPoints == null || spawnPoints.Count == 0) return;

        // 绘制每个生成点
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.isOccupied)
            {
                // 绿色实心圆 = 有花朵
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.9f);
            }
            else
            {
                // 灰色空心圆 = 空闲
                Gizmos.color = new Color(0.7f, 0.7f, 0.7f, 0.6f);
            }

            Vector3 pos = new Vector3(sp.position.x, sp.position.y, 0f);
            Gizmos.DrawWireSphere(pos, 0.3f);

            if (sp.isOccupied)
            {
                Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.3f);
                Gizmos.DrawSphere(pos, 0.3f);
            }

            // 绘制点编号
            int index = spawnPoints.IndexOf(sp);
            UnityEditor.Handles.Label(pos + new Vector3(0.4f, 0.4f, 0f), $"P{index}");

            // 绘制最小间距圆弧（仅选中时显示最近的一个）
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawWireSphere(pos, minDistanceBetweenPoints);
        }
    }
#endif
}
