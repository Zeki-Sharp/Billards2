using UnityEngine;

/// <summary>
/// 固定值 Float Property
/// 
/// 【用途】：
/// - 返回固定的常量值
/// - 最简单的值提供者
/// 
/// 【示例】：
/// - 固定回复 20 点血
/// - 固定造成 50 点伤害
/// </summary>
[System.Serializable]
public class ConstantFloat : PropertyGetFloat
{
    [Tooltip("固定值")]
    public float value = 0f;
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public ConstantFloat()
    {
    }
    
    /// <summary>
    /// 带参数构造函数
    /// </summary>
    public ConstantFloat(float value)
    {
        this.value = value;
    }
    
    public override float Get(SkillArgs args)
    {
        return value;
    }
    
    public override string GetDebugInfo()
    {
        return $"固定值: {value}";
    }
}

