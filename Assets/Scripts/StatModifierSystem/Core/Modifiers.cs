using UnityEngine;

/// <summary>
/// 修改器容器 - 管理单个属性的所有类型修改器
/// 
/// 【设计理念】：
/// - 分别管理 Constant（固定值）和 Percent（百分比）修改器
/// - 提供标准的计算公式：(base + constant) * (1 + percent)
/// - 参考 GC2 的 Modifiers 设计
/// 
/// 【职责】：
/// - 组合 Constant 和 Percent 两个 ModifierList
/// - 提供统一的最终值计算接口
/// - 管理单个属性的完整修改器体系
/// </summary>
public class Modifiers
{
    #region 私有字段
    
    /// <summary>
    /// 固定值修改器列表
    /// </summary>
    private ModifierList constantModifiers = new ModifierList();
    
    /// <summary>
    /// 百分比修改器列表
    /// </summary>
    private ModifierList percentModifiers = new ModifierList();
    
    /// <summary>
    /// 属性ID
    /// </summary>
    private string statID;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建修改器容器
    /// </summary>
    /// <param name="statID">属性ID</param>
    public Modifiers(string statID)
    {
        this.statID = statID;
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 固定值修改总和
    /// </summary>
    public float ConstantTotal => constantModifiers.Total;
    
    /// <summary>
    /// 百分比修改总和
    /// </summary>
    public float PercentTotal => percentModifiers.Total;
    
    /// <summary>
    /// 总修改器数量
    /// </summary>
    public int TotalCount => constantModifiers.Count + percentModifiers.Count;
    
    #endregion
    
    #region 修改器管理
    
    /// <summary>
    /// 添加固定值修改器
    /// </summary>
    public void AddConstant(Modifier modifier)
    {
        constantModifiers.Add(modifier);
    }
    
    /// <summary>
    /// 添加百分比修改器
    /// </summary>
    public void AddPercent(Modifier modifier)
    {
        percentModifiers.Add(modifier);
    }
    
    /// <summary>
    /// 移除固定值修改器
    /// </summary>
    public bool RemoveConstant(Modifier modifier)
    {
        return constantModifiers.Remove(modifier);
    }
    
    /// <summary>
    /// 移除百分比修改器
    /// </summary>
    public bool RemovePercent(Modifier modifier)
    {
        return percentModifiers.Remove(modifier);
    }
    
    /// <summary>
    /// 清空所有修改器
    /// </summary>
    public void Clear()
    {
        constantModifiers.Clear();
        percentModifiers.Clear();
    }
    
    #endregion
    
    #region 最终值计算
    
    /// <summary>
    /// 计算最终值
    /// 公式：(baseValue + constantTotal) * (1 + percentTotal)
    /// </summary>
    /// <param name="baseValue">基础值</param>
    /// <returns>最终值</returns>
    public float CalculateFinalValue(float baseValue)
    {
        // GC2 标准公式：(base + constant) * (1 + percent)
        float finalValue = (baseValue + constantModifiers.Total) * (1f + percentModifiers.Total);
        return finalValue;
    }
    
    #endregion
    
    #region 查询方法
    
    /// <summary>
    /// 获取所有固定值修改器（只读）
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Modifier> GetConstantModifiers()
    {
        return constantModifiers.GetAll();
    }
    
    /// <summary>
    /// 获取所有百分比修改器（只读）
    /// </summary>
    public System.Collections.Generic.IReadOnlyList<Modifier> GetPercentModifiers()
    {
        return percentModifiers.GetAll();
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (TotalCount == 0)
        {
            return $"[{statID}] 无修改器";
        }
        
        string info = $"[{statID}] 修改器统计:\n";
        info += $"  固定值: {constantModifiers.Count} 个, 总计: {ConstantTotal:F2}\n";
        info += $"  百分比: {percentModifiers.Count} 个, 总计: {(PercentTotal * 100f):F1}%\n";
        
        if (constantModifiers.Count > 0)
        {
            info += "  固定值详情:\n";
            foreach (var modifier in constantModifiers.GetAll())
            {
                info += $"    {modifier.GetDebugInfo()}\n";
            }
        }
        
        if (percentModifiers.Count > 0)
        {
            info += "  百分比详情:\n";
            foreach (var modifier in percentModifiers.GetAll())
            {
                info += $"    {modifier.GetDebugInfo()}\n";
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 获取简化的调试信息
    /// </summary>
    public string GetSimpleDebugInfo()
    {
        return $"[{statID}] Constant: {ConstantTotal:F2}, Percent: {(PercentTotal * 100f):F1}%";
    }
    
    #endregion
}

