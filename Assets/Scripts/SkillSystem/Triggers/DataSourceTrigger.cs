using UnityEngine;

/// <summary>
/// 数据源触发器 - 专门监控生命值变化
/// </summary>
public class DataSourceTrigger : ITrigger
{
    public string TriggerName => "DataSourceTrigger";
    
    private System.Func<object, float> dataExtractor;
    private DataExtractorType extractorType;
    private bool hasInitialized = false;
    
    /// <summary>
    /// 设置数据提取器
    /// </summary>
    /// <param name="extractor">数据提取函数</param>
    public void SetDataExtractor(System.Func<object, float> extractor)
    {
        dataExtractor = extractor;
    }
    
    /// <summary>
    /// 设置数据提取器类型
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    public void SetDataExtractorType(DataExtractorType type)
    {
        extractorType = type;
        dataExtractor = GetDataExtractor(type);
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        hasInitialized = false;
    }
    
    /// <summary>
    /// 检查事件 - 处理初始化和生命值变化
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否触发</returns>
    public bool CheckEvent(object eventData)
    {
        // 情况1：初始化时检查一次
        if (!hasInitialized)
        {
            hasInitialized = true;
            Debug.Log($"[DataSourceTrigger] 初始化检查，数据类型: {extractorType}");
            return true; // 初始化时总是触发
        }
        
        // 情况2：生命值变化时触发
        if (IsHealthDataChange(eventData))
        {
            Debug.Log($"[DataSourceTrigger] 检测到生命值变化事件");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查是否为生命值数据变化
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否为生命值变化</returns>
    private bool IsHealthDataChange(object eventData)
    {
        // 只处理生命值相关事件
        return eventData is HealthStateData;
    }
    
    /// <summary>
    /// 获取当前值 - 供条件使用
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>提取的数值</returns>
    public float GetCurrentValue(object eventData)
    {
        if (dataExtractor != null)
        {
            return dataExtractor(eventData);
        }
        return 0f;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        hasInitialized = false;
        Debug.Log($"[DataSourceTrigger] 重置触发器状态");
    }
    
    /// <summary>
    /// 根据类型获取数据提取器
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    /// <returns>数据提取函数</returns>
    private System.Func<object, float> GetDataExtractor(DataExtractorType type)
    {
        switch (type)
        {
            case DataExtractorType.Health:
                return DataExtractors.HealthExtractor;
            case DataExtractorType.Attack:
                return DataExtractors.AttackExtractor;
            case DataExtractorType.Defense:
                return DataExtractors.DefenseExtractor;
            case DataExtractorType.Speed:
                return DataExtractors.SpeedExtractor;
            case DataExtractorType.Mana:
                return DataExtractors.ManaExtractor;
            default:
                Debug.LogError($"不支持的数据提取器类型: {type}");
                return null;
        }
    }
}
