using UnityEngine;

/// <summary>
/// 随机值 Float Property
/// 
/// 【用途】：
/// - 返回指定范围内的随机值
/// - 增加游戏随机性和变化
/// 
/// 【示例】：
/// - 回复 10-30 点血
/// - 造成 40-60 点伤害
/// - 持续 5-10 秒
/// </summary>
[System.Serializable]
public class RandomFloat : PropertyGetFloat
{
    [Tooltip("最小值")]
    public float minValue = 0f;
    
    [Tooltip("最大值")]
    public float maxValue = 100f;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public RandomFloat()
    {
    }
    
    /// <summary>
    /// 带参数构造函数
    /// </summary>
    public RandomFloat(float minValue, float maxValue)
    {
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
    
    public override float Get(SkillArgs args)
    {
        return Random.Range(minValue, maxValue);
    }
    
    public override string GetDebugInfo()
    {
        return $"随机值: {minValue} ~ {maxValue}";
    }
}

