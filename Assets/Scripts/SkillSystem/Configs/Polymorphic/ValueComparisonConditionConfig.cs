using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 值比较条件配置 - 比较数值是否满足条件
/// </summary>
[System.Serializable]
public class ValueComparisonConditionConfig : ConditionBase
{
    [LabelText("数据提取器类型")]
    [Tooltip("从事件数据中提取什么类型的数据进行比较")]
    public DataExtractorType dataExtractorType = DataExtractorType.Health;

    [LabelText("比较类型")]
    [Tooltip("如何比较提取的数值")]
    public ComparisonType comparisonType = ComparisonType.GreaterThanOrEqual;

    [LabelText("目标值")]
    [Tooltip("比较的目标值")]
    [HideIf("@comparisonType == ComparisonType.InRange")]
    public float targetValue = 1.0f;

    [LabelText("最小值")]
    [Tooltip("范围比较的最小值")]
    [ShowIf("@comparisonType == ComparisonType.InRange")]
    public float minValue = 0f;

    [LabelText("最大值")]
    [Tooltip("范围比较的最大值")]
    [ShowIf("@comparisonType == ComparisonType.InRange")]
    public float maxValue = 1f;

    public override ICondition CreateCondition()
    {
        var valueComparisonCondition = new ValueComparisonCondition();
        valueComparisonCondition.SetComparison(comparisonType, targetValue);
        
        if (comparisonType == ComparisonType.InRange)
        {
            valueComparisonCondition.SetRange(minValue, maxValue);
        }
        
        // 使用DataExtractors静态类获取提取器
        System.Func<object, float> extractor = DataExtractors.GetExtractor(dataExtractorType);
        valueComparisonCondition.SetValueExtractor(extractor);
        
        return valueComparisonCondition;
    }

    public override string GetDebugInfo()
    {
        if (comparisonType == ComparisonType.InRange)
        {
            return $"值比较条件: {dataExtractorType} 在 {minValue}-{maxValue} 范围内";
        }
        else
        {
            return $"值比较条件: {dataExtractorType} {comparisonType} {targetValue}";
        }
    }
}

