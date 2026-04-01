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
        public string instanceId;  // 唯一实例ID

        public SlotCustomerData() { }

        public SlotCustomerData(int prefabIndex, int customerNumber, string instanceId)
        {
            this.prefabIndex = prefabIndex;
            this.customerNumber = customerNumber;
            this.instanceId = instanceId;
            this.hasOrdered = false;
        }

        public SlotCustomerData(int prefabIndex, int customerNumber, string instanceId, bool hasOrdered)
        {
            this.prefabIndex = prefabIndex;
            this.customerNumber = customerNumber;
            this.instanceId = instanceId;
            this.hasOrdered = hasOrdered;
        }
    }

    [Header("客户预制体")]
    [SerializeField] GameObject[] customerPrefabs = new GameObject[4];

    [Header("生成点")]
    [SerializeField] Transform[] spawnPoints = new Transform[4];
    [SerializeField] string[] _spawnPointNames = new string[4];

    [Header("生成设置")]
    [SerializeField] int spawnIntervalMinutes = 3;
    [SerializeField] string[] availableFlowers = new string[] { "Rose2", "Daisy2", "Tulip2" };
    [Range(1, 3)]
    [SerializeField] int flowersPerOrder = 2;

    [Header("运行时数据（跨场景保存）")]
    [SerializeField] SlotCustomerData[] _slotData = new SlotCustomerData[4];

    // _currentCustomerNumber 需要持久化以保持编号连续性
    [SerializeField] int _currentCustomerNumber = 1;
    
    int _minutesSinceLastSpawn;
    string[] _cachedSpawnPointNames = new string[4];

    bool _isInitialized = false;
    
    // 防止同一场景加载期间多次恢复
    bool _hasRestoredThisLoad = false;
    
    // 运行时客户引用（非持久化）
    GameObject[] _slotCustomers = new GameObject[4];

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

        InitializeData();
    }

    void Start()
    {
        _isInitialized = true;
        AutoFindSpawnPoints();

        // 只在 Start 时恢复（首次进入 FloristMain 时）
        // 后续场景切换由 OnSceneLoaded 处理
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

    void InitializeData()
    {
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

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CustomerSpawner] 场景加载: {scene.name}");

        // 重置恢复标志
        _hasRestoredThisLoad = false;

        // 清空旧的客户引用（它们已被销毁）
        ClearInvalidCustomerRefs();

        AutoFindSpawnPoints();

        // 只有在主场景才恢复客户
        if (scene.name == "FloristMain")
        {
            TryRestoreAllSlots();
        }
    }

    void ClearInvalidCustomerRefs()
    {
        for (int i = 0; i < 4; i++)
        {
            if (_slotCustomers[i] != null && !IsCustomerValid(_slotCustomers[i]))
            {
                Debug.Log($"[CustomerSpawner] 槽位 {i} 的客户引用已失效，清空。");
                _slotCustomers[i] = null;
            }
        }
    }

    bool IsCustomerValid(GameObject customer)
    {
        return customer != null && customer.activeInHierarchy;
    }

    public void AutoFindSpawnPoints()
    {
        Debug.Log("[CustomerSpawner] 调用 AutoFindSpawnPoints");
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
        // 防止重复恢复
        if (_hasRestoredThisLoad)
        {
            Debug.Log("[CustomerSpawner] TryRestoreAllSlots 已在本次场景加载中调用过，跳过。");
            return;
        }
        _hasRestoredThisLoad = true;

        Debug.Log($"[CustomerSpawner] TryRestoreAllSlots 被调用（_currentCustomerNumber = {_currentCustomerNumber}）");

        // 清空无效引用
        ClearInvalidCustomerRefs();

        for (int i = 0; i < 4; i++)
        {
            // 检查：槽位有数据 且 没有有效客户
            if (_slotData[i].prefabIndex >= 0 && !IsCustomerValid(_slotCustomers[i]))
            {
                RestoreSlot(i);
            }
        }

        LogAllSlots();
    }

    void LogAllSlots()
    {
        string status = $"[CustomerSpawner] 当前槽位状态（_currentCustomerNumber = {_currentCustomerNumber}）:\n";
        for (int i = 0; i < 4; i++)
        {
            bool hasData = _slotData[i].prefabIndex >= 0;
            bool hasCustomer = IsCustomerValid(_slotCustomers[i]);
            status += $"  槽位{i}: 数据={hasData}, 客户={hasCustomer}, 编号={_slotData[i].customerNumber}, 已下单={_slotData[i].hasOrdered}, ID={_slotData[i].instanceId}\n";
        }
        Debug.Log(status);
    }

    void RestoreSlot(int slotIndex)
    {
        if (spawnPoints[slotIndex] == null)
        {
            Debug.LogWarning($"[CustomerSpawner] 槽位 {slotIndex} 没有指定生成点，跳过恢复。");
            return;
        }

        int prefabIdx = _slotData[slotIndex].prefabIndex;
        if (prefabIdx < 0 || prefabIdx >= customerPrefabs.Length || customerPrefabs[prefabIdx] == null)
        {
            Debug.LogWarning($"[CustomerSpawner] 槽位 {slotIndex} 的预制体索引无效: {prefabIdx}，重置槽位。");
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
            // 传递 instanceId 和已下单状态
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                availableFlowers, flowersPerOrder, this, _slotData[slotIndex].instanceId);

            // 恢复客户的下单状态（这是关键！）
            coordinator.RestoreHasOrderedState(_slotData[slotIndex].hasOrdered);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
                // 恢复客户的下单状态
                interaction.RestoreHasOrderedState(_slotData[slotIndex].hasOrdered);
                interaction.gameObject.SetActive(true);
            }
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 恢复了客户: {customer.name}（编号 {_slotData[slotIndex].customerNumber}，已下单={_slotData[slotIndex].hasOrdered}）");
    }

    public void OnGameMinuteChanged()
    {
        if (GameTimeController.Instance == null) return;
        if (!_isInitialized) return;

        // 只在 FloristMain 场景生成客户
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "FloristMain") return;

        _minutesSinceLastSpawn++;
        if (_minutesSinceLastSpawn < spawnIntervalMinutes) return;

        _minutesSinceLastSpawn = 0;
        Debug.Log("[CustomerSpawner] 触发定时生成");
        TrySpawnAllEmptySlots();
    }

    void TrySpawnAllEmptySlots()
    {
        // 清空无效引用后再检查
        ClearInvalidCustomerRefs();

        for (int i = 0; i < 4; i++)
        {
            // 只在槽位为空时生成
            if (_slotData[i].prefabIndex < 0)
                TrySpawnInSlot(i);
        }
    }

    public void TrySpawnInSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        if (spawnPoints[slotIndex] == null)
        {
            Debug.LogWarning($"[CustomerSpawner] 槽位 {slotIndex} 没有生成点！");
            return;
        }
        if (_slotData[slotIndex].prefabIndex >= 0)
        {
            Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 已有客户，跳过生成。");
            return;
        }

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

        // 生成唯一实例ID
        string instanceId = $"{_currentCustomerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        GameObject customer = Instantiate(chosen.prefab, spawnPoints[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_currentCustomerNumber}";
        _slotCustomers[slotIndex] = customer;

        // 保存时带上 instanceId
        _slotData[slotIndex] = new SlotCustomerData(chosen.index, _currentCustomerNumber, instanceId, false);

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                availableFlowers, flowersPerOrder, this, instanceId);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
                // 新生成的客户默认没有下单，所以不调用 RestoreHasOrderedState
            }
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 生成了客户: {customer.name}（编号 {_currentCustomerNumber}，ID={instanceId}）");

        _currentCustomerNumber++;
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
        ClearInvalidCustomerRefs();
        return _slotCustomers.Count(c => IsCustomerValid(c));
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

    public int CurrentCustomerNumber => _currentCustomerNumber;
}
