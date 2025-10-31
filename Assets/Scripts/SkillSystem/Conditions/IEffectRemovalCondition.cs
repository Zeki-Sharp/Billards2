using UnityEngine;

/// <summary>
/// 效果移除条件接口 - 定义何时移除技能效果
/// 用于管理持续性效果的生命周期
/// </summary>
public interface IEffectRemovalCondition
{
    /// <summary>
    /// 条件名称
    /// </summary>
    string ConditionName { get; }
    
    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="args">技能参数（包含事件数据）</param>
    /// <returns>是否应该移除效果</returns>
    bool ShouldRemoveEffect(SkillArgs args);
    
    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    void Reset();
}
