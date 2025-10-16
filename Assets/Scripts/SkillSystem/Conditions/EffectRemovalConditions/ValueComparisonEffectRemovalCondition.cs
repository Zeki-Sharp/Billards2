using UnityEngine;

/// <summary>
/// 值比较效果移除条件
/// 基于数值比较来决定是否移除效果
/// </summary>
public class ValueComparisonEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "ValueComparisonEffectRemovalCondition";
    
    private ValueComparisonCondition valueCondition;
    private DataExtractorType dataExtractorType;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="comparisonType">比较类型</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    public ValueComparisonEffectRemovalCondition(ComparisonType comparisonType, float targetValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
    {
        valueCondition = new ValueComparisonCondition();
        valueCondition.SetComparison(comparisonType, targetValue);
        valueCondition.SetValueExtractor(valueExtractor);
        this.dataExtractorType = dataExtractorType;
    }
    
    /// <summary>
    /// 构造函数 - 范围比较
    /// </summary>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    public ValueComparisonEffectRemovalCondition(float minValue, float maxValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
    {
        valueCondition = new ValueComparisonCondition();
        valueCondition.SetRange(minValue, maxValue);
        valueCondition.SetValueExtractor(valueExtractor);
        this.dataExtractorType = dataExtractorType;
    }
    
    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        valueCondition?.Initialize();
        Debug.Log($"[{ConditionName}] 初始化完成 - 值比较移除条件");
    }
    
    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        if (valueCondition == null)
        {
            Debug.LogWarning($"[{ConditionName}] 值比较条件未设置");
            return false;
        }
        
        bool shouldRemove = valueCondition.CheckCondition(eventData);
        
        if (shouldRemove)
        {
            Debug.Log($"[{ConditionName}] 条件满足，移除效果");
        }
        
        return shouldRemove;
    }
    
    /// <summary>
    /// 重置效果移除条件状态
    /// </summary>
    public void Reset()
    {
        valueCondition?.Reset();
        Debug.Log($"[{ConditionName}] 状态重置 - 值比较移除条件已重置");
    }
    
    /// <summary>
    /// 检查事件是否与数据提取器类型相关
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否相关</returns>
    public bool IsEventRelevant(object eventData)
    {
        switch (dataExtractorType)
        {
            case DataExtractorType.Health:
                return eventData is HealthStateData;
            case DataExtractorType.Attack:
                return eventData is AttackData;
            case DataExtractorType.Defense:
                return eventData is AttackData; // 防御通常与攻击事件相关
            case DataExtractorType.Speed:
                return false; // 速度变化事件暂未实现
            case DataExtractorType.Mana:
                return false; // 法力变化事件暂未实现
            default:
                Debug.LogWarning($"[{ConditionName}] 未知的数据提取器类型: {dataExtractorType}");
                return false;
        }
    }
}
