using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人属性管理器 - 使用三层属性系统
/// 
/// 【核心功能】：
/// - 使用 RuntimeStatsManager（基础属性）
/// - 使用 RuntimeAttributes（动态资源，血量）
/// - 使用 RuntimeStatusEffects（状态效果）
/// 
/// 【简化设计】：
/// - 不需要跨场景持久化（敌人会重新生成）
/// - 不使用 GameSession（敌人数据不保存）
/// - 不发布事件（血条直接调用更新）
/// 
/// 【架构统一】：
/// - 与 PlayerStats 架构一致
/// - 统一玩家和敌人的属性管理方式
/// </summary>
public class EnemyStats : MonoBehaviour
{
    #region 配置
    
    [Header("基础数据")]
    private EnemyData enemyData;
    private int currentLevel = 1;  // 当前等级
    
    [Header("调试设置")]
    public bool enableDebugLog = false;  // 敌人默认不输出日志
    
    #endregion
    
    #region 核心系统（三层属性）
    
    /// <summary>
    /// Stats 层 - 基础属性（伤害、速度等）
    /// </summary>
    private RuntimeStatsManager runtimeStats;
    
    /// <summary>
    /// Attributes 层 - 动态资源（生命值）
    /// </summary>
    private RuntimeAttributes runtimeAttributes;
    
    /// <summary>
    /// StatusEffects 层 - 状态效果（中毒、加速等）
    /// </summary>
    private RuntimeStatusEffects runtimeStatusEffects;
    
    #endregion
    
    #region Unity 生命周期
    
    /// <summary>
    /// 设置 EnemyData（由 EnemyBehavior 调用）
    /// </summary>
    public void SetEnemyData(EnemyData data)
    {
        enemyData = data;
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: EnemyData 已设置");
        }
    }
    
    /// <summary>
    /// 初始化属性管理器（由 EnemyBehavior 调用）
    /// </summary>
    public void Initialize(int level = 1)
    {
        currentLevel = level;
        InitializeStatsManager();
    }
    
    /// <summary>
    /// 初始化属性管理器
    /// </summary>
    void InitializeStatsManager()
    {
        if (enemyData == null)
        {
            Debug.LogError($"[EnemyStats] {gameObject.name}: EnemyData 未设置，无法初始化！");
            return;
        }
        
        // ✅ 创建三层属性系统
        runtimeStats = new RuntimeStatsManager(enableDebugLog);
        runtimeAttributes = new RuntimeAttributes(enableDebugLog);
        runtimeStatusEffects = new RuntimeStatusEffects(enableDebugLog);
        
        // 注册基础属性和资源
        RegisterBaseStats();
        RegisterBaseAttributes();
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: ✅ 初始化完成，三层属性系统已就绪");
        }
    }
    
    /// <summary>
    /// 注册基础属性（Stats 层）
    /// </summary>
    private void RegisterBaseStats()
    {
        // ✅ 从指定等级配置读取数值
        var levelConfig = enemyData.GetLevelConfig(currentLevel);
        
        // 读取数值（优先 levelConfig，回退到旧字段）
        float maxHealth = levelConfig?.maxHealth ?? enemyData.maxHealth;
        float damage = levelConfig?.damage ?? enemyData.damage;
        float moveSpeed = levelConfig?.moveSpeed ?? enemyData.moveSpeed;
        float attackRange = levelConfig?.attackRange ?? enemyData.attackRange;
        float attackCooldown = levelConfig?.attackCooldown ?? enemyData.attackCooldown;
        
        var baseStats = new Dictionary<string, float>
        {
            { "MaxHealth", maxHealth },
            { "Damage", damage },
            { "MoveSpeed", moveSpeed },
            { "AttackRange", attackRange },
            { "AttackCooldown", attackCooldown }
        };
        
        runtimeStats.RegisterStats(baseStats);
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name} Lv{currentLevel}: 注册基础属性 - MaxHealth: {maxHealth}, Damage: {damage}");
        }
    }
    
    /// <summary>
    /// 注册基础属性资源（Attributes 层）
    /// </summary>
    private void RegisterBaseAttributes()
    {
        // ✅ 从指定等级配置读取血量
        var levelConfig = enemyData.GetLevelConfig(currentLevel);
        float maxHealth = levelConfig?.maxHealth ?? enemyData.maxHealth;
        
        // 注册生命值属性（动态资源）
        runtimeAttributes.RegisterAttribute(
            "Health",           // attributeID
            0f,                // minValue
            maxHealth,         // maxValue
            maxHealth          // startValue（满血开始）
        );
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name} Lv{currentLevel}: 注册 Health 属性，最大值: {maxHealth}");
        }
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
    
    #endregion
    
    #region Stats 层访问接口
    
    /// <summary>
    /// 获取属性最终值
    /// </summary>
    public float GetFinalStat(string statID)
    {
        if (runtimeStats == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeStats 未初始化");
            return 0f;
        }
        
        return runtimeStats.GetStatValue(statID);
    }
    
    /// <summary>
    /// 添加属性修饰器（常数）
    /// </summary>
    public ModifierHandle AddModifier(string statID, float value, object source = null)
    {
        if (runtimeStats == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeStats 未初始化");
            return null;
        }
        
        return runtimeStats.AddConstantModifier(statID, value, source);
    }
    
    /// <summary>
    /// 添加属性修饰器（百分比）
    /// </summary>
    public ModifierHandle AddPercentModifier(string statID, float percentValue, object source = null)
    {
        if (runtimeStats == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeStats 未初始化");
            return null;
        }
        
        return runtimeStats.AddPercentModifier(statID, percentValue, source);
    }
    
    /// <summary>
    /// 移除属性修饰器
    /// </summary>
    public bool RemoveModifier(string statID, ModifierHandle handle)
    {
        if (runtimeStats == null) return false;
        return runtimeStats.RemoveModifier(statID, handle);
    }
    
    #endregion
    
    #region Attributes 层访问接口
    
    /// <summary>
    /// 获取生命值当前值
    /// </summary>
    public float CurrentHealth
    {
        get
        {
            if (runtimeAttributes == null) return 0f;
            return runtimeAttributes.GetCurrentValue("Health");
        }
    }
    
    /// <summary>
    /// 获取生命值最大值
    /// </summary>
    public float MaxHealth
    {
        get
        {
            if (runtimeAttributes == null) return 0f;
            return runtimeAttributes.GetMaxValue("Health");
        }
    }
    
    /// <summary>
    /// 获取生命值百分比
    /// </summary>
    public float HealthRatio
    {
        get
        {
            if (runtimeAttributes == null) return 0f;
            return runtimeAttributes.GetRatio("Health");
        }
    }
    
    /// <summary>
    /// 设置生命值
    /// </summary>
    public void SetHealth(float value)
    {
        if (runtimeAttributes == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeAttributes 未初始化");
            return;
        }
        
        runtimeAttributes.SetValue("Health", value);
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: SetHealth {value:F1}, 当前血量: {CurrentHealth:F1}/{MaxHealth:F1}");
        }
    }
    
    /// <summary>
    /// 增加生命值
    /// </summary>
    public void AddHealth(float amount)
    {
        if (runtimeAttributes == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeAttributes 未初始化");
            return;
        }
        
        float oldHealth = CurrentHealth;
        runtimeAttributes.Add("Health", amount);
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: 回血 {amount:F1}, {oldHealth:F1} → {CurrentHealth:F1}");
        }
    }
    
    /// <summary>
    /// 减少生命值
    /// </summary>
    public void SubtractHealth(float amount)
    {
        if (runtimeAttributes == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeAttributes 未初始化");
            return;
        }
        
        float oldHealth = CurrentHealth;
        runtimeAttributes.Subtract("Health", amount);
        
        if (enableDebugLog)
        {
            Debug.Log($"[EnemyStats] {gameObject.name}: 受伤 {amount:F1}, {oldHealth:F1} → {CurrentHealth:F1}");
        }
    }
    
    /// <summary>
    /// 获取指定 Attribute（通用接口，供 Property 系统使用）
    /// </summary>
    public RuntimeAttribute GetAttribute(string attributeID)
    {
        if (runtimeAttributes == null) return null;
        return runtimeAttributes.GetAttribute(attributeID);
    }
    
    #endregion
    
    #region StatusEffects 层访问接口
    
    /// <summary>
    /// 添加状态效果
    /// </summary>
    public RuntimeStatusEffect AddStatusEffect(StatusEffectData effectData, object source = null)
    {
        if (runtimeStatusEffects == null)
        {
            Debug.LogWarning($"[EnemyStats] {gameObject.name}: RuntimeStatusEffects 未初始化");
            return null;
        }
        
        return runtimeStatusEffects.AddEffect(effectData, source);
    }
    
    /// <summary>
    /// 移除状态效果
    /// </summary>
    public bool RemoveStatusEffect(RuntimeStatusEffect effect)
    {
        if (runtimeStatusEffects == null) return false;
        return runtimeStatusEffects.RemoveEffect(effect);
    }
    
    /// <summary>
    /// 根据ID移除状态效果
    /// </summary>
    public bool RemoveStatusEffectByID(string effectID)
    {
        if (runtimeStatusEffects == null) return false;
        return runtimeStatusEffects.RemoveEffectByID(effectID);
    }
    
    /// <summary>
    /// 检查是否有指定状态效果
    /// </summary>
    public bool HasStatusEffect(string effectID)
    {
        if (runtimeStatusEffects == null) return false;
        return runtimeStatusEffects.HasEffect(effectID);
    }
    
    #endregion
    
    #region 调试接口
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (runtimeAttributes == null) return "未初始化";
        
        return $"血量: {CurrentHealth:F1}/{MaxHealth:F1} ({HealthRatio * 100:F0}%)";
    }
    
    #endregion
}

