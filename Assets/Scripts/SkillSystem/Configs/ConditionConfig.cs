using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 单个条件配置 - 用于配置一个具体的条件
/// </summary>
[System.Serializable]
public class SingleConditionConfig
{
    [Header("条件类型")]
    public ConditionType conditionType = ConditionType.Count;
    
    // 计数条件参数 - 只在 conditionType == Count 时显示
    [Header("计数条件参数")]
    [ShowIf("conditionType", ConditionType.Count)]
    [Tooltip("需要达到的计数")]
    public int requiredCount = 2;
    
    // 时间窗口条件参数 - 只在 conditionType == TimeWindow 时显示
    [Header("时间窗口条件参数")]
    [ShowIf("conditionType", ConditionType.TimeWindow)]
    [Tooltip("时间窗口长度（秒）")]
    public float timeWindow = 5f;
    
    // 血量条件参数 - 只在 conditionType == Health 时显示
    [Header("血量条件参数")]
    [ShowIf("conditionType", ConditionType.Health)]
    [Tooltip("血量阈值（百分比）")]
    [Range(0f, 1f)]
    public float healthThreshold = 0.3f;
    
    [ShowIf("conditionType", ConditionType.Health)]
    [Tooltip("比较类型")]
    public HealthComparisonType comparisonType = HealthComparisonType.LessThan;
    
    /// <summary>
    /// 创建单个条件实例
    /// </summary>
    public ICondition CreateCondition()
    {
        switch (conditionType)
        {
            case ConditionType.Count:
                var countCondition = new CountCondition();
                countCondition.SetRequiredCount(requiredCount);
                return countCondition;
            case ConditionType.TimeWindow:
                // 暂时返回 null，后续实现
                Debug.LogWarning("TimeWindow 条件暂未实现");
                return null;
            case ConditionType.Health:
                // 暂时返回 null，后续实现
                Debug.LogWarning("Health 条件暂未实现");
                return null;
            default:
                Debug.LogError($"不支持的条件类型: {conditionType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (conditionType)
        {
            case ConditionType.Count:
                return $"计数条件: 需要 {requiredCount} 次";
            case ConditionType.TimeWindow:
                return $"时间窗口条件: {timeWindow}秒内 {requiredCount} 次";
            case ConditionType.Health:
                return $"血量条件: {comparisonType} {healthThreshold:P0}";
            default:
                return $"条件: {conditionType}";
        }
    }
}

/// <summary>
/// 条件配置 - 支持多个条件和逻辑判断
/// 如果没有条件，触发即执行
/// </summary>
[System.Serializable]
public class ConditionConfig
{
    [BoxGroup("条件设置")]
    [LabelText("是否有条件")]
    [Tooltip("如果为false，触发即执行；如果为true，需要满足条件")]
    public bool hasConditions = true;
    
    [BoxGroup("条件设置")]
    [ShowIf("hasConditions")]
    [LabelText("条件逻辑")]
    [Tooltip("多个条件之间的逻辑关系")]
    public ConditionLogicType logicType = ConditionLogicType.And;
    
    [BoxGroup("条件设置")]
    [ShowIf("hasConditions")]
    [LabelText("条件列表")]
    [Tooltip("多个条件的列表")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "conditionType")]
    public List<SingleConditionConfig> conditions = new List<SingleConditionConfig> { new SingleConditionConfig() };
    
    /// <summary>
    /// 创建条件实例
    /// </summary>
    public ICondition CreateCondition()
    {
        // 如果没有条件，返回一个总是返回true的条件
        if (!hasConditions)
        {
            return new AlwaysTrueCondition();
        }
        
        // 如果只有一个条件，直接返回该条件
        if (conditions.Count == 1)
        {
            return conditions[0].CreateCondition();
        }
        
        // 如果有多个条件，创建复合条件
        if (conditions.Count > 1)
        {
            var compositeCondition = new CompositeCondition();
            compositeCondition.SetLogicType(logicType);
            
            foreach (var conditionConfig in conditions)
            {
                var condition = conditionConfig.CreateCondition();
                if (condition != null)
                {
                    compositeCondition.AddCondition(condition);
                }
            }
            
            return compositeCondition;
        }
        
        // 默认返回总是为true的条件
        return new AlwaysTrueCondition();
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (!hasConditions)
        {
            return "无条件（触发即执行）";
        }
        
        if (conditions.Count == 0)
        {
            return "条件列表为空";
        }
        
        if (conditions.Count == 1)
        {
            return conditions[0].GetDebugInfo();
        }
        
        string logicText = logicType == ConditionLogicType.And ? "AND" : "OR";
        string conditionTexts = string.Join($" {logicText} ", conditions.Select(c => c.GetDebugInfo()));
        return $"复合条件 ({logicText}): {conditionTexts}";
    }
}

/// <summary>
/// 血量比较类型
/// </summary>
public enum HealthComparisonType
{
    LessThan,    // 小于
    GreaterThan, // 大于
    Equal        // 等于
}

/// <summary>
/// 条件逻辑类型
/// </summary>
public enum ConditionLogicType
{
    And,    // 所有条件都必须满足
    Or      // 任一条件满足即可
}

/// <summary>
/// 条件类型枚举
/// </summary>
public enum ConditionType
{
    Count,          // 计数条件
    TimeWindow,     // 时间窗口条件（暂未实现）
    Health,         // 血量条件（暂未实现）
    Resource,       // 资源条件（暂未实现）
    State           // 状态条件（暂未实现）
}
