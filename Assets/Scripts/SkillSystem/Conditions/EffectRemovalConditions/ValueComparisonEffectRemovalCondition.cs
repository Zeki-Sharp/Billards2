using UnityEngine;

/// <summary>
/// 值比较效果移除条件
/// 基于数值比较来决定是否移除效果
/// </summary>
public class ValueComparisonEffectRemovalCondition : BaseValueMonitorCondition, IEffectRemovalCondition
{
    public string ConditionName => "ValueComparisonEffectRemovalCondition";
    
    private IEffect effect;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="comparisonType">比较类型</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    public ValueComparisonEffectRemovalCondition(ComparisonType comparisonType, float targetValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
        : base(comparisonType, targetValue, valueExtractor, dataExtractorType)
    {
    }
    
    /// <summary>
    /// 构造函数 - 范围比较
    /// </summary>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    public ValueComparisonEffectRemovalCondition(float minValue, float maxValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
        : base(minValue, maxValue, valueExtractor, dataExtractorType)
    {
    }
    
    /// <summary>
    /// 初始化效果移除条件
    /// </summary>
    public void Initialize()
    {
        StartMonitoring();  // 调用基类方法开始监听
    }
    
    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        bool shouldRemove = CheckCondition(eventData);  // 调用基类方法
        
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
        StopMonitoring();  // 调用基类方法停止监听
        Debug.Log($"[{ConditionName}] 已停止监听");
    }
    
    /// <summary>
    /// 设置效果引用（用于实时移除效果）
    /// </summary>
    /// <param name="effect">效果引用</param>
    public void SetDependencies(IEffect effect)
    {
        this.effect = effect;
        Debug.Log($"[{ConditionName}] 设置依赖组件 - 效果: {effect?.EffectName}");
    }
    
    /// <summary>
    /// 条件满足时的回调 - 实现基类抽象方法
    /// EffectRemovalCondition 的特定处理：移除效果（删除修改器）
    /// </summary>
    protected override void OnConditionMet(object eventData)
    {
        // 移除效果
        effect?.RemoveEffect();
        
        if (eventData is HealthStateData healthData)
        {
            Debug.Log($"[{ConditionName}] 生命值变化触发效果移除 - 当前生命值: {healthData.HealthPercentage:P1}");
        }
        else
        {
            Debug.Log($"[{ConditionName}] 条件满足，已移除效果");
        }
    }
}
