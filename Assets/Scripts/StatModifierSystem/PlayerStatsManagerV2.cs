using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家属性管理器 V2 - 使用轻量级 Modifier 系统
/// 
/// 【核心改进】：
/// - 使用 RuntimeStatsManager（轻量级系统）
/// - Modifier 改为 struct，减少 GC 压力
/// - ModifierList 提供 O(1) 总值访问
/// - 生命周期管理分离到 ModifierHandle
/// 
/// 【向后兼容】：
/// - 保持与旧 PlayerStatsManager 相同的公共接口
/// - 支持旧的 StatModifier 类型（适配器模式）
/// - 可以直接替换旧版本使用
/// </summary>
public class PlayerStatsManagerV2 : MonoBehaviour
{
    #region 配置
    
    [Header("基础数据")]
    private PlayerData playerData;
    
    [Header("调试设置")]
    public bool enableDebugLog = true;
    
    #endregion
    
    #region 核心系统
    
    /// <summary>
    /// 运行时属性管理器（新系统）
    /// </summary>
    private RuntimeStatsManager runtimeStats;
    
    /// <summary>
    /// 修改器句柄映射（用于兼容旧系统）
    /// Key: 旧的 StatModifier, Value: ModifierHandle
    /// </summary>
    private Dictionary<StatModifier, ModifierHandle> modifierHandleMap = new Dictionary<StatModifier, ModifierHandle>();
    
    #endregion
    
    #region Unity 生命周期
    
    /// <summary>
    /// 初始化属性管理器（由 Player 调用）
    /// </summary>
    public void Initialize()
    {
        InitializeStatsManager();
    }
    
    /// <summary>
    /// 设置 PlayerData（由 Player 调用）
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
        Debug.Log("PlayerStatsManagerV2: PlayerData 已设置");
    }
    
    void Start()
    {
        // 如果 Player 还没有调用 Initialize，则自动初始化
        if (playerData == null)
        {
            Debug.LogWarning("PlayerStatsManagerV2: Player 尚未调用 Initialize，自动初始化");
            InitializeStatsManager();
        }
    }
    
    /// <summary>
    /// 初始化属性管理器
    /// </summary>
    void InitializeStatsManager()
    {
        // 创建运行时属性管理器
        runtimeStats = new RuntimeStatsManager(enableDebugLog);
        
        // 注册基础属性
        if (playerData != null)
        {
            RegisterBaseStats();
        }
        
        // 订阅事件
        SubscribeToEvents();
        
        if (enableDebugLog)
        {
            Debug.Log("PlayerStatsManagerV2: 初始化完成，使用轻量级 Modifier 系统");
        }
    }
    
    /// <summary>
    /// 注册基础属性
    /// </summary>
    private void RegisterBaseStats()
    {
        var baseStats = new Dictionary<string, float>
        {
            { "MaxHealth", playerData.baseMaxHealth },
            { "MicroMoveSpeed", playerData.baseMicroMoveSpeed },
            { "Damage", GetBaseDamage() },
            { "AreaRadius", playerData.areaRadius }
        };
        
        runtimeStats.RegisterStats(baseStats);
    }
    
    /// <summary>
    /// 获取基础攻击力
    /// </summary>
    private float GetBaseDamage()
    {
        if (playerData == null) return 0f;
        
        return playerData.attackMode == PlayerData.AttackMode.Collision 
            ? playerData.collisionDamage 
            : playerData.areaDamage;
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    private void SubscribeToEvents()
    {
        GameEventBus.OnBallStopped += HandleBallStopped;
        GameEventBus.OnGameFlowStateChanged += HandleGameFlowStateChanged;
        GameEventBus.OnHealthChanged += HandleHealthChanged;
        GameEventBus.OnLevelCompleted += HandleLevelCompleted;
    }
    
    void Update()
    {
        // 更新临时修改器
        if (runtimeStats != null)
        {
            runtimeStats.UpdateModifiers(Time.deltaTime);
        }
    }
    
    void OnDestroy()
    {
        // 取消事件订阅
        GameEventBus.OnBallStopped -= HandleBallStopped;
        GameEventBus.OnGameFlowStateChanged -= HandleGameFlowStateChanged;
        GameEventBus.OnHealthChanged -= HandleHealthChanged;
        GameEventBus.OnLevelCompleted -= HandleLevelCompleted;
    }
    
    #endregion
    
    #region 公共接口（向后兼容）
    
    /// <summary>
    /// 应用修饰器（兼容旧接口）
    /// </summary>
    public void ApplyModifier(StatModifier oldModifier)
    {
        if (oldModifier == null)
        {
            Debug.LogError("PlayerStatsManagerV2: 尝试应用空的修饰器");
            return;
        }
        
        // 转换为新系统
        ModifierHandle handle = null;
        
        // 根据旧的修改器类型转换
        bool isPercent = (oldModifier.type == StatModifierType.PercentAdd || 
                          oldModifier.type == StatModifierType.PercentMult);
        
        // 创建新的 Modifier（轻量级）
        Modifier newModifier = new Modifier(oldModifier.targetStat, oldModifier.value);
        
        // 根据旧修改器的属性选择创建方式
        if (oldModifier.effectRemovalCondition != null)
        {
            // 带移除条件
            handle = runtimeStats.AddConditionalModifier(
                oldModifier.targetStat,
                oldModifier.value,
                isPercent,
                oldModifier.effectRemovalCondition,
                oldModifier.source
            );
        }
        else if (oldModifier.duration > 0)
        {
            // 临时修改器
            handle = runtimeStats.AddTemporaryModifier(
                oldModifier.targetStat,
                oldModifier.value,
                isPercent,
                oldModifier.duration,
                oldModifier.source
            );
        }
        else
        {
            // 永久修改器
            if (isPercent)
            {
                handle = runtimeStats.AddPercentModifier(
                    oldModifier.targetStat,
                    oldModifier.value,
                    oldModifier.source
                );
            }
            else
            {
                handle = runtimeStats.AddConstantModifier(
                    oldModifier.targetStat,
                    oldModifier.value,
                    oldModifier.source
                );
            }
        }
        
        // 保存映射关系
        if (handle != null)
        {
            modifierHandleMap[oldModifier] = handle;
        }
        
        // 触发属性变化
        OnStatChanged(oldModifier.targetStat);
    }
    
    /// <summary>
    /// 移除修饰器（兼容旧接口）
    /// </summary>
    public void RemoveModifier(StatModifier oldModifier)
    {
        if (oldModifier == null) return;
        
        // 查找对应的句柄
        if (modifierHandleMap.TryGetValue(oldModifier, out var handle))
        {
            runtimeStats.RemoveModifier(oldModifier.targetStat, handle);
            modifierHandleMap.Remove(oldModifier);
            
            // 触发属性变化
            OnStatChanged(oldModifier.targetStat);
        }
        else
        {
            Debug.LogWarning($"PlayerStatsManagerV2: 未找到修改器句柄，无法移除");
        }
    }
    
    /// <summary>
    /// 移除指定来源的所有修饰器（兼容旧接口）
    /// </summary>
    public void RemoveModifiersBySource(object source)
    {
        // 找到所有匹配来源的旧修改器
        var modifiersToRemove = new List<StatModifier>();
        foreach (var kvp in modifierHandleMap)
        {
            if (kvp.Value.Source == source)
            {
                modifiersToRemove.Add(kvp.Key);
            }
        }
        
        // 移除它们
        foreach (var oldModifier in modifiersToRemove)
        {
            RemoveModifier(oldModifier);
        }
        
        if (enableDebugLog && modifiersToRemove.Count > 0)
        {
            Debug.Log($"PlayerStatsManagerV2: 移除来源 {source?.GetType().Name} 的 {modifiersToRemove.Count} 个修饰器");
        }
    }
    
    /// <summary>
    /// 检查修饰器是否存在于活跃列表中（兼容旧接口）
    /// </summary>
    /// <param name="modifier">要检查的修饰器</param>
    /// <returns>是否存在</returns>
    public bool HasModifier(StatModifier modifier)
    {
        return modifierHandleMap.ContainsKey(modifier);
    }
    
    #endregion
    
    #region 属性访问（向后兼容）
    
    /// <summary>
    /// 获取最终最大血量
    /// </summary>
    public float FinalMaxHealth => GetFinalStat("MaxHealth");
    
    /// <summary>
    /// 获取最终微调移动速度
    /// </summary>
    public float FinalMicroMoveSpeed => GetFinalStat("MicroMoveSpeed");
    
    /// <summary>
    /// 获取最终攻击力
    /// </summary>
    public float FinalDamage => GetFinalStat("Damage");
    
    /// <summary>
    /// 获取最终攻击范围（仅范围攻击模式有效）
    /// </summary>
    public float FinalAreaRadius => GetFinalStat("AreaRadius");
    
    /// <summary>
    /// 获取指定属性的最终值
    /// </summary>
    public float GetFinalStat(string statName)
    {
        // ✅ 优先从静态数据读取最终值（与旧系统兼容）
        switch (statName)
        {
            case "MaxHealth":
                if (GameRuntimeData.HasMaxHealthData())
                    return GameRuntimeData.GetMaxHealth();
                break;
            case "Damage":
                if (GameRuntimeData.HasDamageData())
                    return GameRuntimeData.GetDamage();
                break;
            case "AreaRadius":
                if (GameRuntimeData.HasAttackRangeData())
                    return GameRuntimeData.GetAttackRange();
                break;
        }
        
        // 从新系统获取
        return runtimeStats.GetStatValue(statName);
    }
    
    /// <summary>
    /// 获取基础属性值（兼容旧接口）
    /// </summary>
    public float GetBaseStat(string statName)
    {
        return runtimeStats.GetBaseValue(statName);
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 处理球停止运动事件
    /// </summary>
    private void HandleBallStopped(BallPhysics ballPhysics)
    {
        var args = SkillArgs.FromEventData(ballPhysics);
        runtimeStats.CheckEventBasedRemoval(args);
    }
    
    /// <summary>
    /// 处理游戏流程状态变化事件
    /// </summary>
    private void HandleGameFlowStateChanged(GameFlowState gameFlowState)
    {
        var gameFlowData = new GameFlowStateChangedData { NewState = gameFlowState };
        var args = SkillArgs.FromEventData(gameFlowData);
        runtimeStats.CheckEventBasedRemoval(args);
    }
    
    /// <summary>
    /// 处理血量变化事件
    /// </summary>
    private void HandleHealthChanged(HealthStateData healthData)
    {
        var args = SkillArgs.FromEventData(healthData);
        runtimeStats.CheckEventBasedRemoval(args);
    }
    
    /// <summary>
    /// 处理关卡完成事件
    /// </summary>
    private void HandleLevelCompleted(int levelIndex, LevelConfig levelConfig)
    {
        var levelCompletedData = new LevelCompletedData(levelIndex, levelConfig);
        var args = SkillArgs.FromEventData(levelCompletedData);
        runtimeStats.CheckEventBasedRemoval(args);
    }
    
    /// <summary>
    /// 属性变化时的回调
    /// </summary>
    private void OnStatChanged(string statName)
    {
        // 计算最终值并保存到静态数据
        float finalValue = runtimeStats.GetStatValue(statName);
        
        // 保存到静态数据（与旧系统兼容）
        switch (statName)
        {
            case "MaxHealth":
                GameRuntimeData.SetMaxHealth(finalValue);
                break;
            case "Damage":
                GameRuntimeData.SetDamage(finalValue);
                break;
            case "AreaRadius":
                GameRuntimeData.SetAttackRange(finalValue);
                break;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"PlayerStatsManagerV2: {statName} 变化，最终值: {finalValue:F2}，已保存到 GameRuntimeData");
        }
    }
    
    #endregion
    
    #region 新系统直接访问（推荐使用）
    
    /// <summary>
    /// 添加固定值修改器（新接口）
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值</param>
    /// <param name="source">来源</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddConstant(string statID, float value, object source = null)
    {
        var handle = runtimeStats.AddConstantModifier(statID, value, source);
        OnStatChanged(statID);
        return handle;
    }
    
    /// <summary>
    /// 添加百分比修改器（新接口）
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值（例如 0.5 = +50%）</param>
    /// <param name="source">来源</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddPercent(string statID, float value, object source = null)
    {
        var handle = runtimeStats.AddPercentModifier(statID, value, source);
        OnStatChanged(statID);
        return handle;
    }
    
    /// <summary>
    /// 添加临时修改器（新接口）
    /// </summary>
    public ModifierHandle AddTemporary(string statID, float value, bool isPercent, float duration, object source = null)
    {
        var handle = runtimeStats.AddTemporaryModifier(statID, value, isPercent, duration, source);
        OnStatChanged(statID);
        return handle;
    }
    
    /// <summary>
    /// 移除修改器（新接口）
    /// </summary>
    public bool RemoveModifier(string statID, ModifierHandle handle)
    {
        bool removed = runtimeStats.RemoveModifier(statID, handle);
        if (removed)
        {
            OnStatChanged(statID);
        }
        return removed;
    }
    
    #endregion
    
    #region 调试和监控
    
    /// <summary>
    /// 获取所有属性的调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return runtimeStats.GetDebugInfo();
    }
    
    /// <summary>
    /// 获取最终属性值调试信息
    /// </summary>
    public string GetFinalStatsDebugInfo()
    {
        return $"最终属性值:\n" +
               $"- 攻击力: {FinalDamage:F2}\n" +
               $"- 最大血量: {FinalMaxHealth:F2}\n" +
               $"- 微调移动速度: {FinalMicroMoveSpeed:F2}\n" +
               $"- 攻击范围: {FinalAreaRadius:F2}";
    }
    
    #endregion
}

