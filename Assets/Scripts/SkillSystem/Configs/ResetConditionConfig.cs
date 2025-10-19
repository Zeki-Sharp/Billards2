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
    
    // 值比较条件参数 - 只在 resetType == ValueComparison 时显示
    [ShowIf("resetType", ResetConditionType.ValueComparison)]
    [LabelText("比较类型")]
    [Tooltip("比较类型")]
    public ComparisonType comparisonType = ComparisonType.LessThanOrEqual;
    
    [ShowIf("resetType", ResetConditionType.ValueComparison)]
    [LabelText("目标值")]
    [Tooltip("目标值")]
    public float targetValue = 0.99f;
    
    [ShowIf("resetType", ResetConditionType.ValueComparison)]
    [ShowIf("comparisonType", ComparisonType.InRange)]
    [LabelText("最小值")]
    [Tooltip("最小值")]
    public float minValue = 0f;
    
    [ShowIf("resetType", ResetConditionType.ValueComparison)]
    [ShowIf("comparisonType", ComparisonType.InRange)]
    [LabelText("最大值")]
    [Tooltip("最大值")]
    public float maxValue = 1f;
    
    [ShowIf("resetType", ResetConditionType.ValueComparison)]
    [LabelText("数据提取器类型")]
    [Tooltip("数据提取器类型")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;
    
    
    
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
            case ResetConditionType.ValueComparison:
                return CreateValueComparisonResetCondition();
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
            case ResetConditionType.ValueComparison:
                return $"值比较重置: {GetValueComparisonDebugInfo()}";
            case ResetConditionType.Never:
                return "永不复位";
            default:
                return $"重置条件: {resetType}";
        }
    }
    
    /// <summary>
    /// 创建值比较重置条件
    /// </summary>
    private IResetCondition CreateValueComparisonResetCondition()
    {
        // 获取值提取器
        var valueExtractor = GetDataExtractor(dataExtractorType);
        
        if (comparisonType == ComparisonType.InRange)
        {
            return new ValueComparisonResetCondition(minValue, maxValue, valueExtractor, dataExtractorType);
        }
        else
        {
            return new ValueComparisonResetCondition(comparisonType, targetValue, valueExtractor, dataExtractorType);
        }
    }
    
    /// <summary>
    /// 获取数据提取器函数
    /// </summary>
    private System.Func<object, float> GetDataExtractor(DataExtractorType extractorType)
    {
        switch (extractorType)
        {
            case DataExtractorType.Health:
                return (data) => {
                    if (data is HealthStateData healthData)
                        return healthData.HealthPercentage;
                    return 0f;
                };
            case DataExtractorType.Attack:
                return (data) => {
                    if (data is AttackData attackData)
                        return attackData.Damage;
                    return 0f;
                };
            case DataExtractorType.Defense:
                return (data) => {
                    if (data is AttackData attackData)
                        return attackData.Damage; // 防御值暂时使用伤害值
                    return 0f;
                };
            case DataExtractorType.Speed:
                return (data) => {
                    // 速度变化事件暂未实现
                    return 0f;
                };
            case DataExtractorType.Mana:
                return (data) => {
                    // 法力变化事件暂未实现
                    return 0f;
                };
            default:
                Debug.LogWarning($"[SingleResetConditionConfig] 未知的数据提取器类型: {extractorType}");
                return (data) => 0f;
        }
    }
    
    /// <summary>
    /// 获取值比较调试信息
    /// </summary>
    private string GetValueComparisonDebugInfo()
    {
        if (comparisonType == ComparisonType.InRange)
        {
            return $"{dataExtractorType} 在范围 [{minValue}, {maxValue}] 内";
        }
        else
        {
            return $"{dataExtractorType} {GetComparisonSymbol()} {targetValue}";
        }
    }
    
    /// <summary>
    /// 获取比较符号
    /// </summary>
    private string GetComparisonSymbol()
    {
        switch (comparisonType)
        {
            case ComparisonType.GreaterThan:
                return ">";
            case ComparisonType.GreaterThanOrEqual:
                return ">=";
            case ComparisonType.LessThan:
                return "<";
            case ComparisonType.LessThanOrEqual:
                return "<=";
            case ComparisonType.Equal:
                return "==";
            default:
                return "?";
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
    ValueComparison,        // 值比较重置
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
