using UnityEngine;

/// <summary>
/// 基于 Attribute 百分比的 Float Property
/// 
/// 【用途】：
/// - 基于目标的 Attribute 当前值或最大值计算
/// - 支持百分比计算
/// 
/// 【示例】：
/// - 回复最大血量的 20%
/// - 造成当前血量的 50% 伤害
/// - 消耗当前能量的 30%
/// </summary>
[System.Serializable]
public class AttributeRatioFloat : PropertyGetFloat
{
    public enum ValueSource
    {
        CurrentValue,  // 当前值
        MaxValue,      // 最大值
        Ratio          // 百分比（当前/最大）
    }
    
    [Tooltip("属性ID（如 Health）")]
    public string attributeID = "Health";
    
    [Tooltip("值来源")]
    public ValueSource source = ValueSource.MaxValue;
    
    [Tooltip("百分比（0.2 = 20%）")]
    [Range(0f, 2f)]
    public float ratio = 0.2f;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public AttributeRatioFloat()
    {
    }
    
    /// <summary>
    /// 带参数构造函数
    /// </summary>
    public AttributeRatioFloat(string attributeID, float ratio, ValueSource source = ValueSource.MaxValue)
    {
        this.attributeID = attributeID;
        this.ratio = ratio;
        this.source = source;
    }
    
    public override float Get(SkillArgs args)
    {
        if (args == null || args.Target == null)
        {
            Debug.LogWarning($"[AttributeRatioFloat] 无效的 args，返回 0");
            return 0f;
        }
        
        // 获取目标的 StatsManager
        var statsManager = args.Target.GetComponent<PlayerStatsManagerV2>();
        if (statsManager == null)
        {
            Debug.LogWarning($"[AttributeRatioFloat] 目标没有 PlayerStatsManagerV2，返回 0");
            return 0f;
        }
        
        // 获取 Attribute
        var attribute = statsManager.GetAttribute(attributeID);
        if (attribute == null)
        {
            Debug.LogWarning($"[AttributeRatioFloat] 属性 {attributeID} 不存在，返回 0");
            return 0f;
        }
        
        // 根据来源计算值
        float baseValue = 0f;
        switch (source)
        {
            case ValueSource.CurrentValue:
                baseValue = attribute.CurrentValue;
                break;
            case ValueSource.MaxValue:
                baseValue = attribute.MaxValue;
                break;
            case ValueSource.Ratio:
                baseValue = attribute.Ratio;
                break;
        }
        
        return baseValue * ratio;
    }
    
    public override string GetDebugInfo()
    {
        return $"{attributeID}.{source} × {ratio * 100}%";
    }
}

