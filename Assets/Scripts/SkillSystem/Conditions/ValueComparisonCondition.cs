using UnityEngine;

/// <summary>
/// 值比较条件 - 通用的数值比较逻辑
/// 支持大于、小于、等于、范围等比较操作
/// </summary>
public class ValueComparisonCondition : ICondition
{
    public string ConditionName => "ValueComparisonCondition";
    
    private ComparisonType comparisonType = ComparisonType.GreaterThanOrEqual;
    private float targetValue = 1.0f;
    private float minValue = 0f;
    private float maxValue = 1f;
    private System.Func<object, float> valueExtractor;
    
    /// <summary>
    /// 设置比较类型和目标值
    /// </summary>
    /// <param name="type">比较类型</param>
    /// <param name="value">目标值</param>
    public void SetComparison(ComparisonType type, float value)
    {
        comparisonType = type;
        targetValue = value;
    }
    
    /// <summary>
    /// 设置范围比较
    /// </summary>
    /// <param name="min">最小值</param>
    /// <param name="max">最大值</param>
    public void SetRange(float min, float max)
    {
        comparisonType = ComparisonType.InRange;
        minValue = min;
        maxValue = max;
    }
    
    /// <summary>
    /// 设置值提取器
    /// </summary>
    /// <param name="extractor">值提取函数</param>
    public void SetValueExtractor(System.Func<object, float> extractor)
    {
        valueExtractor = extractor;
    }
    
    /// <summary>
    /// 初始化条件
    /// </summary>
    public void Initialize()
    {
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>条件是否满足</returns>
    public bool CheckCondition(object eventData)
    {
        if (valueExtractor == null)
        {
            Debug.LogWarning($"[{ConditionName}] 值提取器未设置");
            return false;
        }
        
        float currentValue = valueExtractor(eventData);
        
        bool result = false;
        switch (comparisonType)
        {
            case ComparisonType.GreaterThanOrEqual:
                result = currentValue >= targetValue;
                break;
            case ComparisonType.LessThanOrEqual:
                result = currentValue <= targetValue;
                break;
            case ComparisonType.LessThan:
                result = currentValue < targetValue;
                break;
            case ComparisonType.GreaterThan:
                result = currentValue > targetValue;
                break;
            case ComparisonType.Equal:
                result = Mathf.Approximately(currentValue, targetValue);
                break;
            case ComparisonType.InRange:
                result = currentValue >= minValue && currentValue <= maxValue;
                break;
            default:
                Debug.LogError($"[{ConditionName}] 不支持的比较类型: {comparisonType}");
                return false;
        }
        
        Debug.Log($"[{ConditionName}] 值比较: {currentValue} vs {targetValue} ({comparisonType}) = {result}");
        
        // 添加调用栈信息来追踪调用来源
        if (currentValue == 0f && eventData != null)
        {
            Debug.LogWarning($"[{ConditionName}] 检测到值为0，事件数据类型: {eventData.GetType().Name}");
            Debug.LogWarning($"[{ConditionName}] 调用栈: {System.Environment.StackTrace}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 重置条件状态
    /// </summary>
    public void Reset()
    {
        // 值比较条件不需要重置
    }
    
    /// <summary>
    /// 回合结束时重置条件状态
    /// </summary>
    public void ResetOnPhaseEnd()
    {
        // 值比较条件不需要重置，因为它是基于当前值的即时比较
        Debug.Log($"[{ConditionName}] 回合结束重置状态 - 无需操作（值比较条件）");
    }
}

/// <summary>
/// 比较类型枚举
/// </summary>
public enum ComparisonType
{
    GreaterThanOrEqual,  // >=
    LessThanOrEqual,     // <=
    LessThan,            // <
    GreaterThan,         // >
    Equal,               // ==
    InRange              // 范围内
}
