using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 运行时属性 - 管理单个属性的完整状态
/// 
/// 【设计理念】：
/// - 组合 Modifiers（数据层）和 ModifierHandle 列表（生命周期层）
/// - 分离纯数据修改和生命周期管理
/// - 提供统一的属性访问接口
/// 
/// 【职责】：
/// - 管理单个属性的基础值
/// - 管理所有修改器句柄
/// - 计算最终值
/// - 处理修改器生命周期
/// </summary>
public class RuntimeStat
{
    #region 核心数据
    
    /// <summary>
    /// 属性ID
    /// </summary>
    public string StatID { get; private set; }
    
    /// <summary>
    /// 基础值
    /// </summary>
    public float BaseValue { get; private set; }
    
    /// <summary>
    /// 修改器容器（纯数据层）
    /// </summary>
    private Modifiers modifiers;
    
    /// <summary>
    /// 修改器句柄列表（生命周期层）
    /// </summary>
    private List<ModifierHandle> handles = new List<ModifierHandle>();
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时属性
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="baseValue">基础值</param>
    public RuntimeStat(string statID, float baseValue)
    {
        this.StatID = statID;
        this.BaseValue = baseValue;
        this.modifiers = new Modifiers(statID);
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 最终值（带修改器）
    /// </summary>
    public float Value => modifiers.CalculateFinalValue(BaseValue);
    
    /// <summary>
    /// 修改器数量
    /// </summary>
    public int ModifierCount => handles.Count;
    
    #endregion
    
    #region 基础值管理
    
    /// <summary>
    /// 设置基础值
    /// </summary>
    public void SetBaseValue(float value)
    {
        BaseValue = value;
    }
    
    #endregion
    
    #region 修改器管理
    
    /// <summary>
    /// 添加永久修改器
    /// </summary>
    /// <param name="modifier">修改器数据</param>
    /// <param name="isPercent">是否为百分比修改器</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddModifier(Modifier modifier, bool isPercent, object source = null)
    {
        var handle = new ModifierHandle(modifier, source);
        handles.Add(handle);
        
        // 添加到对应的列表
        if (isPercent)
        {
            modifiers.AddPercent(modifier);
        }
        else
        {
            modifiers.AddConstant(modifier);
        }
        
        return handle;
    }
    
    /// <summary>
    /// 添加临时修改器
    /// </summary>
    /// <param name="modifier">修改器数据</param>
    /// <param name="isPercent">是否为百分比修改器</param>
    /// <param name="duration">持续时间</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddTemporaryModifier(Modifier modifier, bool isPercent, float duration, object source = null)
    {
        var handle = new ModifierHandle(modifier, duration, source);
        handles.Add(handle);
        
        // 添加到对应的列表
        if (isPercent)
        {
            modifiers.AddPercent(modifier);
        }
        else
        {
            modifiers.AddConstant(modifier);
        }
        
        return handle;
    }
    
    /// <summary>
    /// 添加带移除条件的修改器
    /// </summary>
    /// <param name="modifier">修改器数据</param>
    /// <param name="isPercent">是否为百分比修改器</param>
    /// <param name="removalCondition">移除条件</param>
    /// <param name="source">来源（可选）</param>
    /// <returns>修改器句柄</returns>
    public ModifierHandle AddConditionalModifier(Modifier modifier, bool isPercent, IEffectRemovalCondition removalCondition, object source = null)
    {
        var handle = new ModifierHandle(modifier, removalCondition, source);
        handles.Add(handle);
        
        // 添加到对应的列表
        if (isPercent)
        {
            modifiers.AddPercent(modifier);
        }
        else
        {
            modifiers.AddConstant(modifier);
        }
        
        return handle;
    }
    
    /// <summary>
    /// 移除修改器
    /// </summary>
    /// <param name="handle">修改器句柄</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveModifier(ModifierHandle handle)
    {
        if (handle == null) return false;
        
        if (!handles.Remove(handle)) return false;
        
        // 从对应的列表移除（尝试两个列表）
        bool removed = modifiers.RemoveConstant(handle.Modifier);
        if (!removed)
        {
            removed = modifiers.RemovePercent(handle.Modifier);
        }
        
        return removed;
    }
    
    /// <summary>
    /// 移除指定来源的所有修改器
    /// </summary>
    /// <param name="source">来源</param>
    /// <returns>移除的修改器数量</returns>
    public int RemoveModifiersBySource(object source)
    {
        var handlesToRemove = handles.Where(h => h.Source == source).ToList();
        
        foreach (var handle in handlesToRemove)
        {
            RemoveModifier(handle);
        }
        
        return handlesToRemove.Count;
    }
    
    /// <summary>
    /// 清空所有修改器
    /// </summary>
    public void Clear()
    {
        handles.Clear();
        modifiers.Clear();
    }
    
    #endregion
    
    #region 生命周期更新
    
    /// <summary>
    /// 更新所有临时修改器的时间
    /// </summary>
    /// <param name="deltaTime">时间增量</param>
    /// <returns>过期的修改器句柄列表</returns>
    public List<ModifierHandle> UpdateTime(float deltaTime)
    {
        var expiredHandles = new List<ModifierHandle>();
        
        foreach (var handle in handles)
        {
            handle.UpdateTime(deltaTime);
            
            if (handle.IsTimeExpired())
            {
                expiredHandles.Add(handle);
            }
        }
        
        return expiredHandles;
    }
    
    /// <summary>
    /// 检查基于事件的移除条件
    /// </summary>
    /// <param name="args">技能参数</param>
    /// <returns>需要移除的修改器句柄列表</returns>
    public List<ModifierHandle> CheckEventBasedRemoval(SkillArgs args)
    {
        var handlesToRemove = new List<ModifierHandle>();
        
        foreach (var handle in handles)
        {
            if (handle.ShouldBeRemoved(args))
            {
                handlesToRemove.Add(handle);
            }
        }
        
        return handlesToRemove;
    }
    
    #endregion
    
    #region 查询方法
    
    /// <summary>
    /// 获取所有修改器句柄（只读）
    /// </summary>
    public IReadOnlyList<ModifierHandle> GetAllHandles()
    {
        return handles.AsReadOnly();
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"[{StatID}]\n";
        info += $"  基础值: {BaseValue:F2}\n";
        info += $"  最终值: {Value:F2}\n";
        info += $"  修改器数量: {ModifierCount}\n";
        
        if (ModifierCount > 0)
        {
            info += modifiers.GetDebugInfo();
        }
        
        return info;
    }
    
    #endregion
}

