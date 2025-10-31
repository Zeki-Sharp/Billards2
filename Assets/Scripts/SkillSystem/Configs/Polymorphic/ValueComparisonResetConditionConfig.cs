using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 值比较重置条件配置 - 根据数值比较结果决定是否重置
/// </summary>
[System.Serializable]
public class ValueComparisonResetConditionConfig : ResetConditionBase
{
    [LabelText("数据提取器类型")]
    [Tooltip("从事件数据中提取什么类型的数据进行比较")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;

    [LabelText("比较类型")]
    [Tooltip("如何比较提取的数值")]
    public ComparisonType comparisonType = ComparisonType.LessThanOrEqual;

    [LabelText("目标值")]
    [Tooltip("比较的目标值")]
    [HideIf("@comparisonType == ComparisonType.InRange")]
    public float targetValue = 0.99f;

    [LabelText("最小值")]
    [Tooltip("范围比较的最小值")]
    [ShowIf("@comparisonType == ComparisonType.InRange")]
    public float minValue = 0f;

    [LabelText("最大值")]
    [Tooltip("范围比较的最大值")]
    [ShowIf("@comparisonType == ComparisonType.InRange")]
    public float maxValue = 1f;

    public override IResetCondition CreateResetCondition()
    {
        // 使用 DataExtractors 静态类获取提取器
        System.Func<object, float> extractor = DataExtractors.GetExtractor(dataExtractorType);
        
        ValueComparisonResetCondition resetCondition;
        
        if (comparisonType == ComparisonType.InRange)
        {
            // 范围比较构造函数
            resetCondition = new ValueComparisonResetCondition(minValue, maxValue, extractor, dataExtractorType);
        }
        else
        {
            // 普通比较构造函数
            resetCondition = new ValueComparisonResetCondition(comparisonType, targetValue, extractor, dataExtractorType);
        }
        
        return resetCondition;
    }

    public override string GetDebugInfo()
    {
        if (comparisonType == ComparisonType.InRange)
        {
            return $"值比较重置: {dataExtractorType} 在 {minValue}-{maxValue} 范围内";
        }
        else
        {
            return $"值比较重置: {dataExtractorType} {comparisonType} {targetValue}";
        }
    }
}

