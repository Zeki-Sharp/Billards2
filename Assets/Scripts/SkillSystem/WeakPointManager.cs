using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 弱点管理器 - 管理所有敌人的弱点系统
/// 单例 MonoBehaviour，负责弱点的生成、判定、刷新和清理
/// 实现 IDamageModifier 接口，作为高优先级伤害修改器
/// 
/// 【执行顺序】：SYSTEM 层 (-50)
/// 【依赖】：GameManager (CORE 层)
/// 【初始化】：OnManagerCreated 中启用系统和订阅事件
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class WeakPointManager : SingletonManager<WeakPointManager>, IDamageModifier
{
    
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
    
    #region IDamageModifier 实现
    
    /// <summary>
    /// 修改器优先级 - 高优先级，确保在伤害应用前执行
    /// </summary>
    public EventPriority Priority => EventPriority.High;
    
    /// <summary>
    /// 修改器名称
    /// </summary>
    public string ModifierName => "弱点判定";
    
    /// <summary>
    /// 是否启用此修改器
    /// </summary>
    public bool IsEnabled => isEnabled;
    
    /// <summary>
    /// 处理伤害修改 - 弱点判定和伤害修改
    /// </summary>
    /// <param name="attackData">攻击数据（可修改）</param>
    /// <returns>是否成功处理了伤害修改</returns>
    public bool ProcessDamage(ref AttackData attackData)
    {
        Debug.Log($"[WeakPointManager] ProcessDamage 被调用 - 攻击者: {attackData.Attacker?.name}, 目标: {attackData.Target?.name}, 伤害: {attackData.Damage}");
        
        if (!isEnabled)
        {
            Debug.Log("[WeakPointManager] 弱点系统未启用");
            return false;
        }
        
        // 只处理玩家攻击敌人的情况
        if (attackData.Attacker == null || !attackData.Attacker.CompareTag("Player"))
        {
            Debug.Log("[WeakPointManager] 不是玩家攻击，跳过");
            return false;
        }
        
        if (attackData.Target == null)
        {
            Debug.Log("[WeakPointManager] 攻击目标为空，跳过");
            return false;
        }
        
        // 查找目标敌人
        Enemy targetEnemy = attackData.Target.GetComponent<Enemy>();
        if (targetEnemy == null)
            targetEnemy = attackData.Target.GetComponentInParent<Enemy>();
        
        if (targetEnemy == null)
        {
            Debug.Log("[WeakPointManager] 目标不是敌人，跳过");
            return false;
        }
        
        if (!weakPoints.ContainsKey(targetEnemy))
        {
            Debug.Log($"[WeakPointManager] 敌人 {targetEnemy.name} 没有弱点，跳过");
            return false;
        }
        
        Debug.Log($"[WeakPointManager] 开始判定弱点命中 - 敌人: {targetEnemy.name}, 攻击位置: {attackData.Position}");
        
        // 判定是否命中弱点（角度扇区判定）
        if (IsWeakPointHit(targetEnemy, attackData.Position))
        {
            // 修改伤害值
            float originalDamage = attackData.Damage;
            attackData.Damage *= damageMultiplier;
            
            Debug.Log($"[WeakPointManager] 🎯 弱点命中! 伤害: {originalDamage} → {attackData.Damage}");
            
            // 刷新弱点位置（如果启用）
            if (refreshOnHit)
            {
                RefreshWeakPoint(targetEnemy);
            }
            
            return true; // 成功处理了伤害修改
        }
        
        Debug.Log("[WeakPointManager] 未命中弱点");
        return false; // 未命中弱点，未修改伤害
    }
    
    #endregion
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugLog;
    
    protected override void OnManagerCreated()
    {
        // ✅ Manager 自身初始化
        GameEventBus.OnGameRestart += ResetState;
        
        // ✅ 自动启用弱点系统，确保 DamageProcessor 能找到
        Enable();
        
        if (showDebugLog)
            Debug.Log("[WeakPointManager] 单例创建成功（SYSTEM 层）");
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅游戏重启事件
        GameEventBus.OnGameRestart -= ResetState;
        
        // 取消订阅其他事件
        UnsubscribeFromEvents();
    }
    
    #endregion
    
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
        
        // 方案1：通过 EnemyManager 单例获取
        EnemyManager enemyManager = EnemyManager.Instance;
        if (enemyManager != null)
        {
            foreach (Enemy enemy in enemyManager.AllEnemies)
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
            marker.Initialize(enemyTransform, localPos, data.currentDirection);
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
            marker?.UpdatePosition(newLocalPos, newDirection);
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
    
    /// <summary>
    /// 重置弱点管理器状态（游戏重启时调用）
    /// </summary>
    public void ResetState()
    {
        // 复用现有的清理方法
        CleanupAllWeakPoints();
        
        // 禁用弱点系统
        isEnabled = false;
        
        // 清空配置
        markerPrefab = null;
        
        if (showDebugLog)
        {
            Debug.Log("[WeakPointManager] 重置完成 - 所有弱点已清理，系统已禁用");
        }
    }
    
    #endregion
    
    #region 事件系统
    
    /// <summary>
    /// 订阅游戏事件
    /// </summary>
    private void SubscribeToEvents()
    {
        // 注意：攻击事件现在通过 DamageProcessor 自动处理，无需手动订阅
        // 订阅敌人死亡事件（清理弱点）
        GameEventBus.OnDeath += OnDeathEvent;
        
        // 订阅阶段变化事件（回合刷新）
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
        
        // 订阅初始敌人生成完成事件
        GameEventBus.OnInitialWaveSpawnComplete += HandleInitialWaveSpawnComplete;
        
        // 订阅波次敌人生成完成事件
        GameEventBus.OnWaveEnemiesSpawnComplete += HandleWaveEnemiesSpawnComplete;
        
        if (showDebugLog)
        {
            Debug.Log("[WeakPointManager] 事件订阅完成 (攻击事件通过 DamageProcessor 处理)");
        }
    }
    
    /// <summary>
    /// 取消订阅
    /// </summary>
    private void UnsubscribeFromEvents()
    {
        // 注意：攻击事件现在通过 DamageProcessor 自动处理，无需手动取消订阅
        GameEventBus.OnDeath -= OnDeathEvent;
        GameEventBus.OnGameFlowStateChanged -= OnGameFlowStateChanged;
        GameEventBus.OnInitialWaveSpawnComplete -= HandleInitialWaveSpawnComplete;
        GameEventBus.OnWaveEnemiesSpawnComplete -= HandleWaveEnemiesSpawnComplete;
        
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
    /// 处理波次敌人生成完成事件
    /// </summary>
    private void HandleWaveEnemiesSpawnComplete()
    {
        if (!isEnabled)
            return;
        
        if (showDebugLog)
            Debug.Log("[WeakPointManager] 接收到波次敌人生成完成事件，开始扫描新敌人");
        
        // 扫描并为新敌人添加弱点
        InitializeExistingEnemies();
    }
    
    // 注意：攻击事件处理现在通过 IDamageModifier.ProcessDamage 方法实现
    
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
}

