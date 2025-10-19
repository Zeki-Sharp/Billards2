using UnityEngine;

/// <summary>
/// 值比较重置条件
/// 基于数值比较来决定是否重置技能触发条件和效果状态
/// 用于实现"当某个条件满足时重置技能"的逻辑
/// </summary>
public class ValueComparisonResetCondition : IResetCondition
{
    public string ConditionName => "ValueComparisonResetCondition";
    
    private ValueComparisonCondition valueCondition;
    private DataExtractorType dataExtractorType;
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
    public ValueComparisonResetCondition(float minValue, float maxValue, System.Func<object, float> valueExtractor, DataExtractorType dataExtractorType)
    {
        valueCondition = new ValueComparisonCondition();
        valueCondition.SetRange(minValue, maxValue);
        valueCondition.SetValueExtractor(valueExtractor);
        this.dataExtractorType = dataExtractorType;
    }
    
    /// <summary>
    /// 初始化重置条件
    /// </summary>
    public void Initialize()
    {
        valueCondition?.Initialize();
        
        // 根据数据提取器类型订阅相应的事件
        if (dataExtractorType == DataExtractorType.Health) {
            GameEventBus.OnHealthChanged += OnHealthChanged;
        }
        // 其他数据类型可以后续添加
    }
    
    /// <summary>
    /// 检查是否应该重置触发条件
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该重置触发条件</returns>
    public bool ShouldReset(object eventData)
    {
        if (valueCondition == null)
        {
            Debug.LogWarning($"[{ConditionName}] 值比较条件未设置");
            return false;
        }
        
        bool shouldReset = valueCondition.CheckCondition(eventData);
        
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
        // 取消订阅事件
        if (dataExtractorType == DataExtractorType.Health) {
            GameEventBus.OnHealthChanged -= OnHealthChanged;
            Debug.Log($"[{ConditionName}] 取消订阅生命值变化事件");
        }
        
        valueCondition?.Reset();
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
    /// 处理生命值变化事件
    /// </summary>
    /// <param name="healthData">生命值数据</param>
    private void OnHealthChanged(HealthStateData healthData)
    {
        // 检查是否应该重置
        if (ShouldReset(healthData)) {
            // 重置触发条件和 canExecute
            condition?.Reset();
            effect?.SetCanExecute(true);
            Debug.Log($"[{ConditionName}] 生命值变化触发重置 - 当前生命值: {healthData.HealthPercentage:P1}, 技能实例ID: {targetSkillInstanceId}");
        }
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
