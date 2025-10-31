using UnityEngine;

/// <summary>
/// 基于 Stat 的 Float Property
/// 
/// 【用途】：
/// - 基于目标的 Stat 值计算
/// - 支持百分比或固定值偏移
/// 
/// 【示例】：
/// - 治疗量 = 攻击力的 50%
/// - 伤害 = 最大血量的 10%
/// - 持续时间 = 速度值 / 10
/// </summary>
[System.Serializable]
public class StatBasedFloat : PropertyGetFloat
{
    [Tooltip("Stat ID（如 Damage、MaxHealth）")]
    public string statID = "Damage";
    
    [Tooltip("乘数")]
    public float multiplier = 1.0f;
    
    [Tooltip("附加值")]
    public float additionalValue = 0f;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public StatBasedFloat()
    {
    }
    
    /// <summary>
    /// 带参数构造函数
    /// </summary>
    public StatBasedFloat(string statID, float multiplier, float additionalValue = 0f)
    {
        this.statID = statID;
        this.multiplier = multiplier;
        this.additionalValue = additionalValue;
    }
    
    public override float Get(SkillArgs args)
    {
        if (args == null || args.Target == null)
        {
            Debug.LogWarning($"[StatBasedFloat] 无效的 args，返回附加值 {additionalValue}");
            return additionalValue;
        }
        
        // 获取目标的 StatsManager
        var statsManager = args.Target.GetComponent<PlayerStats>();
        if (statsManager == null)
        {
            Debug.LogWarning($"[StatBasedFloat] 目标没有 PlayerStatsManagerV2，返回附加值 {additionalValue}");
            return additionalValue;
        }
        
        // 获取 Stat 值
        float statValue = statsManager.GetFinalStat(statID);
        
        // 计算：Stat * 乘数 + 附加值
        return statValue * multiplier + additionalValue;
    }
    
    public override string GetDebugInfo()
    {
        string formula = $"{statID} × {multiplier}";
        if (additionalValue != 0)
        {
            formula += $" + {additionalValue}";
        }
        return formula;
    }
}

