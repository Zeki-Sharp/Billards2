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
    [LabelText("条件类型")]
    [Tooltip("选择条件的判断类型")]
    public ConditionType conditionType = ConditionType.Count;
    
    // 计数条件参数 - 只在 conditionType == Count 时显示
    [ShowIf("conditionType", ConditionType.Count)]
    [LabelText("需要达到的计数")]
    [Tooltip("需要达到的计数")]
    public int requiredCount = 2;
    
    // 时间窗口条件参数 - 只在 conditionType == TimeWindow 时显示
    [ShowIf("conditionType", ConditionType.TimeWindow)]
    [LabelText("时间窗口长度")]
    [Tooltip("时间窗口长度（秒）")]
    public float timeWindow = 5f;
    
    // 值比较条件参数 - 只在 conditionType == ValueComparison 时显示
    [ShowIf("conditionType", ConditionType.ValueComparison)]
    [LabelText("比较类型")]
    [Tooltip("比较类型")]
    public ComparisonType comparisonType = ComparisonType.GreaterThanOrEqual;
    
    [ShowIf("conditionType", ConditionType.ValueComparison)]
    [LabelText("目标值")]
    [Tooltip("目标值")]
    public float targetValue = 1.0f;
    
    [ShowIf("conditionType", ConditionType.ValueComparison)]
    [ShowIf("comparisonType", ComparisonType.InRange)]
    [LabelText("最小值")]
    [Tooltip("最小值")]
    public float minValue = 0f;
    
    [ShowIf("conditionType", ConditionType.ValueComparison)]
    [ShowIf("comparisonType", ComparisonType.InRange)]
    [LabelText("最大值")]
    [Tooltip("最大值")]
    public float maxValue = 1f;
    
    [ShowIf("conditionType", ConditionType.ValueComparison)]
    [LabelText("数据提取器类型")]
    [Tooltip("数据提取器类型")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;
    
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
            case ConditionType.ValueComparison:
                var valueComparisonCondition = new ValueComparisonCondition();
                valueComparisonCondition.SetComparison(comparisonType, targetValue);
                if (comparisonType == ComparisonType.InRange)
                {
                    valueComparisonCondition.SetRange(minValue, maxValue);
                }
                valueComparisonCondition.SetValueExtractor(GetDataExtractor(dataExtractorType));
                return valueComparisonCondition;
            case ConditionType.TimeWindow:
                // 暂时返回 null，后续实现
                Debug.LogWarning("TimeWindow 条件暂未实现");
                return null;
            default:
                Debug.LogError($"不支持的条件类型: {conditionType}");
                return null;
        }
    }
    
    /// <summary>
    /// 根据类型获取数据提取器
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    /// <returns>数据提取函数</returns>
    private System.Func<object, float> GetDataExtractor(DataExtractorType type)
    {
        switch (type)
        {
            case DataExtractorType.Health:
                return DataExtractors.HealthExtractor;
            case DataExtractorType.Attack:
                return DataExtractors.AttackExtractor;
            case DataExtractorType.Defense:
                return DataExtractors.DefenseExtractor;
            case DataExtractorType.Speed:
                return DataExtractors.SpeedExtractor;
            case DataExtractorType.Mana:
                return DataExtractors.ManaExtractor;
            default:
                Debug.LogError($"不支持的数据提取器类型: {type}");
                return (eventData) => 0f;
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
            case ConditionType.ValueComparison:
                if (comparisonType == ComparisonType.InRange)
                {
                    return $"值比较条件: {dataExtractorType} 在 {minValue}-{maxValue} 范围内";
                }
                else
                {
                    return $"值比较条件: {dataExtractorType} {comparisonType} {targetValue}";
                }
            case ConditionType.TimeWindow:
                return $"时间窗口条件: {timeWindow}秒内 {requiredCount} 次";
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
    [LabelText("是否有条件")]
    [Tooltip("如果为false，触发即执行；如果为true，需要满足条件")]
    public bool hasConditions = true;
    
    [ShowIf("hasConditions")]
    [LabelText("条件逻辑")]
    [Tooltip("多个条件之间的逻辑关系")]
    public ConditionLogicType logicType = ConditionLogicType.And;
    
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
    Count,              // 计数条件
    ValueComparison,    // 值比较条件
    TimeWindow,         // 时间窗口条件（暂未实现）
    Resource,           // 资源条件（暂未实现）
    State               // 状态条件（暂未实现）
}
