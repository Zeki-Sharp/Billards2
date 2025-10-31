using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 属性列表 - 配置一组 Stats
/// 
/// 【设计理念】：
/// - 用于 ScriptableObject 配置
/// - 定义一组属性的模板
/// - 可用于 PlayerClass、EnemyClass 等配置
/// 
/// 【参考 GC2】：
/// - 类似 GC2 的 StatList
/// </summary>
[System.Serializable]
public class StatList
{
    [Tooltip("属性列表")]
    public List<StatData> stats = new List<StatData>();
    
    /// <summary>
    /// 根据 ID 获取属性数据
    /// </summary>
    public StatData GetStat(string statID)
    {
        return stats.FirstOrDefault(s => s.statID == statID && s.isEnabled);
    }
    
    /// <summary>
    /// 检查是否包含指定属性
    /// </summary>
    public bool HasStat(string statID)
    {
        return GetStat(statID) != null;
    }
    
    /// <summary>
    /// 获取所有有效的属性
    /// </summary>
    public List<StatData> GetAllValidStats()
    {
        return stats.Where(s => s.IsValid()).ToList();
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (stats == null || stats.Count == 0)
        {
            return "StatList: 空列表";
        }
        
        string info = $"StatList: {stats.Count} 个属性\n";
        foreach (var stat in stats)
        {
            if (stat.IsValid())
            {
                info += $"  {stat.GetDebugInfo()}\n";
            }
        }
        return info;
    }
}

