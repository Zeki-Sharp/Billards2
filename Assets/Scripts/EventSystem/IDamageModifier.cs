using UnityEngine;

/// <summary>
/// 伤害修改器接口
/// 用于在 DamageProcessor 中按优先级处理伤害
/// </summary>
public interface IDamageModifier
{
    /// <summary>
    /// 修改器优先级
    /// </summary>
    EventPriority Priority { get; }
    
    /// <summary>
    /// 修改器名称（用于调试和日志）
    /// </summary>
    string ModifierName { get; }
    
    /// <summary>
    /// 处理伤害修改
    /// </summary>
    /// <param name="attackData">攻击数据（可修改）</param>
    /// <returns>是否成功处理了伤害修改</returns>
    bool ProcessDamage(ref AttackData attackData);
    
    /// <summary>
    /// 是否启用此修改器
    /// </summary>
    bool IsEnabled { get; }
}
