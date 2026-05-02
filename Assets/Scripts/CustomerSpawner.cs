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
        public string instanceId;

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
    [Range(1, 3)]
    [SerializeField] int flowersPerOrder = 2;
    [Tooltip("启用后每次生成1到多个随机数量的客户；关闭后每次只生成1个客户")]
    [SerializeField] bool enableMultiCustomerSpawn = true;
    [Range(1, 4)]
    [Tooltip("每次生成客户的数量范围（最小值）")]
    [SerializeField] int spawnCountMin = 1;
    [Range(1, 4)]
    [Tooltip("每次生成客户的数量范围（最大值），最大为4")]
    [SerializeField] int spawnCountMax = 3;

    [Header("生成加速")]
    [Tooltip("启用后生成间隔随游戏时间不断缩短")]
    [SerializeField] bool enableSpawnAcceleration = false;
    [Tooltip("每经过1分钟游戏时间，生成间隔缩短的百分比（0.05 = 每分钟缩短5%%）。值越大加速越快")]
    [Range(0.01f, 0.5f)]
    [SerializeField] float spawnAccelerationRate = 0.05f;
    [Tooltip("生成间隔的最短时间下限（秒），防止间隔无限缩短")]
    [SerializeField] float minSpawnIntervalSeconds = 15f;
    float _accumulatedGameMinutes = 0f;

    [Header("鲜花和丝带来源（从Registry自动获取）")]
    [SerializeField] private FlowerSpriteRegistry flowerRegistry;
    [SerializeField] private RibbonSpriteRegistry ribbonRegistry;

    /// <summary>获取所有可用的鲜花名称（从Registry获取）</summary>
    public string[] GetAvailableFlowers()
    {
        if (flowerRegistry != null)
        {
            var names = flowerRegistry.GetAllFlowerNames();
            if (names != null && names.Length > 0)
                return names;
        }
        Debug.LogWarning("[CustomerSpawner] FlowerRegistry未配置或为空，使用备用鲜花列表");
        return new string[] { "Rose2", "Daisy2", "Tulip2" };
    }

    /// <summary>获取所有可用的丝带名称（从Registry获取）</summary>
    public string[] GetAvailableRibbons()
    {
        if (ribbonRegistry != null)
        {
            var names = ribbonRegistry.GetAllRibbonNames();
            if (names != null && names.Length > 0)
                return names;
        }
        Debug.LogWarning("[CustomerSpawner] RibbonRegistry未配置或为空，使用备用丝带列表");
        return new string[] { "RibbonRed", "RibbonBlue", "RibbonYellow" };
    }

    [Header("教程模式")]
    [SerializeField] bool startWithTutorial = true;

    [Header("运行时数据（跨场景保存）")]
    [SerializeField] SlotCustomerData[] _slotData = new SlotCustomerData[4];

    [SerializeField] int _currentCustomerNumber = 1;

    float _lastSpawnAccumulatedMinutes = 0f;
    string[] _cachedSpawnPointNames = new string[4];

    bool _isInitialized = false;
    bool _hasRestoredThisLoad = false;
    GameObject[] _slotCustomers = new GameObject[4];

    bool _isTutorialMode = false;
    bool _tutorialCompleted = false;

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

        if (startWithTutorial)
        {
            _isTutorialMode = true;
            TrySpawnInSlot(0);
            _lastSpawnAccumulatedMinutes = GameTimeController.Instance?.GetTotalMinutes() ?? 0f;
        }
        else
        {
            TryRestoreAllSlots();
        }
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

        _hasRestoredThisLoad = false;

        ClearInvalidCustomerRefs();
        AutoFindSpawnPoints();

        if (scene.name == "FloristMain")
            TryRestoreAllSlots();
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
                Debug.LogWarning($"[CustomerSpawner] 未找到名为 \"{targetName}\" 的生成位置。");
        }
    }

    public void TryRestoreAllSlots()
    {
        if (_hasRestoredThisLoad)
        {
            Debug.Log("[CustomerSpawner] TryRestoreAllSlots 已在本次场景加载中调用过，跳过。");
            return;
        }
        _hasRestoredThisLoad = true;

        Debug.Log($"[CustomerSpawner] TryRestoreAllSlots 被调用（_currentCustomerNumber = {_currentCustomerNumber}）");

        ClearInvalidCustomerRefs();

        for (int i = 0; i < 4; i++)
        {
            if (_slotData[i].prefabIndex >= 0 && !IsCustomerValid(_slotCustomers[i]))
                RestoreSlot(i);
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
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                GetAvailableFlowers(), flowersPerOrder, this, _slotData[slotIndex].instanceId);
            coordinator.InitializeRibbons(GetAvailableRibbons());
            coordinator.RestoreHasOrderedState(_slotData[slotIndex].hasOrdered);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
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
        if (_isTutorialMode && !_tutorialCompleted) return;

        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "FloristMain") return;

        _accumulatedGameMinutes++;

        float gameMinutes = GameTimeController.Instance.GetTotalMinutes();
        float minutesSinceLastSpawn = gameMinutes - _lastSpawnAccumulatedMinutes;

        float currentIntervalMinutes = GetCurrentSpawnInterval() / 60f;

        if (minutesSinceLastSpawn >= currentIntervalMinutes)
        {
            _lastSpawnAccumulatedMinutes = gameMinutes;
            SpawnMultipleCustomers();
        }
    }

    float GetCurrentSpawnInterval()
    {
        if (!enableSpawnAcceleration)
            return spawnIntervalMinutes * 60f;

        float baseMinutes = spawnIntervalMinutes;
        float accelerated = baseMinutes * (1f - spawnAccelerationRate * _accumulatedGameMinutes);
        return Mathf.Max(minSpawnIntervalSeconds, accelerated * 60f);
    }

    void SpawnMultipleCustomers()
    {
        ClearInvalidCustomerRefs();

        int emptySlots = 0;
        for (int i = 0; i < 4; i++)
        {
            if (_slotData[i].prefabIndex < 0)
                emptySlots++;
        }

        if (emptySlots == 0)
        {
            Debug.Log("[CustomerSpawner] 所有槽位都满了，不生成新客户");
            return;
        }

        int toSpawn;
        if (enableMultiCustomerSpawn)
        {
            toSpawn = UnityEngine.Random.Range(spawnCountMin, Mathf.Min(spawnCountMax, emptySlots) + 1);
        }
        else
        {
            toSpawn = 1;
        }

        Debug.Log($"[CustomerSpawner] 此次生成 {toSpawn} 个客户（空槽位: {emptySlots}）");

        int spawned = 0;
        for (int i = 0; i < 4 && spawned < toSpawn; i++)
        {
            if (_slotData[i].prefabIndex < 0)
            {
                TrySpawnInSlot(i);
                spawned++;
            }
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

        string instanceId = $"{_currentCustomerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        GameObject customer = Instantiate(chosen.prefab, spawnPoints[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_currentCustomerNumber}";
        _slotCustomers[slotIndex] = customer;

        _slotData[slotIndex] = new SlotCustomerData(chosen.index, _currentCustomerNumber, instanceId, false);

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                GetAvailableFlowers(), flowersPerOrder, this, instanceId);
            coordinator.InitializeRibbons(GetAvailableRibbons());
            if (_isTutorialMode)
                coordinator.SetTutorialCustomer(true);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
            }
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 生成了客户: {customer.name}（编号 {_currentCustomerNumber}，ID={instanceId}，教程模式={_isTutorialMode}）");

        _currentCustomerNumber++;
    }

    public void SetTutorialCompleted()
    {
        if (!_isTutorialMode) return;
        _isTutorialMode = false;
        _accumulatedGameMinutes = 0f;
        Debug.Log("[CustomerSpawner] 教程完成，启用正常客户生成");
    }

    public bool IsTutorialMode => _isTutorialMode;

    public void OnCustomerOrdered(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;
        _slotData[slotIndex].hasOrdered = true;
    }

    public void OnCustomerLeft(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return;

        if (_isTutorialMode && slotIndex == 0)
        {
            _tutorialCompleted = true;
            _accumulatedGameMinutes = 0f;
            Debug.Log("[CustomerSpawner] 教程客户已完成，恢复正常生成逻辑");
        }

        if (_slotCustomers[slotIndex] != null)
        {
            Debug.Log($"[CustomerSpawner] 销毁槽位 {slotIndex} 的客户: {_slotCustomers[slotIndex].name}");
            Destroy(_slotCustomers[slotIndex]);
        }

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
        SpawnMultipleCustomers();
    }

    public SlotCustomerData GetSlotData(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 4) return null;
        return _slotData[slotIndex];
    }

    public int CurrentCustomerNumber => _currentCustomerNumber;
}