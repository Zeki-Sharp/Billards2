using UnityEngine;

/// <summary>
/// 立即重置条件
/// 技能执行后立即重置触发条件，让技能可以再次被触发
/// </summary>
public class ImmediateResetCondition : IResetCondition
{
    public string ConditionName => "ImmediateResetCondition";
    
    /// <summary>
    /// 目标技能实例ID（用于事件过滤）
    /// </summary>
    private string targetSkillInstanceId;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImmediateResetCondition()
    {
    }

    /// <summary>
    /// 设置目标技能实例ID
    /// </summary>
    /// <param name="skillInstanceId">技能实例ID</param>
    public void SetTargetSkillInstanceId(string skillInstanceId)
    {
        this.targetSkillInstanceId = skillInstanceId;
        Debug.Log($"[{ConditionName}] 设置目标技能实例ID: {skillInstanceId}");
    }

    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        Debug.Log($"[{ConditionName}] 初始化完成 - 立即重置触发条件");
    }

    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>只响应对应技能实例的技能执行完毕事件</returns>
    public bool ShouldReset(object eventData)
    {
        // 只响应技能执行完毕事件
        if (eventData is SkillExecutedEventData skillEvent)
        {
            // 只响应对应技能实例的事件
            if (skillEvent.SkillInstanceId == targetSkillInstanceId)
            {
                Debug.Log($"[{ConditionName}] 响应技能 {skillEvent.SkillName} 执行完毕事件，立即重置");
                return true;
            }
            else
            {
                Debug.Log($"[{ConditionName}] 忽略其他技能的事件 - 目标: {targetSkillInstanceId}, 事件来源: {skillEvent.SkillInstanceId}");
                return false;
            }
        }
        
        // 其他事件类型不响应
        Debug.Log($"[{ConditionName}] 忽略非技能执行完毕事件: {eventData?.GetType().Name}");
        return false;
    }

    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        Debug.Log($"[{ConditionName}] 状态重置 - 立即重置条件无需特殊重置");
    }
}
