using UnityEngine;

/// <summary>
/// 修改器句柄 - 管理单个修改器的生命周期
/// 
/// 【设计理念】：
/// - 分离修改器数据（Modifier）和生命周期管理
/// - Modifier 是纯数据（struct）
/// - ModifierHandle 管理时间、来源、移除条件等
/// - 符合关注点分离原则
/// 
/// 【职责】：
/// - 存储修改器引用
/// - 管理持续时间
/// - 管理移除条件
/// - 追踪来源信息（用于调试）
/// </summary>
public class ModifierHandle
{
    #region 核心数据
    
    /// <summary>
    /// 修改器数据（纯数据）
    /// </summary>
    public Modifier Modifier { get; private set; }
    
    /// <summary>
    /// 修改器来源（用于调试和批量移除）
    /// </summary>
    public object Source { get; private set; }
    
    #endregion
    
    #region 时间管理
    
    /// <summary>
    /// 持续时间（0 表示永久）
    /// </summary>
    public float Duration { get; private set; }
    
    /// <summary>
    /// 剩余时间
    /// </summary>
    public float TimeRemaining { get; private set; }
    
    /// <summary>
    /// 是否是临时效果
    /// </summary>
    public bool IsTemporary => Duration > 0f;
    
    #endregion
    
    #region 移除条件
    
    /// <summary>
    /// 效果移除条件（可选）
    /// </summary>
    public IEffectRemovalCondition RemovalCondition { get; private set; }
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建永久修改器句柄
    /// </summary>
    public ModifierHandle(Modifier modifier, object source = null)
    {
        this.Modifier = modifier;
        this.Source = source;
        this.Duration = 0f;
        this.TimeRemaining = 0f;
        this.RemovalCondition = null;
    }
    
    /// <summary>
    /// 创建临时修改器句柄
    /// </summary>
    public ModifierHandle(Modifier modifier, float duration, object source = null)
    {
        this.Modifier = modifier;
        this.Source = source;
        this.Duration = duration;
        this.TimeRemaining = duration;
        this.RemovalCondition = null;
    }
    
    /// <summary>
    /// 创建带移除条件的修改器句柄
    /// </summary>
    public ModifierHandle(Modifier modifier, IEffectRemovalCondition removalCondition, object source = null)
    {
        this.Modifier = modifier;
        this.Source = source;
        this.Duration = 0f;
        this.TimeRemaining = 0f;
        this.RemovalCondition = removalCondition;
    }
    
    #endregion
    
    #region 生命周期管理
    
    /// <summary>
    /// 更新剩余时间
    /// </summary>
    public void UpdateTime(float deltaTime)
    {
        if (IsTemporary)
        {
            TimeRemaining -= deltaTime;
            TimeRemaining = Mathf.Max(0f, TimeRemaining);
        }
    }
    
    /// <summary>
    /// 检查是否时间到期
    /// </summary>
    public bool IsTimeExpired()
    {
        return IsTemporary && TimeRemaining <= 0f;
    }
    
    /// <summary>
    /// 检查是否应该被移除（基于条件）
    /// </summary>
    public bool ShouldBeRemoved(SkillArgs args)
    {
        // 时间到期检查
        if (IsTimeExpired())
        {
            return true;
        }
        
        // 基于条件的移除检查
        if (RemovalCondition != null)
        {
            return RemovalCondition.ShouldRemoveEffect(args);
        }
        
        return false;
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = Modifier.GetDebugInfo();
        
        if (Source != null)
        {
            info += $" [来源: {Source.GetType().Name}]";
        }
        
        if (IsTemporary)
        {
            info += $" [时间: {TimeRemaining:F1}s/{Duration:F1}s]";
        }
        
        if (RemovalCondition != null)
        {
            info += $" [条件: {RemovalCondition.ConditionName}]";
        }
        
        return info;
    }
    
    #endregion
}

