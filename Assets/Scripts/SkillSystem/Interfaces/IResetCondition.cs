using UnityEngine;

/// <summary>
/// 重置条件接口 - 定义何时重置技能触发条件
/// 设计理念：类似条件系统，支持复合重置逻辑
/// </summary>
public interface IResetCondition
{
    /// <summary>
    /// 重置条件名称
    /// </summary>
    string ConditionName { get; }
    
    /// <summary>
    /// 初始化重置条件
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 检查是否应该重置条件
    /// </summary>
    /// <param name="args">技能参数（包含事件数据）</param>
    /// <returns>是否应该重置条件</returns>
    bool ShouldReset(SkillArgs args);
    
    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    void Reset();
    
    /// <summary>
    /// 设置目标技能实例ID（用于事件过滤）
    /// </summary>
    /// <param name="skillInstanceId">技能实例ID</param>
    void SetTargetSkillInstanceId(string skillInstanceId);
}
