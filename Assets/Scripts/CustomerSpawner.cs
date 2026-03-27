using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Linq;

public class CustomerSpawner : MonoBehaviour
{
    public static CustomerSpawner Instance { get; private set; }

    [Serializable]
    public class SlotCustomerData
    {
        public int prefabIndex = -1;
        public int customerNumber;
        public bool hasOrdered;

        public SlotCustomerData() { }

        public SlotCustomerData(int prefabIndex, int customerNumber)
        {
            this.prefabIndex = prefabIndex;
            this.customerNumber = customerNumber;
            hasOrdered = false;
        }
    }

    [SerializeField] GameObject[] customerPrefabs = new GameObject[4];
    [SerializeField] Transform[] spawnPoints = new Transform[4];
    [SerializeField] string[] _spawnPointNames = new string[4];
    [SerializeField] int spawnIntervalMinutes = 3;
    [SerializeField] string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };
    [Range(1, 3)]
    [SerializeField] int flowersPerOrder = 2;
    [SerializeField] SlotCustomerData[] _slotData = new SlotCustomerData[4];

    GameObject[] _slotCustomers = new GameObject[4];
    int _currentCustomerNumber = 1;
    int _minutesSinceLastSpawn;
    string[] _cachedSpawnPointNames = new string[4];

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        for (int i = 0; i < 4; i++)
            if (_slotData[i] == null)
                _slotData[i] = new SlotCustomerData();

        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrEmpty(_cachedSpawnPointNames[i]))
            {
                if (!string.IsNullOrEmpty(_spawnPointNames[i]))
                    _cachedSpawnPointNames[i] = _spawnPointNames[i];
                else if (spawnPoints[i] != null)
                    _cachedSpawnPointNames[i] = spawnPoints[i].gameObject.name;
                else
                    _cachedSpawnPointNames[i] = $"Position{i + 1}";
            }
        }
    }

    void Start()
    {
        AutoFindSpawnPoints();
        TryRestoreAllSlots();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AutoFindSpawnPoints();
        TryRestoreAllSlots();
    }

    public void AutoFindSpawnPoints()
    {
        Debug.Log("调用 AutoFindSpawnPoints");
        for (int i = 0; i < 4; i++)
        {
            if (spawnPoints[i] != null) continue;
            string targetName = _cachedSpawnPointNames[i];
            var found = GameObject.Find(targetName);
            if (found != null)
            {
                spawnPoints[i] = found.transform;
                Debug.Log($"[CustomerSpawner] 槽位 {i} 找到位置: {targetName}");
            }
            else
            {
                Debug.LogWarning($"[CustomerSpawner] 未找到名为 \"{targetName}\" 的生成位置。");
            }
        }
    }

    public void TryRestoreAllSlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_slotData[i].prefabIndex >= 0 && _slotCustomers[i] == null)
                RestoreSlot(i);
        }
    }

    void RestoreSlot(int slotIndex)
    {
        if (spawnPoints[slotIndex] == null) return;

        int prefabIdx = _slotData[slotIndex].prefabIndex;
        if (prefabIdx < 0 || prefabIdx >= customerPrefabs.Length || customerPrefabs[prefabIdx] == null)
        {
            _slotData[slotIndex] = new SlotCustomerData();
            return;
        }

        GameObject customer = Instantiate(customerPrefabs[prefabIdx], spawnPoints[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_slotData[slotIndex].customerNumber}";
        _slotCustomers[slotIndex] = customer;

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                availableFlowers, flowersPerOrder, this);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.gameObject.SetActive(true);
            }
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 恢复了客户: {customer.name}");
    }

    public void OnGameMinuteChanged()
    {
        if (GameTimeController.Instance == null) return;

        _minutesSinceLastSpawn++;
        if (_minutesSinceLastSpawn < spawnIntervalMinutes) return;

        _minutesSinceLastSpawn = 0;
        TrySpawnAllEmptySlots();
    }

    void TrySpawnAllEmptySlots()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_slotData[i].prefabIndex < 0)
                TrySpawnInSlot(i);
        }
    }

    public void TrySpawnInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        if (spawnPoints[slotIndex] == null) return;
        if (_slotData[slotIndex].prefabIndex >= 0) return;

        var validPrefabs = customerPrefabs
            .Select((prefab, index) => new { prefab, index })
            .Where(x => x.prefab != null)
            .ToArray();

        if (validPrefabs.Length == 0)
        {
            Debug.LogWarning("[CustomerSpawner] 没有可用的客户预制体。");
            return;
        }

        var chosen = validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Length)];
        GameObject customer = Instantiate(chosen.prefab, spawnPoints[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_currentCustomerNumber}";
        _slotCustomers[slotIndex] = customer;
        _slotData[slotIndex] = new SlotCustomerData(chosen.index, _currentCustomerNumber);
        _currentCustomerNumber++;

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                availableFlowers, flowersPerOrder, this);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 生成了客户: {customer.name}");
    }

    public void OnCustomerOrdered(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        _slotData[slotIndex].hasOrdered = true;
    }

    public void OnCustomerLeft(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        _slotCustomers[slotIndex] = null;
        _slotData[slotIndex] = new SlotCustomerData();
        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 客户已离开，槽位置空。");
    }

    public bool IsSlotEmpty(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return true;
        return _slotData[slotIndex].prefabIndex < 0;
    }

    public int GetActiveCustomerCount()
    {
        return _slotCustomers.Count(c => c != null);
    }

    public void ForceSpawnAll()
    {
        _minutesSinceLastSpawn = 0;
        TrySpawnAllEmptySlots();
    }

    public SlotCustomerData GetSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        return _slotData[slotIndex];
    }
}
