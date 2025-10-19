using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 复合重置条件 - 支持多个重置条件的逻辑组合
/// 支持 AND 和 OR 逻辑
/// </summary>
public class CompositeResetCondition : IResetCondition
{
    public string ConditionName => "CompositeResetCondition";
    
    private List<IResetCondition> resetConditions = new List<IResetCondition>();
    private ResetLogicType logicType = ResetLogicType.Or;
    
    /// <summary>
    /// 设置逻辑类型
    /// </summary>
    /// <param name="logicType">逻辑类型</param>
    public void SetLogicType(ResetLogicType logicType)
    {
        this.logicType = logicType;
        Debug.Log($"[{ConditionName}] 设置逻辑类型: {logicType}");
    }
    
    /// <summary>
    /// 添加重置条件
    /// </summary>
    /// <param name="resetCondition">要添加的重置条件</param>
    public void AddResetCondition(IResetCondition resetCondition)
    {
        if (resetCondition != null)
        {
            resetConditions.Add(resetCondition);
            Debug.Log($"[{ConditionName}] 添加重置条件: {resetCondition.ConditionName}");
        }
    }
    
    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        foreach (var resetCondition in resetConditions)
        {
            resetCondition?.Initialize();
        }
        Debug.Log($"[{ConditionName}] 初始化完成，包含 {resetConditions.Count} 个重置条件，逻辑类型: {logicType}");
    }
    
    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>根据逻辑类型判断是否应该重置</returns>
    public bool ShouldReset(object eventData)
    {
        if (resetConditions.Count == 0)
        {
            Debug.LogWarning($"[{ConditionName}] 没有重置条件，默认返回false");
            return false;
        }
        
        bool result;
        
        if (logicType == ResetLogicType.And)
        {
            // AND 逻辑：所有重置条件都必须满足
            result = resetConditions.All(resetCondition => resetCondition.ShouldReset(eventData));
            Debug.Log($"[{ConditionName}] AND 逻辑检查: {result} (需要所有 {resetConditions.Count} 个重置条件都满足)");
        }
        else
        {
            // OR 逻辑：任一重置条件满足即可
            result = resetConditions.Any(resetCondition => resetCondition.ShouldReset(eventData));
            Debug.Log($"[{ConditionName}] OR 逻辑检查: {result} (需要任一 {resetConditions.Count} 个重置条件满足)");
        }
        
        return result;
    }
    
    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        foreach (var resetCondition in resetConditions)
        {
            resetCondition?.Reset();
        }
        Debug.Log($"[{ConditionName}] 重置所有 {resetConditions.Count} 个重置条件");
    }
    
    /// <summary>
    /// 设置目标技能实例ID（用于事件过滤）
    /// </summary>
    /// <param name="skillInstanceId">技能实例ID</param>
    public void SetTargetSkillInstanceId(string skillInstanceId)
    {
        // 为所有子重置条件设置目标技能实例ID
        foreach (var resetCondition in resetConditions)
        {
            resetCondition?.SetTargetSkillInstanceId(skillInstanceId);
        }
        Debug.Log($"[{ConditionName}] 为所有 {resetConditions.Count} 个子重置条件设置目标技能实例ID: {skillInstanceId}");
    }
}
