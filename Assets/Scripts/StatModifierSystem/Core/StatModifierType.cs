/// <summary>
/// 修饰器类型枚举
/// 定义属性修改的计算方式
/// </summary>
public enum StatModifierType
{
    /// <summary>
    /// 固定值加算：基础值 + Value
    /// 例如：基础攻击 10，Add 5 → 最终 15
    /// </summary>
    Add,
    
    /// <summary>
    /// 百分比加算：基础值 * (1 + Value)
    /// 例如：基础攻击 10，PercentAdd 0.5 → 最终 15
    /// </summary>
    PercentAdd,
    
    /// <summary>
    /// 百分比乘算：最终值 * Value
    /// 例如：基础攻击 10，PercentMult 1.5 → 最终 15
    /// </summary>
    PercentMult
}

