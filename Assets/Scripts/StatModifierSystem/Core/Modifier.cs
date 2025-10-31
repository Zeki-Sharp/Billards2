using UnityEngine;

/// <summary>
/// 轻量级属性修改器 - 极简数据结构
/// 
/// 【设计理念】：
/// - 使用 struct 减少 GC 压力
/// - 只包含核心数据（StatID + Value）
/// - 生命周期管理由外部系统负责
/// - 参考 GC2 的 Modifier 设计
/// 
/// 【职责】：
/// - 纯数据，不包含逻辑
/// - 表示"对某个属性修改多少"
/// - 不关心来源、时间、条件等
/// </summary>
[System.Serializable]
public struct Modifier
{
    #region 核心数据
    
    /// <summary>
    /// 目标属性ID（例如："MaxHealth", "Damage", "MoveSpeed"）
    /// </summary>
    public string StatID;
    
    /// <summary>
    /// 修改值
    /// </summary>
    public float Value;
    
    #endregion
    
    #region 构造函数
    
    /// <summary>
    /// 创建修改器
    /// </summary>
    /// <param name="statID">属性ID</param>
    /// <param name="value">修改值</param>
    public Modifier(string statID, float value)
    {
        this.StatID = statID;
        this.Value = value;
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 克隆修改器
    /// </summary>
    public Modifier Clone()
    {
        return new Modifier(StatID, Value);
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[{StatID}] {(Value >= 0 ? "+" : "")}{Value}";
    }
    
    #endregion
}

