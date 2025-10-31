using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 运行时状态效果管理器 - 管理所有激活的状态效果
/// 
/// 【设计理念】：
/// - 管理多个 RuntimeStatusEffect
/// - 处理堆叠、刷新、过期
/// - 提供统一的添加/移除接口
/// - 每帧更新所有效果
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 RuntimeStatusEffects
/// 
/// 【典型应用】：
/// - 管理玩家身上的所有 Buff/Debuff
/// - 管理敌人身上的所有状态
/// </summary>
public class RuntimeStatusEffects
{
    #region 私有字段
    
    /// <summary>
    /// 所有激活的状态效果
    /// </summary>
    private List<RuntimeStatusEffect> activeEffects = new List<RuntimeStatusEffect>();
    
    /// <summary>
    /// 是否启用调试日志
    /// </summary>
    private bool enableDebugLog = false;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时状态效果管理器
    /// </summary>
    public RuntimeStatusEffects(bool enableDebugLog = false)
    {
        this.enableDebugLog = enableDebugLog;
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 激活的效果数量
    /// </summary>
    public int ActiveCount => activeEffects.Count;
    
    #endregion
    
    #region 添加/移除效果
    
    /// <summary>
    /// 添加状态效果
    /// </summary>
    /// <param name="effectData">效果配置</param>
    /// <param name="source">效果来源</param>
    /// <returns>运行时效果实例</returns>
    public RuntimeStatusEffect AddEffect(StatusEffectData effectData, object source = null)
    {
        if (effectData == null || !effectData.IsValid())
        {
            Debug.LogError("[RuntimeStatusEffects] 状态效果配置无效");
            return null;
        }
        
        // 检查是否已存在相同效果
        var existingEffect = FindEffect(effectData.effectID);
        
        if (existingEffect != null)
        {
            // 如果可以堆叠，增加层数
            if (effectData.canStack)
            {
                if (existingEffect.AddStack())
                {
                    if (enableDebugLog)
                    {
                        Debug.Log($"[RuntimeStatusEffects] {effectData.displayName} 堆叠层数增加: {existingEffect.StackCount}");
                    }
                    return existingEffect;
                }
                else
                {
                    // 已达到最大堆叠，刷新时间
                    existingEffect.RefreshDuration();
                    if (enableDebugLog)
                    {
                        Debug.Log($"[RuntimeStatusEffects] {effectData.displayName} 已达最大堆叠，刷新持续时间");
                    }
                    return existingEffect;
                }
            }
            else
            {
                // 不可堆叠，刷新持续时间
                existingEffect.RefreshDuration();
                if (enableDebugLog)
                {
                    Debug.Log($"[RuntimeStatusEffects] {effectData.displayName} 刷新持续时间");
                }
                return existingEffect;
            }
        }
        
        // 创建新效果
        var newEffect = new RuntimeStatusEffect(effectData, source);
        activeEffects.Add(newEffect);
        
        // 触发开始回调
        newEffect.OnStart();
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatusEffects] 添加新状态效果: {effectData.displayName}");
        }
        
        return newEffect;
    }
    
    /// <summary>
    /// 移除状态效果
    /// </summary>
    public bool RemoveEffect(RuntimeStatusEffect effect)
    {
        if (effect == null) return false;
        
        if (activeEffects.Remove(effect))
        {
            // 触发结束回调
            effect.OnEnd();
            
            if (enableDebugLog)
            {
                Debug.Log($"[RuntimeStatusEffects] 移除状态效果: {effect.EffectID}");
            }
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 根据 ID 移除状态效果
    /// </summary>
    public bool RemoveEffectByID(string effectID)
    {
        var effect = FindEffect(effectID);
        return RemoveEffect(effect);
    }
    
    /// <summary>
    /// 移除所有状态效果
    /// </summary>
    public void ClearAll()
    {
        // 触发所有效果的结束回调
        foreach (var effect in activeEffects.ToList())
        {
            effect.OnEnd();
        }
        
        activeEffects.Clear();
        
        if (enableDebugLog)
        {
            Debug.Log("[RuntimeStatusEffects] 清空所有状态效果");
        }
    }
    
    #endregion
    
    #region 查询方法
    
    /// <summary>
    /// 查找指定ID的效果
    /// </summary>
    public RuntimeStatusEffect FindEffect(string effectID)
    {
        return activeEffects.FirstOrDefault(e => e.EffectID == effectID);
    }
    
    /// <summary>
    /// 检查是否有指定效果
    /// </summary>
    public bool HasEffect(string effectID)
    {
        return FindEffect(effectID) != null;
    }
    
    /// <summary>
    /// 获取所有激活的效果（只读）
    /// </summary>
    public IReadOnlyList<RuntimeStatusEffect> GetAllEffects()
    {
        return activeEffects.AsReadOnly();
    }
    
    #endregion
    
    #region 生命周期更新
    
    /// <summary>
    /// 更新所有状态效果（通常在 Update 中调用）
    /// </summary>
    public void UpdateEffects(float deltaTime)
    {
        var effectsToRemove = new List<RuntimeStatusEffect>();
        
        foreach (var effect in activeEffects)
        {
            // 更新时间
            effect.UpdateTime(deltaTime);
            
            // 调用激活期间回调
            effect.WhileActive(deltaTime);
            
            // 检查是否过期
            if (effect.IsExpired)
            {
                effectsToRemove.Add(effect);
            }
        }
        
        // 移除过期的效果
        foreach (var effect in effectsToRemove)
        {
            RemoveEffect(effect);
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
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (activeEffects.Count == 0)
        {
            return "RuntimeStatusEffects: 无激活效果";
        }
        
        string info = $"RuntimeStatusEffects: {activeEffects.Count} 个激活效果\n";
        foreach (var effect in activeEffects)
        {
            info += $"  {effect.GetDebugInfo()}\n";
        }
        return info;
    }
    
    #endregion
    
    #region 序列化接口（跨场景持久化）
    
    /// <summary>
    /// 导出激活的状态效果快照（用于跨场景保存）
    /// </summary>
    public List<StatusEffectSnapshot> ExportStatusEffects()
    {
        var snapshots = new List<StatusEffectSnapshot>();
        
        foreach (var effect in activeEffects)
        {
            var snapshot = new StatusEffectSnapshot
            {
                effectID = effect.Data.effectID,
                remainingDuration = effect.TimeRemaining,
                stackCount = effect.StackCount,
                sourceID = "" // 源对象ID，暂时留空
            };
            
            snapshots.Add(snapshot);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatusEffects] 📤 导出 {snapshots.Count} 个状态效果快照");
        }
        
        return snapshots;
    }
    
    /// <summary>
    /// 恢复状态效果（用于跨场景恢复）
    /// 注意：需要提供 StatusEffectData 的查找方法
    /// </summary>
    public void RestoreStatusEffects(List<StatusEffectSnapshot> snapshots, System.Func<string, StatusEffectData> dataLookup)
    {
        if (snapshots == null || snapshots.Count == 0) return;
        if (dataLookup == null)
        {
            Debug.LogError("[RuntimeStatusEffects] ❌ 恢复失败：缺少 StatusEffectData 查找方法");
            return;
        }
        
        int restoredCount = 0;
        
        foreach (var snapshot in snapshots)
        {
            StatusEffectData data = dataLookup(snapshot.effectID);
            
            if (data != null)
            {
                // 暂时简化处理，重新添加效果
                // TODO: 未来需要恢复剩余时间和堆叠数
                AddEffect(data, null);
                restoredCount++;
            }
            else if (enableDebugLog)
            {
                Debug.LogWarning($"[RuntimeStatusEffects] ⚠️ 无法找到效果数据: {snapshot.effectID}");
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[RuntimeStatusEffects] 📥 恢复了 {restoredCount}/{snapshots.Count} 个状态效果");
        }
    }
    
    #endregion
}

