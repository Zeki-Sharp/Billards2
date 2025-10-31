using UnityEngine;

/// <summary>
/// 值比较重置条件
/// 基于数值比较来决定是否重置技能触发条件和效果状态
/// 用于实现"当某个条件满足时重置技能"的逻辑
/// </summary>
public class ValueComparisonResetCondition : BaseValueMonitorCondition, IResetCondition
{
    public string ConditionName => "ValueComparisonResetCondition";
    
    private ICondition condition;
    private IEffect effect;
    private string targetSkillInstanceId;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="comparisonType">比较类型</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    public ValueComparisonResetCondition(ComparisonType comparisonType, float targetValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
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
    public ValueComparisonResetCondition(float minValue, float maxValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
        : base(minValue, maxValue, valueExtractor, dataExtractorType)
    {
    }
    
    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        StartMonitoring();  // 调用基类方法开始监听
    }
    
    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该重置触发条件</returns>
    public bool ShouldReset(SkillArgs args)
    {
        bool shouldReset = CheckCondition(args);  // 调用基类方法
        
        if (shouldReset)
        {
            Debug.Log($"[{ConditionName}] 条件满足，应该重置触发条件");
        }
        
        return shouldReset;
    }
    
    /// <summary>
    /// 重置重置条件状态
    /// </summary>
    public void Reset()
    {
        StopMonitoring();  // 调用基类方法停止监听
        Debug.Log($"[{ConditionName}] 已停止监听");
    }
    
    /// <summary>
    /// 设置目标技能实例ID（用于事件过滤）
    /// </summary>
    /// <param name="skillInstanceId">技能实例ID</param>
    public void SetTargetSkillInstanceId(string skillInstanceId)
    {
        this.targetSkillInstanceId = skillInstanceId;
        Debug.Log($"[{ConditionName}] 设置目标技能实例ID: {skillInstanceId}");
    }
    
    /// <summary>
    /// 设置依赖组件
    /// </summary>
    /// <param name="condition">触发条件</param>
    /// <param name="effect">效果</param>
    public void SetDependencies(ICondition condition, IEffect effect)
    {
        this.condition = condition;
        this.effect = effect;
        Debug.Log($"[{ConditionName}] 设置依赖组件 - 触发条件: {condition?.ConditionName}, 效果: {effect?.EffectName}");
    }
    
    /// <summary>
    /// 条件满足时的回调 - 实现基类抽象方法
    /// ResetCondition 的特定处理：重置触发条件 + 设置 canExecute
    /// </summary>
    protected override void OnConditionMet(object eventData)
    {
        // 重置触发条件和 canExecute
        condition?.Reset();
        effect?.SetCanExecute(true);
        
        if (eventData is HealthStateData healthData)
        {
            Debug.Log($"[{ConditionName}] 生命值变化触发重置 - 当前生命值: {healthData.HealthPercentage:P1}, 技能实例ID: {targetSkillInstanceId}");
        }
        else
        {
            Debug.Log($"[{ConditionName}] 条件满足，已重置触发条件");
        }
    }
}
