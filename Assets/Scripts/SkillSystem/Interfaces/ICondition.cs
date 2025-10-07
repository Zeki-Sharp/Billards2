using UnityEngine;

/// <summary>
/// 条件接口 - 技能系统的第一阶段最小验证
/// 职责：判断是否满足技能触发条件
/// 基于触发器检测到的事件进行条件判断，决定技能是否应该被触发
/// </summary>
public interface ICondition
{
    /// <summary>
    /// 条件名称
    /// </summary>
    string ConditionName { get; }
    
    /// <summary>
    /// 初始化条件
    /// </summary>
    void Initialize();
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>条件是否满足</returns>
    bool CheckCondition(object eventData);
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    void Reset();
}
