using UnityEngine;

/// <summary>
/// 关卡完成时移除效果的条件
/// 当关卡完成事件触发时，移除相关的修饰器效果
/// </summary>
public class OnLevelCompletedEffectRemovalCondition : IEffectRemovalCondition
{
    public string ConditionName => "关卡完成时移除";
    
    /// <summary>
    /// 初始化移除条件
    /// </summary>
    public void Initialize()
    {
        // 关卡完成移除条件不需要特殊初始化
    }
    
    /// <summary>
    /// 重置移除条件
    /// </summary>
    public void Reset()
    {
        // 关卡完成移除条件不需要特殊重置
    }
    
    /// <summary>
    /// 检查是否应该移除效果
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>是否应该移除效果</returns>
    public bool ShouldRemoveEffect(object eventData)
    {
        // 检查是否是关卡完成事件
        if (eventData is LevelCompletedData levelCompletedData)
        {
            Debug.Log($"[OnLevelCompletedEffectRemovalCondition] 关卡完成，移除效果 - 关卡: {levelCompletedData.LevelIndex}");
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        return $"移除条件: {ConditionName}";
    }
}

/// <summary>
/// 关卡完成事件数据
/// </summary>
public class LevelCompletedData
{
    public int LevelIndex { get; set; }
    public LevelConfig LevelConfig { get; set; }
    
    public LevelCompletedData(int levelIndex, LevelConfig levelConfig)
    {
        LevelIndex = levelIndex;
        LevelConfig = levelConfig;
    }
}
