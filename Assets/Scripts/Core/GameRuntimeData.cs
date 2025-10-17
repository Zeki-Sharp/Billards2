using UnityEngine;

/// <summary>
/// 游戏运行时数据管理器 - 静态类
/// 负责管理跨场景的临时属性数据，替代 PlayerStateManager 的功能
/// 
/// 【核心职责】：
/// - 保存当前血量、最大血量、伤害值、攻击范围等临时属性
/// - 提供跨场景数据保留功能
/// - 作为各系统获取最终属性值的统一数据源
/// 
/// 【设计原则】：
/// - 只保存最终数值结果，不保存修饰器中间状态
/// - 保持 SO 配置只读，所有临时修改都通过此静态类管理
/// - 简化跨场景数据流，避免复杂的序列化逻辑
/// </summary>
public static class GameRuntimeData
{
    #region 私有字段
    
    // 4个临时属性，-1f 表示未设置
    private static float currentHealth = -1f;        // 当前生命值
    private static float maxHealth = -1f;            // 最大生命值
    private static float damage = -1f;               // 伤害值（碰撞/范围）
    private static float attackRange = -1f;         // 攻击范围（仅范围攻击角色）
    
    // 初始化标志
    private static bool hasInitialized = false;
    
    // 调试设置
    private static bool enableDebugLog = true;
    
    #endregion
    
    #region 初始化
    
    /// <summary>
    /// 初始化游戏运行时数据
    /// </summary>
    public static void Initialize()
    {
        if (!hasInitialized)
        {
            currentHealth = -1f;
            maxHealth = -1f;
            damage = -1f;
            attackRange = -1f;
            hasInitialized = true;
            
            if (enableDebugLog)
            {
                Debug.Log("[GameRuntimeData] 初始化完成");
            }
        }
    }
    
    #endregion
    
    #region 当前血量管理
    
    /// <summary>
    /// 设置当前血量
    /// </summary>
    /// <param name="health">血量值</param>
    public static void SetCurrentHealth(float health)
    {
        currentHealth = health;
        
        if (enableDebugLog)
        {
            Debug.Log($"[GameRuntimeData] 设置当前血量: {health}");
        }
    }
    
    /// <summary>
    /// 获取当前血量
    /// </summary>
    /// <returns>当前血量值</returns>
    public static float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// 检查是否有当前血量数据
    /// </summary>
    /// <returns>是否有数据</returns>
    public static bool HasCurrentHealthData()
    {
        return currentHealth >= 0f;
    }
    
    #endregion
    
    #region 最大血量管理
    
    /// <summary>
    /// 设置最大血量
    /// </summary>
    /// <param name="health">最大血量值</param>
    public static void SetMaxHealth(float health)
    {
        maxHealth = health;
        
        if (enableDebugLog)
        {
            Debug.Log($"[GameRuntimeData] 设置最大血量: {health}");
        }
    }
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    /// <returns>最大血量值</returns>
    public static float GetMaxHealth()
    {
        return maxHealth;
    }
    
    /// <summary>
    /// 检查是否有最大血量数据
    /// </summary>
    /// <returns>是否有数据</returns>
    public static bool HasMaxHealthData()
    {
        return maxHealth >= 0f;
    }
    
    #endregion
    
    #region 伤害值管理
    
    /// <summary>
    /// 设置伤害值
    /// </summary>
    /// <param name="damageValue">伤害值</param>
    public static void SetDamage(float damageValue)
    {
        damage = damageValue;
        
        if (enableDebugLog)
        {
            Debug.Log($"[GameRuntimeData] 设置伤害值: {damageValue}");
        }
    }
    
    /// <summary>
    /// 获取伤害值
    /// </summary>
    /// <returns>伤害值</returns>
    public static float GetDamage()
    {
        return damage;
    }
    
    /// <summary>
    /// 检查是否有伤害值数据
    /// </summary>
    /// <returns>是否有数据</returns>
    public static bool HasDamageData()
    {
        return damage >= 0f;
    }
    
    #endregion
    
    #region 攻击范围管理
    
    /// <summary>
    /// 设置攻击范围
    /// </summary>
    /// <param name="range">攻击范围值</param>
    public static void SetAttackRange(float range)
    {
        attackRange = range;
        
        if (enableDebugLog)
        {
            Debug.Log($"[GameRuntimeData] 设置攻击范围: {range}");
        }
    }
    
    /// <summary>
    /// 获取攻击范围
    /// </summary>
    /// <returns>攻击范围值</returns>
    public static float GetAttackRange()
    {
        return attackRange;
    }
    
    /// <summary>
    /// 检查是否有攻击范围数据
    /// </summary>
    /// <returns>是否有数据</returns>
    public static bool HasAttackRangeData()
    {
        return attackRange >= 0f;
    }
    
    #endregion
    
    #region 数据管理
    
    /// <summary>
    /// 清理所有数据（游戏重新开始时调用）
    /// </summary>
    public static void ClearAllData()
    {
        currentHealth = -1f;
        maxHealth = -1f;
        damage = -1f;
        attackRange = -1f;
        hasInitialized = false;
        
        if (enableDebugLog)
        {
            Debug.Log("[GameRuntimeData] 所有数据已清理");
        }
    }
    
    /// <summary>
    /// 设置调试日志开关
    /// </summary>
    /// <param name="enabled">是否启用调试日志</param>
    public static void SetDebugLogEnabled(bool enabled)
    {
        enableDebugLog = enabled;
    }
    
    #endregion
    
    #region 调试信息
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public static string GetDebugInfo()
    {
        return $"GameRuntimeData 调试信息:\n" +
               $"- 当前生命值: {(HasCurrentHealthData() ? currentHealth.ToString() : "未设置")}\n" +
               $"- 最大生命值: {(HasMaxHealthData() ? maxHealth.ToString() : "未设置")}\n" +
               $"- 伤害值: {(HasDamageData() ? damage.ToString() : "未设置")}\n" +
               $"- 攻击范围: {(HasAttackRangeData() ? attackRange.ToString() : "未设置")}\n" +
               $"- 已初始化: {hasInitialized}";
    }
    
    /// <summary>
    /// 打印调试信息到控制台
    /// </summary>
    public static void PrintDebugInfo()
    {
        Debug.Log(GetDebugInfo());
    }
    
    #endregion
}
