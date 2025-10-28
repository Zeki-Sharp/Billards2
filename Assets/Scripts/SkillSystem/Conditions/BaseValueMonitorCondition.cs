using UnityEngine;

/// <summary>
/// 值监控条件基类
/// 提供通用的值变化监听和事件订阅逻辑
/// 子类只需实现具体的回调处理
/// 
/// 【设计目的】：
/// 消除 ValueComparisonResetCondition 和 ValueComparisonEffectRemovalCondition 的重复代码
/// 两者都需要监听值变化，只是回调处理不同：
/// - ResetCondition：重置触发条件 + 设置 canExecute
/// - EffectRemovalCondition：移除效果（删除修改器）
/// </summary>
public abstract class BaseValueMonitorCondition
{
    protected ValueComparisonCondition valueCondition;
    protected DataExtractorType dataExtractorType;
    
    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="comparisonType">比较类型</param>
    /// <param name="targetValue">目标值</param>
    /// <param name="valueExtractor">值提取器</param>
    /// <param name="dataExtractorType">数据提取器类型</param>
    protected BaseValueMonitorCondition(
        ComparisonType comparisonType, 
        float targetValue, 
        System.Func<object, float> valueExtractor, 
        DataExtractorType dataExtractorType)
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
    protected BaseValueMonitorCondition(
        float minValue, 
        float maxValue, 
        System.Func<object, float> valueExtractor, 
        DataExtractorType dataExtractorType)
    {
        valueCondition = new ValueComparisonCondition();
        valueCondition.SetRange(minValue, maxValue);
        valueCondition.SetValueExtractor(valueExtractor);
        this.dataExtractorType = dataExtractorType;
    }
    
    /// <summary>
    /// 开始监听值变化
    /// </summary>
    protected void StartMonitoring()
    {
        valueCondition?.Initialize();
        
        // 根据数据提取器类型订阅相应的事件
        switch (dataExtractorType)
        {
            case DataExtractorType.Health:
                GameEventBus.OnHealthChanged += OnHealthChanged;
                break;
            case DataExtractorType.Attack:
                // 未来扩展：GameEventBus.OnAttackChanged += OnAttackChanged;
                Debug.LogWarning("[BaseValueMonitorCondition] Attack 数据源监听暂未实现");
                break;
            case DataExtractorType.Defense:
                // 未来扩展
                Debug.LogWarning("[BaseValueMonitorCondition] Defense 数据源监听暂未实现");
                break;
            case DataExtractorType.Speed:
                Debug.LogWarning("[BaseValueMonitorCondition] Speed 数据源监听暂未实现");
                break;
            case DataExtractorType.Mana:
                Debug.LogWarning("[BaseValueMonitorCondition] Mana 数据源监听暂未实现");
                break;
            default:
                Debug.LogError($"[BaseValueMonitorCondition] 未知的数据提取器类型: {dataExtractorType}");
                break;
        }
    }
    
    /// <summary>
    /// 停止监听值变化
    /// </summary>
    protected void StopMonitoring()
    {
        // 根据数据提取器类型取消订阅事件
        switch (dataExtractorType)
        {
            case DataExtractorType.Health:
                GameEventBus.OnHealthChanged -= OnHealthChanged;
                break;
            case DataExtractorType.Attack:
                // GameEventBus.OnAttackChanged -= OnAttackChanged;
                break;
            case DataExtractorType.Defense:
                // 未来扩展
                break;
        }
        
        valueCondition?.Reset();
    }
    
    /// <summary>
    /// 检查事件是否与数据提取器类型相关
    /// </summary>
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
                return false; // 未实现
            case DataExtractorType.Mana:
                return false; // 未实现
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 检查条件是否满足
    /// </summary>
    protected bool CheckCondition(object eventData)
    {
        return valueCondition?.CheckCondition(eventData) ?? false;
    }
    
    // ========== 事件回调（根据 DataExtractorType 分发） ==========
    
    /// <summary>
    /// 血量变化事件回调
    /// </summary>
    private void OnHealthChanged(HealthStateData healthData)
    {
        if (CheckCondition(healthData))
        {
            OnConditionMet(healthData);
        }
    }
    
    // 未来扩展其他类型：
    // private void OnAttackChanged(AttackData attackData)
    // {
    //     if (CheckCondition(attackData))
    //     {
    //         OnConditionMet(attackData);
    //     }
    // }
    
    /// <summary>
    /// 条件满足时的回调 - 由子类实现具体逻辑
    /// </summary>
    /// <param name="eventData">事件数据</param>
    protected abstract void OnConditionMet(object eventData);
}

