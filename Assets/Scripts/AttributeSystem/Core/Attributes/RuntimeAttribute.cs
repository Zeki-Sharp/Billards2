using UnityEngine;

/// <summary>
/// 运行时属性资源 - 管理单个 Attribute 的运行时状态
/// 
/// 【设计理念】：
/// - 管理有上下限的动态资源
/// - 提供当前值、最大值、百分比等访问
/// - 自动 Clamp 到范围内
/// - 支持事件通知
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 RuntimeAttribute
/// - MaxValue 可以引用 Stat（本项目简化处理）
/// 
/// 【核心功能】：
/// - 当前值管理（CurrentValue）
/// - 百分比计算（Ratio）
/// - 自动限制范围
/// - 变化事件
/// </summary>
public class RuntimeAttribute
{
    #region 核心数据
    
    /// <summary>
    /// 属性ID
    /// </summary>
    public string AttributeID { get; private set; }
    
    /// <summary>
    /// 当前值
    /// </summary>
    private float currentValue;
    
    /// <summary>
    /// 最小值
    /// </summary>
    public float MinValue { get; private set; }
    
    /// <summary>
    /// 最大值
    /// </summary>
    public float MaxValue { get; private set; }
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建运行时属性资源
    /// </summary>
    /// <param name="attributeID">属性ID</param>
    /// <param name="minValue">最小值</param>
    /// <param name="maxValue">最大值</param>
    /// <param name="startValue">初始值</param>
    public RuntimeAttribute(string attributeID, float minValue, float maxValue, float startValue)
    {
        this.AttributeID = attributeID;
        this.MinValue = minValue;
        this.MaxValue = maxValue;
        this.currentValue = Mathf.Clamp(startValue, minValue, maxValue);
    }
    
    /// <summary>
    /// 从配置创建
    /// </summary>
    public static RuntimeAttribute FromData(AttributeData data)
    {
        return new RuntimeAttribute(
            data.attributeID,
            data.minValue,
            data.maxValue,
            data.GetStartValue()
        );
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 当前值（自动限制范围）
    /// </summary>
    public float CurrentValue
    {
        get => currentValue;
        set
        {
            float oldValue = currentValue;
            currentValue = Mathf.Clamp(value, MinValue, MaxValue);
            
            // 值变化时触发事件
            if (!Mathf.Approximately(oldValue, currentValue))
            {
                OnValueChanged?.Invoke(oldValue, currentValue);
            }
        }
    }
    
    /// <summary>
    /// 百分比（当前值/最大值）
    /// </summary>
    public float Ratio => MaxValue > 0 ? currentValue / MaxValue : 0f;
    
    /// <summary>
    /// 是否已满
    /// </summary>
    public bool IsFull => Mathf.Approximately(currentValue, MaxValue);
    
    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => Mathf.Approximately(currentValue, MinValue);
    
    #endregion
    
    #region 值修改方法
    
    /// <summary>
    /// 增加值
    /// </summary>
    public float Add(float amount)
    {
        CurrentValue += amount;
        return currentValue;
    }
    
    /// <summary>
    /// 减少值
    /// </summary>
    public float Subtract(float amount)
    {
        CurrentValue -= amount;
        return currentValue;
    }
    
    /// <summary>
    /// 设置为满值
    /// </summary>
    public void SetToMax()
    {
        CurrentValue = MaxValue;
    }
    
    /// <summary>
    /// 设置为最小值
    /// </summary>
    public void SetToMin()
    {
        CurrentValue = MinValue;
    }
    
    /// <summary>
    /// 设置百分比
    /// </summary>
    public void SetPercent(float percent)
    {
        CurrentValue = Mathf.Lerp(MinValue, MaxValue, Mathf.Clamp01(percent));
    }
    
    #endregion
    
    #region 范围管理
    
    /// <summary>
    /// 更新最大值（会自动调整当前值）
    /// </summary>
    public void SetMaxValue(float newMaxValue)
    {
        MaxValue = Mathf.Max(MinValue, newMaxValue);
        CurrentValue = Mathf.Clamp(currentValue, MinValue, MaxValue);
    }
    
    /// <summary>
    /// 更新最小值（会自动调整当前值）
    /// </summary>
    public void SetMinValue(float newMinValue)
    {
        MinValue = Mathf.Min(newMinValue, MaxValue);
        CurrentValue = Mathf.Clamp(currentValue, MinValue, MaxValue);
    }
    
    #endregion
    
    #region 事件
    
    /// <summary>
    /// 值变化事件
    /// </summary>
    public event System.Action<float, float> OnValueChanged;
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{AttributeID}] {CurrentValue:F1}/{MaxValue:F1} ({Ratio * 100:F0}%)";
    }
    
    #endregion
}

