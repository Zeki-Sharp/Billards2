using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 运行时属性管理器 - 使用轻量级 Modifier 系统
/// 
/// 【设计理念】：
/// - 使用新的轻量级 Modifier/ModifierList/Modifiers 系统
/// - 管理多个 RuntimeStat
/// - 提供统一的属性访问和修改接口
/// - 高性能，低 GC 压力
/// 
/// 【与旧系统的区别】：
/// - Modifier 是 struct（旧系统是 class）
/// - 使用缓存总值，O(1) 访问（旧系统是 O(n) 计算）
/// - 生命周期管理分离（ModifierHandle）
/// - 更清晰的职责划分
/// </summary>
public class RuntimeStatsManager
{
    #region 私有字段
    
    /// <summary>
    /// 所有运行时属性
    /// </summary>
    private Dictionary<string, RuntimeStat> stats = new Dictionary<string, RuntimeStat>();
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    private bool enableDebugLog = false;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时属性管理器
    /// </summary>
    /// <param name="enableDebugLog">是否启用调试日志</param>
    public RuntimeStatsManager(bool enableDebugLog = false)
    {
        this.enableDebugLog = enableDebugLog;
    }
    
    #endregion
    
    #region 属性注册
    
    /// <summary>
    /// 注册属性
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="baseValue">基础值</param>
    public void RegisterStat(string statID, float baseValue)
    {
        if (stats.ContainsKey(statID))
        {
            Debug.LogWarning($"[RuntimeStatsManager] 属性 {statID} 已存在，将覆盖");
        }
        
        stats[statID] = new RuntimeStat(statID, baseValue);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 注册属性: {statID} = {baseValue}");
        }
    }
    
    /// <summary>
    /// 批量注册属性
    /// </summary>
    public void RegisterStats(Dictionary<string, float> baseStats)
    {
        foreach (var kvp in baseStats)
        {
            RegisterStat(kvp.Key, kvp.Value);
        }
    }
    
    #endregion
    
    #region 属性访问
    
    /// <summary>
    /// 获取属性最终值
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <returns>最终值，不存在返回 0</returns>
    public float GetStatValue(string statID)
    {
        if (stats.TryGetValue(statID, out var stat))
        {
            return stat.Value;
        }
        
        Debug.LogWarning($"[RuntimeStatsManager] 属性 {statID} 不存在");
        return 0f;
    }
    
    /// <summary>
    /// 获取属性基础值
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <returns>基础值，不存在返回 0</returns>
    public float GetBaseValue(string statID)
    {
        if (stats.TryGetValue(statID, out var stat))
        {
            return stat.BaseValue;
        }
        
        Debug.LogWarning($"[RuntimeStatsManager] 属性 {statID} 不存在");
        return 0f;
    }
    
    /// <summary>
    /// 设置属性基础值
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="baseValue">新的基础值</param>
    public void SetBaseValue(string statID, float baseValue)
    {
        if (stats.TryGetValue(statID, out var stat))
        {
            stat.SetBaseValue(baseValue);
            
            if (enableDebugLog)
            {
                Debug.Log($"[RuntimeStatsManager] 设置 {statID} 基础值: {baseValue}");
            }
        }
        else
        {
            // 如果属性不存在，自动注册
            RegisterStat(statID, baseValue);
        }
    }
    
    #endregion
    
    #region 修改器管理
    
    /// <summary>
    /// 添加固定值修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddConstantModifier(string statID, float value, object source = null)
    {
        var modifier = new Modifier(statID, value);
        
        if (!stats.TryGetValue(statID, out var stat))
        {
            Debug.LogError($"[RuntimeStatsManager] 属性 {statID} 不存在，无法添加修改器");
            return null;
        }
        
        var handle = stat.AddModifier(modifier, isPercent: false, source);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 添加固定值修改器: {modifier.GetDebugInfo()}");
        }
        
        return handle;
    }
    
    /// <summary>
    /// 添加百分比修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值（例如 0.5 表示 +50%）</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddPercentModifier(string statID, float value, object source = null)
    {
        var modifier = new Modifier(statID, value);
        
        if (!stats.TryGetValue(statID, out var stat))
        {
            Debug.LogError($"[RuntimeStatsManager] 属性 {statID} 不存在，无法添加修改器");
            return null;
        }
        
        var handle = stat.AddModifier(modifier, isPercent: true, source);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 添加百分比修改器: {modifier.GetDebugInfo()}");
        }
        
        return handle;
    }
    
    /// <summary>
    /// 添加临时修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值</param>
    /// <param name="isPercent">是否为百分比</param>
    /// <param name="duration">持续时间</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddTemporaryModifier(string statID, float value, bool isPercent, float duration, object source = null)
    {
        var modifier = new Modifier(statID, value);
        
        if (!stats.TryGetValue(statID, out var stat))
        {
            Debug.LogError($"[RuntimeStatsManager] 属性 {statID} 不存在，无法添加修改器");
            return null;
        }
        
        var handle = stat.AddTemporaryModifier(modifier, isPercent, duration, source);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 添加临时修改器: {modifier.GetDebugInfo()}, 持续 {duration}s");
        }
        
        return handle;
    }
    
    /// <summary>
    /// 添加带移除条件的修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值</param>
    /// <param name="isPercent">是否为百分比</param>
    /// <param name="removalCondition">移除条件</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddConditionalModifier(string statID, float value, bool isPercent, IEffectRemovalCondition removalCondition, object source = null)
    {
        var modifier = new Modifier(statID, value);
        
        if (!stats.TryGetValue(statID, out var stat))
        {
            Debug.LogError($"[RuntimeStatsManager] 属性 {statID} 不存在，无法添加修改器");
            return null;
        }
        
        var handle = stat.AddConditionalModifier(modifier, isPercent, removalCondition, source);
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 添加条件修改器: {modifier.GetDebugInfo()}, 条件: {removalCondition.ConditionName}");
        }
        
        return handle;
    }
    
    /// <summary>
    /// 移除修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="handle">修改器句柄</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveModifier(string statID, ModifierHandle handle)
    {
        if (!stats.TryGetValue(statID, out var stat))
        {
            Debug.LogWarning($"[RuntimeStatsManager] 属性 {statID} 不存在");
            return false;
        }
        
        bool removed = stat.RemoveModifier(handle);
        
        if (removed && enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 移除修改器: {handle.GetDebugInfo()}");
        }
        
        return removed;
    }
    
    /// <summary>
    /// 移除指定来源的所有修改器
    /// </summary>
    /// <param name="source">来源</param>
    /// <returns>移除的修改器总数</returns>
    public int RemoveModifiersBySource(object source)
    {
        int totalRemoved = 0;
        
        foreach (var stat in stats.Values)
        {
            totalRemoved += stat.RemoveModifiersBySource(source);
        }
        
        if (totalRemoved > 0 && enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 移除来源 {source?.GetType().Name} 的 {totalRemoved} 个修改器");
        }
        
        return totalRemoved;
    }
    
    #endregion
    
    #region 生命周期更新
    
    /// <summary>
    /// 更新所有临时修改器（通常在 Update 中调用）
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    public void UpdateModifiers(float deltaTime)
    {
        foreach (var stat in stats.Values)
        {
            var expiredHandles = stat.UpdateTime(deltaTime);
            
            // 移除过期的修改器
            foreach (var handle in expiredHandles)
            {
                stat.RemoveModifier(handle);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[RuntimeStatsManager] 修改器已过期并移除: {handle.GetDebugInfo()}");
                }
            }
        }
    }
    
    /// <summary>
    /// 检查基于事件的修改器移除
    /// </summary>
    /// <param name="args">技能参数</param>
    public void CheckEventBasedRemoval(SkillArgs args)
    {
        foreach (var stat in stats.Values)
        {
            var handlesToRemove = stat.CheckEventBasedRemoval(args);
            
            // 移除满足条件的修改器
            foreach (var handle in handlesToRemove)
            {
                stat.RemoveModifier(handle);
                
                if (enableDebugLog)
                {
                    Debug.Log($"[RuntimeStatsManager] 条件满足，移除修改器: {handle.GetDebugInfo()}");
                }
            }
        }
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 设置调试日志
    /// </summary>
    public void SetDebugLog(bool enable)
    {
        enableDebugLog = enable;
    }
    
    /// <summary>
    /// 获取所有属性的调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (stats.Count == 0)
        {
            return "运行时属性管理器：无注册属性";
        }
        
        string info = $"运行时属性管理器：共 {stats.Count} 个属性\n\n";
        
        foreach (var stat in stats.Values)
        {
            info += stat.GetDebugInfo() + "\n";
        }
        
        return info;
    }
    
    #endregion
    
    #region 序列化接口（跨场景持久化）
    
    /// <summary>
    /// 导出激活的修改器快照（用于跨场景保存）
    /// 注意：当前简化实现，仅导出基础信息，不包含复杂的源对象引用
    /// </summary>
    public List<ModifierSnapshot> ExportModifiers()
    {
        var snapshots = new List<ModifierSnapshot>();
        
        // 注意：当前版本暂不完全序列化修改器
        // 修改器通常由技能系统在场景加载时重新应用
        // 这里保留接口供未来扩展
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 📤 导出修改器快照（当前版本：简化实现）");
        }
        
        return snapshots;
    }
    
    /// <summary>
    /// 恢复修改器（用于跨场景恢复）
    /// 注意：当前简化实现
    /// </summary>
    public void RestoreModifiers(List<ModifierSnapshot> snapshots)
    {
        if (snapshots == null || snapshots.Count == 0) return;
        
        // 注意：当前版本暂不完全恢复修改器
        // 修改器通常由技能系统在场景加载时重新应用
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatsManager] 📥 恢复修改器快照（当前版本：简化实现）");
        }
    }
    
    /// <summary>
    /// 导出基础属性值（用于调试）
    /// </summary>
    public Dictionary<string, float> ExportBaseValues()
    {
        var snapshot = new Dictionary<string, float>();
        
        foreach (var kvp in stats)
        {
            snapshot[kvp.Key] = kvp.Value.BaseValue;
        }
        
        return snapshot;
    }
    
    #endregion
}

