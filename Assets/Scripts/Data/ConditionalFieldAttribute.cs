using UnityEngine;

/// <summary>
/// 条件字段特性 - 根据指定字段的值来控制当前字段的显示
/// 这个特性需要放在运行时脚本中，因为它会被ScriptableObject使用
/// 支持多个条件，所有条件都必须满足才显示字段
/// </summary>
public class ConditionalFieldAttribute : PropertyAttribute
{
    public string[] ConditionalSourceFields;
    public bool HideInInspector;
    public bool Inverse;
    public object[] CompareValues;
    
    /// <summary>
    /// 构造函数 - 支持多个条件字段
    /// </summary>
    /// <param name="conditionalSourceFields">条件字段名数组</param>
    /// <param name="hideInInspector">是否在条件不满足时隐藏</param>
    /// <param name="inverse">是否反转条件</param>
    /// <param name="compareValues">比较值数组</param>
    public ConditionalFieldAttribute(string[] conditionalSourceFields, bool hideInInspector = false, bool inverse = false, params object[] compareValues)
    {
        this.ConditionalSourceFields = conditionalSourceFields;
        this.HideInInspector = hideInInspector;
        this.Inverse = inverse;
        this.CompareValues = compareValues;
    }
    
    /// <summary>
    /// 构造函数 - 单个条件字段
    /// </summary>
    /// <param name="conditionalSourceField">条件字段名</param>
    /// <param name="hideInInspector">是否在条件不满足时隐藏</param>
    /// <param name="inverse">是否反转条件</param>
    /// <param name="compareValues">比较值数组</param>
    public ConditionalFieldAttribute(string conditionalSourceField, bool hideInInspector = false, bool inverse = false, params object[] compareValues)
    {
        this.ConditionalSourceFields = new string[] { conditionalSourceField };
        this.HideInInspector = hideInInspector;
        this.Inverse = inverse;
        this.CompareValues = compareValues;
    }
    
    /// <summary>
    /// 简化构造函数 - 只在条件字段为true时显示
    /// </summary>
    /// <param name="conditionalSourceField">条件字段名</param>
    public ConditionalFieldAttribute(string conditionalSourceField)
    {
        this.ConditionalSourceFields = new string[] { conditionalSourceField };
        this.HideInInspector = false;
        this.Inverse = false;
        this.CompareValues = new object[] { true };
    }
}
