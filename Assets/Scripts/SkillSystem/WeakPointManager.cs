using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 弱点管理器 - 管理所有敌人的弱点系统
/// 单例 MonoBehaviour，负责弱点的生成、判定、刷新和清理
/// </summary>
public class WeakPointManager : MonoBehaviour
{
    public static WeakPointManager Instance { get; private set; }
    
    // 配置参数
    private GameObject markerPrefab;
    private float radius = 0.5f;
    private float damageMultiplier = 1.5f;
    private bool refreshOnHit = true;
    
    // 运行时数据
    private Dictionary<Enemy, WeakPointData> weakPoints = new Dictionary<Enemy, WeakPointData>();
    private bool isEnabled = false;
    
    [Header("调试")]
    [SerializeField] private bool showDebugLog = true;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (showDebugLog)
                Debug.Log("[WeakPointManager] 单例创建成功");
        }
        else
        {
            Debug.LogWarning("[WeakPointManager] 检测到重复实例，销毁");
            Destroy(gameObject);
        }
    }
    
    /// <summary>
    /// 获取或创建管理器实例
    /// </summary>
    public static WeakPointManager GetOrCreateInstance()
    {
        if (Instance != null)
            return Instance;
        
        GameObject managerObj = new GameObject("WeakPointManager");
        return managerObj.AddComponent<WeakPointManager>();
    }
    
    /// <summary>
    /// 配置管理器参数
    /// </summary>
    public void Configure(
        GameObject prefab,
        float radius,
        float damageMultiplier,
        bool refreshOnHit)
    {
        this.markerPrefab = prefab;
        this.radius = radius;
        this.damageMultiplier = damageMultiplier;
        this.refreshOnHit = refreshOnHit;
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 配置完成 - 半径:{radius}, 倍率:{damageMultiplier}x, 击中刷新:{refreshOnHit}");
        }
    }
    
    /// <summary>
    /// 启用弱点系统
    /// </summary>
    public void Enable()
    {
        if (isEnabled)
        {
            Debug.LogWarning("[WeakPointManager] 系统已启用，忽略重复调用");
            return;
        }
        
        isEnabled = true;
        
        if (showDebugLog)
            Debug.Log("[WeakPointManager] 启用弱点系统");
        
        // 订阅事件（包括敌人生成完成事件）
        SubscribeToEvents();
        
        // 注意：不再在启用时立即扫描敌人
        // 而是等待 OnInitialWaveSpawnComplete 事件
    }
    
    /// <summary>
    /// 禁用弱点系统
    /// </summary>
    public void Disable()
    {
        if (!isEnabled)
            return;
        
        isEnabled = false;
        
        if (showDebugLog)
            Debug.Log("[WeakPointManager] 禁用弱点系统");
        
        // 清理所有弱点标记
        CleanupAllWeakPoints();
        
        // 取消订阅
        UnsubscribeFromEvents();
    }
    
    #region 敌人管理
    
    /// <summary>
    /// 为现有敌人初始化弱点
    /// </summary>
    private void InitializeExistingEnemies()
    {
        if (!isEnabled) return;
        
        int enemiesAddedCount = 0;
        
        // 方案1：通过 EnemyController 获取
        EnemyController enemyController = FindFirstObjectByType<EnemyController>();
        if (enemyController != null)
        {
            foreach (Enemy enemy in enemyController.AllEnemies)
            {
                if (enemy != null && !weakPoints.ContainsKey(enemy))
                {
                    AddWeakPointToEnemy(enemy);
                    enemiesAddedCount++;
                }
            }
        }
        
        // 方案2：直接查找场景中的所有敌人（兜底）
        Enemy[] enemies = FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null && !weakPoints.ContainsKey(enemy))
            {
                AddWeakPointToEnemy(enemy);
                enemiesAddedCount++;
            }
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 为 {enemiesAddedCount} 个新敌人添加了弱点 (总计: {weakPoints.Count} 个敌人有弱点)");
        }
    }
    
    /// <summary>
    /// 为单个敌人添加弱点
    /// </summary>
    private void AddWeakPointToEnemy(Enemy enemy)
    {
        if (enemy == null || weakPoints.ContainsKey(enemy))
            return;
        
        // 创建弱点数据
        WeakPointData data = new WeakPointData();
        data.currentDirection = Random.Range(0, 4); // 随机初始方向
        
        // 计算局部坐标
        Vector2 localPos = data.GetLocalPosition(radius);
        
        // 实例化弱点标记预制体
        GameObject markerObj = Instantiate(markerPrefab);
        
        // 初始化标记组件
        WeakPointMarker marker = markerObj.GetComponent<WeakPointMarker>();
        if (marker != null)
        {
            // 获取敌人的 enemyItem Transform（实际可见的敌人物体）
            Transform enemyTransform = enemy.enemyItem != null ? enemy.enemyItem : enemy.transform;
            marker.Initialize(enemyTransform, localPos);
        }
        else
        {
            Debug.LogError($"[WeakPointManager] 预制体缺少 WeakPointMarker 组件！");
            Destroy(markerObj);
            return;
        }
        
        data.markerObject = markerObj;
        weakPoints[enemy] = data;
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 为敌人 {enemy.name} 添加弱点 - 方向:{data.currentDirection}");
        }
    }
    
    /// <summary>
    /// 刷新弱点位置
    /// </summary>
    private void RefreshWeakPoint(Enemy enemy)
    {
        if (!weakPoints.ContainsKey(enemy))
            return;
        
        WeakPointData data = weakPoints[enemy];
        
        // 生成新方向（避免重复）
        int newDirection = data.GenerateNewDirection();
        data.currentDirection = newDirection;
        
        // 更新标记位置
        Vector2 newLocalPos = data.GetLocalPosition(radius);
        if (data.markerObject != null)
        {
            WeakPointMarker marker = data.markerObject.GetComponent<WeakPointMarker>();
            marker?.UpdatePosition(newLocalPos);
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 刷新敌人 {enemy.name} 弱点 - 新方向:{newDirection}");
        }
    }
    
    /// <summary>
    /// 刷新所有弱点
    /// </summary>
    private void RefreshAllWeakPoints()
    {
        foreach (var enemy in new List<Enemy>(weakPoints.Keys))
        {
            if (enemy != null)
            {
                RefreshWeakPoint(enemy);
            }
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 刷新所有弱点 - 共 {weakPoints.Count} 个");
        }
    }
    
    /// <summary>
    /// 清理所有弱点
    /// </summary>
    private void CleanupAllWeakPoints()
    {
        foreach (var kvp in weakPoints)
        {
            if (kvp.Value.markerObject != null)
            {
                Destroy(kvp.Value.markerObject);
            }
        }
        weakPoints.Clear();
        
        if (showDebugLog)
        {
            Debug.Log("[WeakPointManager] 清理所有弱点标记");
        }
    }
    
    #endregion
    
    #region 事件系统
    
    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 订阅攻击事件（用于判定和修改伤害）
        GameEventBus.OnAttack += OnAttackEvent;
        
        // 订阅敌人死亡事件（清理弱点）
        GameEventBus.OnDeath += OnDeathEvent;
        
        // 订阅阶段变化事件（回合刷新）
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
        
        // 订阅初始敌人生成完成事件
        GameEventBus.OnInitialWaveSpawnComplete += HandleInitialWaveSpawnComplete;
        
        if (showDebugLog)
        {
            Debug.Log("[WeakPointManager] 事件订阅完成");
        }
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        GameEventBus.OnAttack -= OnAttackEvent;
        GameEventBus.OnDeath -= OnDeathEvent;
        GameEventBus.OnGameFlowStateChanged -= OnGameFlowStateChanged;
        GameEventBus.OnInitialWaveSpawnComplete -= HandleInitialWaveSpawnComplete;
        
        if (showDebugLog)
        {
            Debug.Log("[WeakPointManager] 取消事件订阅");
        }
    }
    
    /// <summary>
    /// 处理初始敌人生成完成事件
    /// </summary>
    private void HandleInitialWaveSpawnComplete()
    {
        if (!isEnabled)
            return;
        
        if (showDebugLog)
            Debug.Log("[WeakPointManager] 接收到初始敌人生成完成事件，开始扫描现有敌人");
        
        // 现在扫描并添加弱点
        InitializeExistingEnemies();
    }
    
    /// <summary>
    /// 攻击事件处理（判定并修改伤害）
    /// </summary>
    private void OnAttackEvent(AttackData attackData)
    {
        if (!isEnabled)
            return;
        
        // 只处理玩家攻击敌人的情况
        if (attackData.Attacker == null || !attackData.Attacker.CompareTag("Player"))
            return;
        
        if (attackData.Target == null)
            return;
        
        // 查找目标敌人
        Enemy targetEnemy = attackData.Target.GetComponent<Enemy>();
        if (targetEnemy == null)
            targetEnemy = attackData.Target.GetComponentInParent<Enemy>();
        
        if (targetEnemy == null || !weakPoints.ContainsKey(targetEnemy))
            return;
        
        // 判定是否命中弱点（角度扇区判定）
        if (IsWeakPointHit(targetEnemy, attackData.Position))
        {
            // 修改伤害值
            float originalDamage = attackData.Damage;
            attackData.Damage *= damageMultiplier;
            
            Debug.Log($"[WeakPointManager] 🎯 弱点命中！伤害: {originalDamage:F1} → {attackData.Damage:F1}");
            
            // 播放命中特效
            WeakPointData data = weakPoints[targetEnemy];
            if (data.markerObject != null)
            {
                WeakPointMarker marker = data.markerObject.GetComponent<WeakPointMarker>();
                marker?.OnHit();
            }
            
            // 如果配置为击中刷新
            if (refreshOnHit)
            {
                RefreshWeakPoint(targetEnemy);
            }
        }
    }
    
    /// <summary>
    /// 判定是否命中弱点（角度扇区判定）
    /// </summary>
    private bool IsWeakPointHit(Enemy enemy, Vector3 hitPosition)
    {
        if (!weakPoints.ContainsKey(enemy))
            return false;
        
        WeakPointData data = weakPoints[enemy];
        
        // 1. 计算碰撞方向（使用正确的敌人位置）
        Vector3 enemyPos = enemy.enemyItem != null ? enemy.enemyItem.position : enemy.transform.position;
        Vector2 toHit = ((Vector2)(hitPosition - enemyPos)).normalized;
        
        // 2. 计算角度（-180 ~ 180）
        float hitAngle = Mathf.Atan2(toHit.y, toHit.x) * Mathf.Rad2Deg;
        
        // 3. 归一化到 0-360
        if (hitAngle < 0) hitAngle += 360f;
        
        // 4. 计算扇区索引（添加45度偏移，使0度对应上方）
        float adjustedAngle = (hitAngle + 45f) % 360f;
        int sectorIndex = Mathf.FloorToInt(adjustedAngle / 90f);
        
        // 5. 比较扇区与弱点方向
        bool isHit = (sectorIndex == data.currentDirection);
        
        if (showDebugLog)
        {
            Debug.Log($"[WeakPointManager] 判定 - 角度:{hitAngle:F1}°, 扇区:{sectorIndex}, 弱点方向:{data.currentDirection}, 结果:{(isHit ? "命中" : "未命中")}");
        }
        
        return isHit;
    }
    
    /// <summary>
    /// 死亡事件处理
    /// </summary>
    private void OnDeathEvent(DeathData deathData)
    {
        if (!isEnabled)
            return;
        
        if (deathData.DeadObject != null)
        {
            Enemy enemy = deathData.DeadObject.GetComponent<Enemy>();
            if (enemy == null)
                enemy = deathData.DeadObject.GetComponentInParent<Enemy>();
            
            if (enemy != null && weakPoints.ContainsKey(enemy))
            {
                // 销毁标记
                if (weakPoints[enemy].markerObject != null)
                {
                    Destroy(weakPoints[enemy].markerObject);
                }
                weakPoints.Remove(enemy);
                
                if (showDebugLog)
                {
                    Debug.Log($"[WeakPointManager] 敌人死亡，清理弱点 - {enemy.name}");
                }
            }
        }
    }
    
    /// <summary>
    /// 游戏阶段变化处理（回合刷新）
    /// </summary>
    private void OnGameFlowStateChanged(GameFlowState state)
    {
        if (!isEnabled)
            return;
        
        // 在玩家回合开始时刷新所有弱点
        if (state == GameFlowState.PlayerPhase)
        {
            if (showDebugLog)
            {
                Debug.Log("[WeakPointManager] 新回合开始，刷新所有弱点");
            }
            
            RefreshAllWeakPoints();
        }
    }
    
    #endregion
    
    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}

