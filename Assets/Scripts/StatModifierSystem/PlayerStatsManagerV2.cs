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
    
    #region 核心系统（三层属性）
    
    /// <summary>
    /// Stats 层 - 基础属性（攻击力、速度等）
    /// </summary>
    private RuntimeStatsManager runtimeStats;
    
    /// <summary>
    /// Attributes 层 - 动态资源（生命值、能量等）
    /// </summary>
    private RuntimeAttributes runtimeAttributes;
    
    /// <summary>
    /// StatusEffects 层 - 状态效果（中毒、加速等）
    /// </summary>
    private RuntimeStatusEffects runtimeStatusEffects;
    
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
        // ✅ 创建三层属性系统
        runtimeStats = new RuntimeStatsManager(enableDebugLog);
        runtimeAttributes = new RuntimeAttributes(enableDebugLog);
        runtimeStatusEffects = new RuntimeStatusEffects(enableDebugLog);
        
        // 注册基础属性和资源
        if (playerData != null)
        {
            RegisterBaseStats();
            RegisterBaseAttributes();
        }
        
        // ✅ 尝试从 GameSession 恢复数据（跨场景持久化）
        RestoreFromGameSession();
        
        // 订阅事件
        SubscribeToEvents();
        
        if (enableDebugLog)
        {
            Debug.Log("PlayerStatsManagerV2: ✅ 初始化完成，三层属性系统已就绪");
        }
    }
    
    /// <summary>
    /// 注册基础属性（Stats 层）
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
    /// 注册基础属性资源（Attributes 层）
    /// </summary>
    private void RegisterBaseAttributes()
    {
        // 注册生命值属性（动态资源）
        runtimeAttributes.RegisterAttribute(
            "Health",                      // attributeID
            0f,                           // minValue
            playerData.baseMaxHealth,     // maxValue
            playerData.baseMaxHealth      // startValue（满血开始）
        );
        
        if (enableDebugLog)
        {
            Debug.Log($"PlayerStatsManagerV2: 注册 Health 属性资源，最大值: {playerData.baseMaxHealth}");
        }
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
        // ✅ 更新三层属性系统
        if (runtimeStats != null)
        {
            runtimeStats.UpdateModifiers(Time.deltaTime);
        }
        
        if (runtimeStatusEffects != null)
        {
            runtimeStatusEffects.UpdateEffects(Time.deltaTime);
        }
    }
    
    void OnDestroy()
    {
        // ✅ 保存数据到 GameSession（跨场景持久化）
        SaveToGameSession();
        
        // 取消事件订阅
        GameEventBus.OnBallStopped -= HandleBallStopped;
        GameEventBus.OnGameFlowStateChanged -= HandleGameFlowStateChanged;
        GameEventBus.OnHealthChanged -= HandleHealthChanged;
        GameEventBus.OnLevelCompleted -= HandleLevelCompleted;
    }
    
    #endregion
    
    #region 批量操作
    
    /// <summary>
    /// 移除指定来源的所有修饰器
    /// </summary>
    public int RemoveModifiersBySource(object source)
    {
        int totalRemoved = runtimeStats.RemoveModifiersBySource(source);
        
        if (totalRemoved > 0)
        {
            // 触发所有相关属性的变化事件
            OnStatChanged("MaxHealth");
            OnStatChanged("Damage");
            OnStatChanged("MicroMoveSpeed");
            OnStatChanged("AreaRadius");
            
            if (enableDebugLog)
            {
                Debug.Log($"PlayerStatsManagerV2: 移除来源 {source?.GetType().Name} 的 {totalRemoved} 个修饰器");
            }
        }
        
        return totalRemoved;
    }
    
    #endregion
    
    #region 属性访问
    
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
        // ✅ 直接从新系统获取（已废弃 GameRuntimeData 静态存储）
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
        
        // ✅ 已废弃 GameRuntimeData 静态存储，数值由 runtimeStats 直接管理
        
        if (enableDebugLog)
        {
            Debug.Log($"PlayerStatsManagerV2: ✅ {statName} 变化，最终值: {finalValue:F2}");
        }
    }
    
    #endregion
    
    #region 修改器管理（核心接口）
    
    /// <summary>
    /// 添加固定值修改器
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
    /// 添加百分比修改器
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
    /// 添加临时修改器
    /// </summary>
    public ModifierHandle AddTemporary(string statID, float value, bool isPercent, float duration, object source = null)
    {
        var handle = runtimeStats.AddTemporaryModifier(statID, value, isPercent, duration, source);
        OnStatChanged(statID);
        return handle;
    }
    
    /// <summary>
    /// 添加带移除条件的修改器
    /// </summary>
    public ModifierHandle AddConditionalModifier(string statID, float value, bool isPercent, IEffectRemovalCondition removalCondition, object source = null)
    {
        var handle = runtimeStats.AddConditionalModifier(statID, value, isPercent, removalCondition, source);
        OnStatChanged(statID);
        return handle;
    }
    
    /// <summary>
    /// 移除修改器
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
    
    #region Attributes 层访问接口
    
    /// <summary>
    /// 获取生命值当前值
    /// </summary>
    public float CurrentHealth => runtimeAttributes.GetCurrentValue("Health");
    
    /// <summary>
    /// 获取生命值最大值
    /// </summary>
    public float MaxHealth => runtimeAttributes.GetMaxValue("Health");
    
    /// <summary>
    /// 获取生命值百分比
    /// </summary>
    public float HealthRatio => runtimeAttributes.GetRatio("Health");
    
    /// <summary>
    /// 设置生命值
    /// </summary>
    public void SetHealth(float value)
    {
        runtimeAttributes.SetValue("Health", value);
    }
    
    /// <summary>
    /// 增加生命值
    /// </summary>
    public void AddHealth(float amount)
    {
        runtimeAttributes.Add("Health", amount);
    }
    
    /// <summary>
    /// 减少生命值
    /// </summary>
    public void SubtractHealth(float amount)
    {
        runtimeAttributes.Subtract("Health", amount);
    }
    
    /// <summary>
    /// 获取 RuntimeAttribute 对象（用于高级操作）
    /// </summary>
    public RuntimeAttribute GetHealthAttribute()
    {
        return runtimeAttributes.GetAttribute("Health");
    }
    
    #endregion
    
    #region StatusEffects 层访问接口
    
    /// <summary>
    /// 添加状态效果
    /// </summary>
    public RuntimeStatusEffect AddStatusEffect(StatusEffectData effectData, object source = null)
    {
        return runtimeStatusEffects.AddEffect(effectData, source);
    }
    
    /// <summary>
    /// 移除状态效果
    /// </summary>
    public bool RemoveStatusEffect(RuntimeStatusEffect effect)
    {
        return runtimeStatusEffects.RemoveEffect(effect);
    }
    
    /// <summary>
    /// 根据ID移除状态效果
    /// </summary>
    public bool RemoveStatusEffectByID(string effectID)
    {
        return runtimeStatusEffects.RemoveEffectByID(effectID);
    }
    
    /// <summary>
    /// 检查是否有指定状态效果
    /// </summary>
    public bool HasStatusEffect(string effectID)
    {
        return runtimeStatusEffects.HasEffect(effectID);
    }
    
    /// <summary>
    /// 获取所有激活的状态效果
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<RuntimeStatusEffect> GetAllStatusEffects()
    {
        return runtimeStatusEffects.GetAllEffects();
    }
    
    #endregion
    
    #region 调试和监控
    
    /// <summary>
    /// 获取三层属性系统的完整调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = "=== 玩家属性系统（三层架构） ===\n\n";
        
        // Stats 层
        info += "【1. Stats 层 - 基础属性】\n";
        info += runtimeStats.GetDebugInfo();
        info += "\n";
        
        // Attributes 层
        info += "【2. Attributes 层 - 动态资源】\n";
        info += runtimeAttributes.GetDebugInfo();
        info += "\n";
        
        // StatusEffects 层
        info += "【3. StatusEffects 层 - 状态效果】\n";
        info += runtimeStatusEffects.GetDebugInfo();
        
        return info;
    }
    
    /// <summary>
    /// 获取最终属性值调试信息（简化版）
    /// </summary>
    public string GetFinalStatsDebugInfo()
    {
        return $"=== 最终属性值 ===\n" +
               $"Stats 层:\n" +
               $"  - 攻击力: {FinalDamage:F2}\n" +
               $"  - 最大血量: {FinalMaxHealth:F2}\n" +
               $"  - 微调移动速度: {FinalMicroMoveSpeed:F2}\n" +
               $"  - 攻击范围: {FinalAreaRadius:F2}\n" +
               $"\n" +
               $"Attributes 层:\n" +
               $"  - 当前生命值: {CurrentHealth:F1}/{MaxHealth:F1} ({HealthRatio * 100:F0}%)\n" +
               $"\n" +
               $"StatusEffects 层:\n" +
               $"  - 激活效果数: {runtimeStatusEffects?.ActiveCount ?? 0}";
    }
    
    #endregion
    
    #region GameSession 集成（跨场景数据持久化）
    
    /// <summary>
    /// 保存数据到 GameSession（场景销毁前调用）
    /// </summary>
    private void SaveToGameSession()
    {
        // ✅ 使用 GetOrCreateInstance 确保 GameSession 存在
        var session = GameSession.GetOrCreateInstance();
        if (session == null)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[PlayerStatsManagerV2] ⚠️ GameSession 创建失败，无法保存数据");
            }
            return;
        }
        
        // 导出 Attributes 当前值
        if (runtimeAttributes != null)
        {
            session.PlayerData.attributeCurrentValues = runtimeAttributes.ExportCurrentValues();
        }
        
        // 导出 Stats 修改器（当前简化版本）
        if (runtimeStats != null)
        {
            session.PlayerData.activeModifiers = runtimeStats.ExportModifiers();
        }
        
        // 导出 StatusEffects（当前简化版本）
        if (runtimeStatusEffects != null)
        {
            session.PlayerData.activeStatusEffects = runtimeStatusEffects.ExportStatusEffects();
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerStatsManagerV2] 📤 已保存数据到 GameSession " +
                     $"(Attributes: {session.PlayerData.attributeCurrentValues.Count}, " +
                     $"Modifiers: {session.PlayerData.activeModifiers.Count}, " +
                     $"Effects: {session.PlayerData.activeStatusEffects.Count})");
        }
    }
    
    /// <summary>
    /// 从 GameSession 恢复数据（初始化后调用）
    /// </summary>
    private void RestoreFromGameSession()
    {
        // ✅ 使用 GetOrCreateInstance 确保 GameSession 存在
        var session = GameSession.GetOrCreateInstance();
        if (session == null || !session.HasPlayerData())
        {
            if (enableDebugLog)
            {
                Debug.Log("[PlayerStatsManagerV2] 📥 GameSession 无保存数据，使用默认初始值");
            }
            return;
        }
        
        var playerData = session.PlayerData;
        
        // 恢复 Attributes 当前值
        if (runtimeAttributes != null && playerData.attributeCurrentValues.Count > 0)
        {
            runtimeAttributes.RestoreCurrentValues(playerData.attributeCurrentValues);
        }
        
        // 恢复 Stats 修改器（当前简化版本，跳过）
        // 修改器通常由技能系统在场景加载时重新应用
        
        // 恢复 StatusEffects（当前简化版本，跳过）
        // 状态效果通常由技能系统在场景加载时重新应用
        
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerStatsManagerV2] 📥 已从 GameSession 恢复数据 " +
                     $"(Attributes: {playerData.attributeCurrentValues.Count})");
        }
    }
    
    #endregion
}

