using UnityEngine;

/// <summary>
/// 效果接口 - 技能系统的第一阶段最小验证
/// 职责：执行技能的具体效果
/// 专注于游戏机制和数值变化，表现效果通过现有事件系统处理
/// </summary>
public interface IEffect
{
    /// <summary>
    /// 效果名称
    /// </summary>
    string EffectName { get; }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 执行效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>效果是否执行成功</returns>
    bool ExecuteEffect(object eventData);
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    void Reset();
    
    /// <summary>
    /// 是否允许执行（完全由重置条件控制）
    /// </summary>
    bool CanExecute { get; }
    
    /// <summary>
    /// 设置是否允许执行（完全由重置条件控制）
    /// </summary>
    /// <param name="canExecute">是否允许执行</param>
    void SetCanExecute(bool canExecute);
}
