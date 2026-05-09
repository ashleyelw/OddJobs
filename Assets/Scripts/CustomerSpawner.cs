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
    [Tooltip("FloristMain 场景的生成点")]
    [SerializeField] Transform[] spawnPoints = new Transform[4];
    [Tooltip("FloristMain 场景的生成点名称（用于自动查找）")]
    [SerializeField] string[] _spawnPointNames = new string[4];

    [Header("Level2 生成点（可选，留空则继承 FloristMain 的设置）")]
    [SerializeField] Transform[] spawnPointsLevel2 = new Transform[4];
    [Tooltip("Level2 场景的生成点名称（用于自动查找）")]
    [SerializeField] string[] _spawnPointNamesLevel2 = new string[4];

    [Header("允许生成客户的场景")]
    [Tooltip("客户可以在这些场景中生成和恢复。留空则默认只有 FloristMain")]
    [SerializeField] string[] _allowedScenes = new string[] { "FloristMain", "Level2" };

    [Header("生成设置（通用）")]
    [SerializeField] int spawnIntervalMinutes = 3;
    [Range(1, 3)]
    [SerializeField] int flowersPerOrder = 2;

    [Header("【Level2专属】多客户同时生成")]
    [Tooltip("仅在 Level2 场景生效。FloristMain 始终为单客户生成。启用后每次生成1到多个随机数量的客户")]
    [SerializeField] bool enableMultiCustomerSpawnLevel2 = true;
    [Range(1, 4)]
    [Tooltip("Level2 每次生成客户的数量范围（最小值）")]
    [SerializeField] int spawnCountMinLevel2 = 2;
    [Range(1, 4)]
    [Tooltip("Level2 每次生成客户的数量范围（最大值），最大为4")]
    [SerializeField] int spawnCountMaxLevel2 = 4;

    [Header("【Level2专属】生成加速")]
    [Tooltip("仅在 Level2 场景生效。FloristMain 始终不加速。启用后生成间隔随游戏时间不断缩短")]
    [SerializeField] bool enableSpawnAccelerationLevel2 = true;
    [Tooltip("每经过1分钟游戏时间，生成间隔缩短的百分比（0.05 = 每分钟缩短5%%）。值越大加速越快")]
    [Range(0.01f, 0.5f)]
    [SerializeField] float spawnAccelerationRateLevel2 = 0.05f;
    [Tooltip("生成间隔的最短时间下限（秒），防止间隔无限缩短")]
    [SerializeField] float minSpawnIntervalSecondsLevel2 = 10f;
    float _accumulatedGameMinutes = 0f;
    float _accumulatedGameMinutesLevel2 = 0f;

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
    string[] _cachedSpawnPointNamesLevel2 = new string[4];

    bool _isInitialized = false;
    bool _hasRestoredThisLoad = false;
    GameObject[] _slotCustomers = new GameObject[4];

    bool _isTutorialMode = false;
    bool _tutorialCompleted = false;

    string _currentSceneName = "";

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

        Debug.Log($"[CustomerSpawner] Start | startWithTutorial={startWithTutorial}, " +
                  $"Scene={SceneManager.GetActiveScene().name}");

        if (startWithTutorial)
        {
            _isTutorialMode = true;
            Debug.Log("[CustomerSpawner] 教程模式开启，在槽位0生成教程客户");
            TrySpawnInSlot(0);
            _lastSpawnAccumulatedMinutes = GameTimeController.Instance?.GetTotalMinutes() ?? 0f;
        }
        else
        {
            Debug.Log("[CustomerSpawner] 非教程模式，尝试恢复所有槽位");
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

        for (int i = 0; i < 4; i++)
        {
            if (string.IsNullOrEmpty(_cachedSpawnPointNamesLevel2[i]))
            {
                if (!string.IsNullOrEmpty(_spawnPointNamesLevel2[i]))
                    _cachedSpawnPointNamesLevel2[i] = _spawnPointNamesLevel2[i];
                else if (spawnPointsLevel2[i] != null)
                    _cachedSpawnPointNamesLevel2[i] = spawnPointsLevel2[i].gameObject.name;
                else
                    _cachedSpawnPointNamesLevel2[i] = $"Position{i + 1}";
            }
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CustomerSpawner] 场景加载: {scene.name} (mode={mode})");

        _hasRestoredThisLoad = false;
        _currentSceneName = scene.name;

        ClearInvalidCustomerRefs();
        AutoFindSpawnPoints();

        Debug.Log($"[CustomerSpawner] 检查是否需要在场景 {scene.name} 恢复客户...");
        if (IsCustomerSpawningAllowed(scene.name))
        {
            Debug.Log($"[CustomerSpawner] 场景 {scene.name} 允许生成客户，调用 TryRestoreAllSlots()");
            TryRestoreAllSlots();
        }
        else
        {
            Debug.Log($"[CustomerSpawner] 场景 {scene.name} 不在允许列表中，不会在此场景自动生成客户");
        }

        // When leaving a game scene (not a transition scene), clear all slot data
        // so stale customer references from the previous scene don't pollute order cleanup
        if (scene.name != "FloristMain" && scene.name != "Level2")
        {
            for (int i = 0; i < 4; i++)
            {
                if (_slotCustomers[i] != null)
                {
                    Destroy(_slotCustomers[i]);
                    _slotCustomers[i] = null;
                }
                _slotData[i] = new SlotCustomerData();
            }
            Debug.Log("[CustomerSpawner] 非游戏场景，清除所有槽位客户数据");
        }
    }

    /// <summary>检查指定场景是否允许生成客户</summary>
    public bool IsCustomerSpawningAllowed(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return false;

        if (_allowedScenes == null || _allowedScenes.Length == 0)
        {
            return sceneName == "FloristMain";
        }

        foreach (var s in _allowedScenes)
        {
            if (!string.IsNullOrEmpty(s) && s == sceneName)
                return true;
        }
        return false;
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
        Debug.Log("[CustomerSpawner] AutoFindSpawnPoints 被调用，检查所有槽位的生成点...");
        string currentScene = SceneManager.GetActiveScene().name;
        bool isLevel2 = currentScene == "Level2";

        string[] cachedNames = isLevel2 ? _cachedSpawnPointNamesLevel2 : _cachedSpawnPointNames;
        Transform[] currentSpawnPoints = isLevel2 ? spawnPointsLevel2 : spawnPoints;

        for (int i = 0; i < 4; i++)
        {
            if (currentSpawnPoints[i] != null)
            {
                Debug.Log($"[CustomerSpawner] 场景 {currentScene} 槽位 {i} 生成点已存在: {currentSpawnPoints[i].gameObject.name}，无需查找");
                continue;
            }
            string targetName = cachedNames[i];
            Debug.Log($"[CustomerSpawner] 场景 {currentScene} 槽位 {i} 生成点为空，尝试查找: \"{targetName}\"");
            var found = GameObject.Find(targetName);
            if (found != null)
            {
                currentSpawnPoints[i] = found.transform;
                Debug.Log($"[CustomerSpawner] 场景 {currentScene} 槽位 {i} 找到位置: {targetName} -> {found.name}");
            }
            else
            {
                Debug.LogError($"[CustomerSpawner] 场景 {currentScene} 槽位 {i} 未找到名为 \"{targetName}\" 的生成位置！");
            }
        }
        Debug.Log("[CustomerSpawner] AutoFindSpawnPoints 完成");
    }

    /// <summary>获取当前场景的生成点数组</summary>
    public Transform[] GetCurrentSpawnPoints()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isLevel2 = currentScene == "Level2";
        return isLevel2 ? spawnPointsLevel2 : spawnPoints;
    }

    /// <summary>获取当前场景的生成点名称缓存</summary>
    public string[] GetCurrentSpawnPointNames()
    {
        string currentScene = SceneManager.GetActiveScene().name;
        bool isLevel2 = currentScene == "Level2";
        return isLevel2 ? _cachedSpawnPointNamesLevel2 : _cachedSpawnPointNames;
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
            Debug.Log($"[CustomerSpawner] 检查槽位 {i}: prefabIndex={_slotData[i].prefabIndex}");
            if (_slotData[i].prefabIndex >= 0 && !IsCustomerValid(_slotCustomers[i]))
            {
                Debug.Log($"[CustomerSpawner] 槽位 {i} 有数据但没有有效客户，调用 RestoreSlot({i})");
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
        Debug.Log($"[CustomerSpawner] RestoreSlot({slotIndex}) 被调用");

        Transform[] currentSpawnPoints = GetCurrentSpawnPoints();
        if (currentSpawnPoints[slotIndex] == null)
        {
            Debug.LogError($"[CustomerSpawner] RestoreSlot: 场景 {SceneManager.GetActiveScene().name} 槽位 {slotIndex} 的生成点为 null，无法恢复客户！");
            return;
        }

        int prefabIdx = _slotData[slotIndex].prefabIndex;
        Debug.Log($"[CustomerSpawner] RestoreSlot: prefabIndex={prefabIdx}, customerPrefabs.Length={customerPrefabs.Length}");

        if (prefabIdx < 0 || prefabIdx >= customerPrefabs.Length || customerPrefabs[prefabIdx] == null)
        {
            Debug.LogWarning($"[CustomerSpawner] 槽位 {slotIndex} 的预制体索引无效: {prefabIdx}，重置槽位。");
            _slotData[slotIndex] = new SlotCustomerData();
            return;
        }

        Debug.Log($"[CustomerSpawner] RestoreSlot: 开始 Instantiate {customerPrefabs[prefabIdx].name} 到 {currentSpawnPoints[slotIndex].name}");
        GameObject customer = Instantiate(customerPrefabs[prefabIdx], currentSpawnPoints[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_slotData[slotIndex].customerNumber}";
        _slotCustomers[slotIndex] = customer;

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            Debug.Log("[CustomerSpawner] RestoreSlot: 找到 CustomerOrderCoordinator，初始化...");
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                GetAvailableFlowers(), flowersPerOrder, this, _slotData[slotIndex].instanceId);
            coordinator.InitializeRibbons(GetAvailableRibbons());
            coordinator.RestoreHasOrderedState(_slotData[slotIndex].hasOrdered);
            Debug.Log("[CustomerSpawner] RestoreSlot: CustomerOrderCoordinator 初始化完成");
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                Debug.Log("[CustomerSpawner] RestoreSlot: 找到 CustomerInteraction，初始化...");
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
                interaction.RestoreHasOrderedState(_slotData[slotIndex].hasOrdered);
                interaction.gameObject.SetActive(true);
                Debug.Log("[CustomerSpawner] RestoreSlot: CustomerInteraction 初始化完成");
            }
        }

        Debug.Log($"[CustomerSpawner] RestoreSlot: 槽位 {slotIndex} 恢复客户完成: {customer.name}");
    }

    public void OnGameMinuteChanged()
    {
        Debug.Log($"[CustomerSpawner] OnGameMinuteChanged 调用 | " +
                  $"GameTimeController={(GameTimeController.Instance != null)}, " +
                  $"_isInitialized={_isInitialized}, " +
                  $"_isTutorialMode={_isTutorialMode}, _tutorialCompleted={_tutorialCompleted}");

        if (GameTimeController.Instance == null) { Debug.Log("[CustomerSpawner] 阻止生成: GameTimeController.Instance 为 null"); return; }
        if (!_isInitialized) { Debug.Log("[CustomerSpawner] 阻止生成: _isInitialized=false"); return; }
        if (_isTutorialMode && !_tutorialCompleted) { Debug.Log("[CustomerSpawner] 阻止生成: 教程模式未完成"); return; }

        Scene currentScene = SceneManager.GetActiveScene();
        Debug.Log($"[CustomerSpawner] 当前场景: {currentScene.name}");
        if (!IsCustomerSpawningAllowed(currentScene.name))
        {
            Debug.Log($"[CustomerSpawner] 阻止生成: 场景 {currentScene.name} 不在允许列表中");
            return;
        }

        _accumulatedGameMinutes++;

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel2 = sceneName == "Level2";

        if (isLevel2 && enableSpawnAccelerationLevel2)
            _accumulatedGameMinutesLevel2++;

        float gameMinutes = GameTimeController.Instance.GetTotalMinutes();
        float minutesSinceLastSpawn = gameMinutes - _lastSpawnAccumulatedMinutes;

        float currentIntervalMinutes = GetCurrentSpawnInterval() / 60f;

        bool currentMultiSpawn = isLevel2 && enableMultiCustomerSpawnLevel2;
        int currentMin = isLevel2 ? spawnCountMinLevel2 : 1;
        int currentMax = isLevel2 ? spawnCountMaxLevel2 : 1;
        bool currentAcceleration = isLevel2 && enableSpawnAccelerationLevel2;

        Debug.Log($"[CustomerSpawner] 生成检查 | 场景={sceneName}, 已过={minutesSinceLastSpawn:F2}分钟, 间隔={currentIntervalMinutes:F2}分钟, " +
                  $"多客户生成={currentMultiSpawn}, 范围=[{currentMin},{currentMax}], " +
                  $"加速={currentAcceleration}, 实际间隔={GetCurrentSpawnInterval():F1}秒");

        if (minutesSinceLastSpawn >= currentIntervalMinutes)
        {
            _lastSpawnAccumulatedMinutes = gameMinutes;
            Debug.Log("[CustomerSpawner] 时间条件满足，调用 SpawnMultipleCustomers()");
            SpawnMultipleCustomers();
        }
        else
        {
            Debug.Log($"[CustomerSpawner] 时间未到，跳过生成（还差 {currentIntervalMinutes - minutesSinceLastSpawn:F2} 分钟）");
        }
    }

    float GetCurrentSpawnInterval()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel2 = sceneName == "Level2";

        if (!isLevel2)
            return spawnIntervalMinutes * 60f;

        if (!enableSpawnAccelerationLevel2)
            return spawnIntervalMinutes * 60f;

        float baseMinutes = spawnIntervalMinutes;
        float accelerated = baseMinutes * (1f - spawnAccelerationRateLevel2 * _accumulatedGameMinutesLevel2);
        return Mathf.Max(minSpawnIntervalSecondsLevel2, accelerated * 60f);
    }

    void SpawnMultipleCustomers()
    {
        Debug.Log("[CustomerSpawner] SpawnMultipleCustomers 开始执行");

        string sceneName = SceneManager.GetActiveScene().name;
        bool isLevel2 = sceneName == "Level2";
        bool currentMultiSpawn = isLevel2 && enableMultiCustomerSpawnLevel2;
        int currentMin = isLevel2 ? spawnCountMinLevel2 : 1;
        int currentMax = isLevel2 ? spawnCountMaxLevel2 : 1;

        ClearInvalidCustomerRefs();

        int emptySlots = 0;
        for (int i = 0; i < 4; i++)
        {
            bool hasData = _slotData[i].prefabIndex >= 0;
            Debug.Log($"[CustomerSpawner] 槽位{i}: prefabIndex={_slotData[i].prefabIndex}, 已占用={hasData}");
            if (!hasData)
                emptySlots++;
        }

        Debug.Log($"[CustomerSpawner] 空槽位总数: {emptySlots}");

        if (emptySlots == 0)
        {
            Debug.Log("[CustomerSpawner] 所有槽位都满了，不生成新客户");
            return;
        }

        int toSpawn;
        if (currentMultiSpawn)
        {
            toSpawn = UnityEngine.Random.Range(currentMin, Mathf.Min(currentMax, emptySlots) + 1);
            Debug.Log($"[CustomerSpawner] 场景={sceneName} 多客户生成模式: Random.Range({currentMin}, {Mathf.Min(currentMax, emptySlots) + 1}) = {toSpawn}");
        }
        else
        {
            toSpawn = 1;
            Debug.Log($"[CustomerSpawner] 场景={sceneName} 单客户生成模式: toSpawn=1");
        }

        Debug.Log($"[CustomerSpawner] 此次将生成 {toSpawn} 个客户");

        int spawned = 0;
        for (int i = 0; i < 4 && spawned < toSpawn; i++)
        {
            if (_slotData[i].prefabIndex < 0)
            {
                Debug.Log($"[CustomerSpawner] 尝试在槽位 {i} 生成客户");
                TrySpawnInSlot(i);
                spawned++;
            }
        }
        Debug.Log($"[CustomerSpawner] SpawnMultipleCustomers 完成，实际生成了 {spawned} 个客户");
    }

    public void TrySpawnInSlot(int slotIndex)
    {
        Debug.Log($"[CustomerSpawner] TrySpawnInSlot({slotIndex}) 被调用");

        if (slotIndex < 0 || slotIndex >= 4) { Debug.LogError($"[CustomerSpawner] 槽位索引无效: {slotIndex}"); return; }

        Transform[] currentSpawnPoints = GetCurrentSpawnPoints();
        if (currentSpawnPoints[slotIndex] == null) { Debug.LogError($"[CustomerSpawner] 场景 {SceneManager.GetActiveScene().name} 槽位 {slotIndex} 的生成点(currentSpawnPoints[{slotIndex}]) 为 null！"); return; }
        Debug.Log($"[CustomerSpawner] 场景 {SceneManager.GetActiveScene().name} 槽位 {slotIndex} 生成点: {currentSpawnPoints[slotIndex].name}");

        if (_slotData[slotIndex].prefabIndex >= 0)
        {
            Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 已有客户(prefabIndex={_slotData[slotIndex].prefabIndex})，跳过生成");
            return;
        }

        var validPrefabs = customerPrefabs
            .Select((prefab, index) => new { prefab, index })
            .Where(x => x.prefab != null)
            .ToArray();

        Debug.Log($"[CustomerSpawner] 可用预制体数量: {validPrefabs.Length}");
        for (int i = 0; i < customerPrefabs.Length; i++)
        {
            Debug.Log($"[CustomerSpawner]   customerPrefabs[{i}] = {customerPrefabs[i]?.name ?? "NULL"}");
        }

        if (validPrefabs.Length == 0)
        {
            Debug.LogError("[CustomerSpawner] 没有可用的客户预制体（全部为 null）！请在 Inspector 中检查 CustomerSpawner 的 customerPrefabs 数组");
            return;
        }

        var chosen = validPrefabs[UnityEngine.Random.Range(0, validPrefabs.Length)];
        Debug.Log($"[CustomerSpawner] 随机选中预制体: index={chosen.index}, name={chosen.prefab.name}");

        string instanceId = $"{_currentCustomerNumber}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";

        Debug.Log($"[CustomerSpawner] 开始 Instantiate 预制体到 spawnPoints[{slotIndex}]");
        GameObject customer = Instantiate(chosen.prefab, GetCurrentSpawnPoints()[slotIndex]);
        customer.transform.localPosition = Vector3.zero;
        customer.transform.localRotation = Quaternion.identity;
        customer.name = $"Customer_{slotIndex}_{_currentCustomerNumber}";
        _slotCustomers[slotIndex] = customer;
        Debug.Log($"[CustomerSpawner] Instantiate 成功，实例名称: {customer.name}");

        _slotData[slotIndex] = new SlotCustomerData(chosen.index, _currentCustomerNumber, instanceId, false);

        var coordinator = customer.GetComponent<CustomerOrderCoordinator>();
        if (coordinator != null)
        {
            Debug.Log($"[CustomerSpawner] 找到 CustomerOrderCoordinator，开始初始化");
            coordinator.Initialize(slotIndex, _slotData[slotIndex].customerNumber,
                GetAvailableFlowers(), flowersPerOrder, this, instanceId);
            coordinator.InitializeRibbons(GetAvailableRibbons());
            Debug.Log($"[CustomerSpawner] CustomerOrderCoordinator 初始化完成 | 可用鲜花: {string.Join(",", GetAvailableFlowers())}, flowersPerOrder={flowersPerOrder}");
            if (_isTutorialMode)
                coordinator.SetTutorialCustomer(true);
        }
        else
        {
            var interaction = customer.GetComponent<CustomerInteraction>();
            if (interaction != null)
            {
                Debug.Log($"[CustomerSpawner] 找到 CustomerInteraction（非花束模式客户）");
                interaction.SetCustomerNumber(_slotData[slotIndex].customerNumber);
                interaction.SetSlotInfo(slotIndex, this);
            }
            else
            {
                Debug.LogWarning($"[CustomerSpawner] 客户预制体上既没有 CustomerOrderCoordinator 也没有 CustomerInteraction！");
            }
        }

        Debug.Log($"[CustomerSpawner] 槽位 {slotIndex} 生成客户成功: {customer.name}（编号 {_currentCustomerNumber}，ID={instanceId}，教程模式={_isTutorialMode}）");

        _currentCustomerNumber++;
    }

    public void SetTutorialCompleted()
    {
        if (!_isTutorialMode) return;
        _isTutorialMode = false;
        _accumulatedGameMinutes = 0f;
        _accumulatedGameMinutesLevel2 = 0f;
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
