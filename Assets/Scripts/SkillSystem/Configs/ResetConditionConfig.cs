using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 单个重置条件配置 - 用于配置一个具体的重置条件
/// </summary>
[System.Serializable]
public class SingleResetConditionConfig
{
    [LabelText("重置条件类型")]
    [Tooltip("选择重置条件类型")]
    public ResetConditionType resetType = ResetConditionType.Immediate;
    
    
    
    /// <summary>
    /// 创建单个重置条件实例
    /// </summary>
    public IResetCondition CreateResetCondition()
    {
        switch (resetType)
        {
            case ResetConditionType.Immediate:
                return new ImmediateResetCondition();
            case ResetConditionType.OnPlayerPhaseEnded:
                return new OnPhaseEndedResetCondition();
            case ResetConditionType.Never:
                return new NeverResetCondition();
            default:
                Debug.LogError($"不支持的重置条件类型: {resetType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (resetType)
        {
            case ResetConditionType.Immediate:
                return "立即重置";
            case ResetConditionType.OnPlayerPhaseEnded:
                return "回合结束重置";
            case ResetConditionType.Never:
                return "永不复位";
            default:
                return $"重置条件: {resetType}";
        }
    }
}

/// <summary>
/// 重置条件配置 - 支持多个重置条件和逻辑判断
/// 类似条件系统，支持复合重置逻辑
/// </summary>
[System.Serializable]
public class ResetConditionConfig
{
    [LabelText("重置逻辑")]
    [Tooltip("多个重置条件之间的逻辑关系（列表为空时使用默认重置逻辑）")]
    public ResetLogicType logicType = ResetLogicType.Or;
    
    [LabelText("重置条件列表")]
    [Tooltip("重置条件列表（列表为空时使用默认重置逻辑）")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "resetType")]
    public List<SingleResetConditionConfig> resetConditions = new List<SingleResetConditionConfig> { new SingleResetConditionConfig() };
    
    /// <summary>
    /// 创建重置条件实例
    /// </summary>
    public IResetCondition CreateResetCondition()
    {
        // 如果列表为空，返回默认的重置条件（立即重置）
        if (resetConditions.Count == 0)
        {
            return new ImmediateResetCondition();
        }
        
        // 如果只有一个重置条件，直接返回该条件
        if (resetConditions.Count == 1)
        {
            return resetConditions[0].CreateResetCondition();
        }
        
        // 如果有多个重置条件，创建复合重置条件
        if (resetConditions.Count > 1)
        {
            var compositeResetCondition = new CompositeResetCondition();
            compositeResetCondition.SetLogicType(logicType);
            
            foreach (var resetConditionConfig in resetConditions)
            {
                var resetCondition = resetConditionConfig.CreateResetCondition();
                if (resetCondition != null)
                {
                    compositeResetCondition.AddResetCondition(resetCondition);
                }
            }
            
            return compositeResetCondition;
        }
        
        // 默认返回立即重置条件
        return new ImmediateResetCondition();
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (resetConditions.Count == 0)
        {
            return "默认重置逻辑";
        }
        
        if (resetConditions.Count == 1)
        {
            return resetConditions[0].GetDebugInfo();
        }
        
        string info = $"复合重置条件 ({logicType}):\n";
        for (int i = 0; i < resetConditions.Count; i++)
        {
            info += $"  {i}: {resetConditions[i].GetDebugInfo()}\n";
        }
        return info;
    }
}

/// <summary>
/// 重置条件类型枚举
/// </summary>
public enum ResetConditionType
{
    Immediate,              // 立即重置
    OnPlayerPhaseEnded,     // 回合结束重置
    Never                   // 永不复位
}

/// <summary>
/// 重置逻辑类型枚举
/// </summary>
public enum ResetLogicType
{
    And,    // 所有条件都满足才重置
    Or      // 任一条件满足就重置
}
