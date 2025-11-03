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
    
    // ✅ 多角色系统：技能归属的角色ID
    private string ownerCharacterID;
    
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
    /// ✅ 多角色系统：设置触发器归属的角色ID
    /// </summary>
    /// <param name="characterID">角色ID</param>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
        
        // ⚠️ TODO: DataSourceTrigger 的角色过滤需要扩展 HealthStateData 添加来源信息
        // 当前 HealthStateData 没有包含来源角色的 GameObject/characterID
        // 暂时不做过滤，假设 DataSourceTrigger 技能总是应用到自己角色
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
    /// <param name="args">技能参数</param>
    /// <returns>是否触发</returns>
    public bool CheckEvent(SkillArgs args)
    {
        // 情况1：初始化时检查一次
        if (!hasInitialized)
        {
            hasInitialized = true;
            return true; // 初始化时总是触发
        }
        
        // 情况2：生命值变化时触发
        if (IsHealthDataChange(args.EventData))
        {
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
    /// <param name="args">技能参数</param>
    /// <returns>提取的数值</returns>
    public float GetCurrentValue(SkillArgs args)
    {
        if (dataExtractor != null)
        {
            return dataExtractor(args.EventData);
        }
        return 0f;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        hasInitialized = false;
    }
    
    /// <summary>
    /// 根据类型获取数据提取器
    /// </summary>
    /// <param name="type">数据提取器类型</param>
    /// <returns>数据提取函数</returns>
    private System.Func<object, float> GetDataExtractor(DataExtractorType type)
    {
        return DataExtractors.GetExtractor(type);
    }
}
