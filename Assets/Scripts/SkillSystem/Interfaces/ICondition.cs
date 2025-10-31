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
    /// <param name="args">技能参数（包含事件数据）</param>
    /// <returns>条件是否满足</returns>
    bool CheckCondition(SkillArgs args);
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    void Reset();
    
    /// <summary>
    /// 回合结束时重置条件状态（可选实现）
    /// 如果不支持回合重置，可以留空或调用Reset()
    /// </summary>
    void ResetOnPhaseEnd();
}
