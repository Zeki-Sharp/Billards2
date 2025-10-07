using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 条件配置 - 用于配置技能条件的参数
/// 根据条件类型显示对应参数
/// </summary>
[System.Serializable]
public class ConditionConfig
{
    [Header("条件类型")]
    public ConditionType conditionType = ConditionType.Count;
    
    // 计数条件参数 - 只在 conditionType == Count 时显示
    [Header("计数条件参数")]
    [ShowIf("conditionType", ConditionType.Count)]
    [Tooltip("需要达到的计数")]
    public int requiredCount = 2;
    
    // 时间窗口条件参数 - 只在 conditionType == TimeWindow 时显示
    [Header("时间窗口条件参数")]
    [ShowIf("conditionType", ConditionType.TimeWindow)]
    [Tooltip("时间窗口长度（秒）")]
    public float timeWindow = 5f;
    
    // 血量条件参数 - 只在 conditionType == Health 时显示
    [Header("血量条件参数")]
    [ShowIf("conditionType", ConditionType.Health)]
    [Tooltip("血量阈值（百分比）")]
    [Range(0f, 1f)]
    public float healthThreshold = 0.3f;
    
    [ShowIf("conditionType", ConditionType.Health)]
    [Tooltip("比较类型")]
    public HealthComparisonType comparisonType = HealthComparisonType.LessThan;
    
    /// <summary>
    /// 创建条件实例
    /// </summary>
    public ICondition CreateCondition()
    {
        switch (conditionType)
        {
            case ConditionType.Count:
                var countCondition = new CountCondition();
                countCondition.SetRequiredCount(requiredCount);
                return countCondition;
            case ConditionType.TimeWindow:
                // 暂时返回 null，后续实现
                Debug.LogWarning("TimeWindow 条件暂未实现");
                return null;
            case ConditionType.Health:
                // 暂时返回 null，后续实现
                Debug.LogWarning("Health 条件暂未实现");
                return null;
            default:
                Debug.LogError($"不支持的条件类型: {conditionType}");
                return null;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        switch (conditionType)
        {
            case ConditionType.Count:
                return $"计数条件: 需要 {requiredCount} 次";
            case ConditionType.TimeWindow:
                return $"时间窗口条件: {timeWindow}秒内 {requiredCount} 次";
            case ConditionType.Health:
                return $"血量条件: {comparisonType} {healthThreshold:P0}";
            default:
                return $"条件: {conditionType}";
        }
    }
}

/// <summary>
/// 血量比较类型
/// </summary>
public enum HealthComparisonType
{
    LessThan,    // 小于
    GreaterThan, // 大于
    Equal        // 等于
}

/// <summary>
/// 条件类型枚举
/// </summary>
public enum ConditionType
{
    Count,          // 计数条件
    TimeWindow,     // 时间窗口条件（暂未实现）
    Health,         // 血量条件（暂未实现）
    Resource,       // 资源条件（暂未实现）
    State           // 状态条件（暂未实现）
}
