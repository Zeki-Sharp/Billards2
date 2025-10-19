using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 效果移除配置 - 用于配置持续效果何时移除的条件
/// 只有PropertyEffect（持续效果）需要配置此选项
/// </summary>
[System.Serializable]
public class EffectRemovalConfig
{
    [LabelText("效果移除类型")]
    [Tooltip("持续效果何时移除的条件")]
    public EffectRemovalType removalType = EffectRemovalType.Duration;
    
    [ShowIf("removalType", EffectRemovalType.Duration)]
    [LabelText("持续时间")]
    [Tooltip("持续时间（秒）")]
    [MinValue(0.1f)]
    public float duration = 30f;
    
    
    [ShowIf("removalType", EffectRemovalType.OnConditionMet)]
    [LabelText("移除条件")]
    [Tooltip("满足条件时移除效果")]
    public ICondition removalCondition;
    
    [ShowIf("removalType", EffectRemovalType.ValueComparison)]
    [LabelText("比较类型")]
    [Tooltip("数值比较类型")]
    public ComparisonType comparisonType = ComparisonType.LessThan;
    
    [ShowIf("@removalType == EffectRemovalType.ValueComparison && comparisonType != ComparisonType.InRange")]
    [LabelText("目标值")]
    [Tooltip("比较的目标值")]
    public float targetValue = 0.5f;
    
    [ShowIf("@removalType == EffectRemovalType.ValueComparison && comparisonType == ComparisonType.InRange")]
    [LabelText("最小值")]
    [Tooltip("范围比较的最小值")]
    public float minValue = 0f;
    
    [ShowIf("@removalType == EffectRemovalType.ValueComparison && comparisonType == ComparisonType.InRange")]
    [LabelText("最大值")]
    [Tooltip("范围比较的最大值")]
    public float maxValue = 1f;
    
    [ShowIf("removalType", EffectRemovalType.ValueComparison)]
    [LabelText("数据提取器类型")]
    [Tooltip("用于提取数值的数据提取器类型")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;
    
    /// <summary>
    /// 创建效果移除条件实例
    /// </summary>
    public IEffectRemovalCondition CreateEffectRemovalCondition()
    {
        switch (removalType)
        {
            case EffectRemovalType.Duration:
                return new DurationEffectRemovalCondition(duration);
            case EffectRemovalType.OnPlayerPhaseEnded:
                return new OnPhaseEndedEffectRemovalCondition();
            case EffectRemovalType.OnLevelCompleted:
                return new OnLevelCompletedEffectRemovalCondition();
            case EffectRemovalType.OnConditionMet:
                return new OnConditionMetEffectRemovalCondition(removalCondition);
            case EffectRemovalType.ValueComparison:
                return CreateValueComparisonRemovalCondition();
            case EffectRemovalType.Never:
                return new NeverEffectRemovalCondition();
            default:
                Debug.LogError($"不支持的效果移除类型: {removalType}");
                return null;
        }
    }
    
    /// <summary>
    /// 创建值比较移除条件
    /// </summary>
    private IEffectRemovalCondition CreateValueComparisonRemovalCondition()
    {
        // 直接使用DataExtractors静态类获取数据提取器
        System.Func<object, float> valueExtractor = DataExtractors.GetExtractor(dataExtractorType);
        
        // 根据比较类型创建条件
        if (comparisonType == ComparisonType.InRange)
        {
            return new ValueComparisonEffectRemovalCondition(minValue, maxValue, valueExtractor, dataExtractorType);
        }
        else
        {
            return new ValueComparisonEffectRemovalCondition(comparisonType, targetValue, valueExtractor, dataExtractorType);
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (removalType)
        {
            case EffectRemovalType.Duration:
                return $"效果移除: {removalType}({duration}s)";
            case EffectRemovalType.ValueComparison:
                if (comparisonType == ComparisonType.InRange)
                {
                    return $"效果移除: {removalType}({minValue}-{maxValue})";
                }
                else
                {
                    return $"效果移除: {removalType}({comparisonType} {targetValue})";
                }
            default:
                return $"效果移除: {removalType}";
        }
    }
}

/// <summary>
/// 效果移除类型枚举
/// </summary>
public enum EffectRemovalType
{
    Duration,            // 持续时间
    OnPlayerPhaseEnded,  // 回合结束移除
    OnLevelCompleted,    // 关卡完成时移除
    OnConditionMet,      // 满足条件时移除
    ValueComparison,     // 值比较移除
    Never                // 永不移除
}
