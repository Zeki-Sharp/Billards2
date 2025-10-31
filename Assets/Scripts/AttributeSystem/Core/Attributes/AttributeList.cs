using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 属性资源列表 - 配置一组 Attributes
/// 
/// 【设计理念】：
/// - 用于 ScriptableObject 配置
/// - 定义一组动态资源的模板
/// - 可用于 PlayerClass、EnemyClass 等配置
/// </summary>
[System.Serializable]
public class AttributeList
{
    [Tooltip("属性资源列表")]
    public List<AttributeData> attributes = new List<AttributeData>();
    
    /// <summary>
    /// 根据 ID 获取属性资源数据
    /// </summary>
    public AttributeData GetAttribute(string attributeID)
    {
        return attributes.FirstOrDefault(a => a.attributeID == attributeID && a.isEnabled);
    }
    
    /// <summary>
    /// 检查是否包含指定属性资源
    /// </summary>
    public bool HasAttribute(string attributeID)
    {
        return GetAttribute(attributeID) != null;
    }
    
    /// <summary>
    /// 获取所有有效的属性资源
    /// </summary>
    public List<AttributeData> GetAllValidAttributes()
    {
        return attributes.Where(a => a.IsValid()).ToList();
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (attributes == null || attributes.Count == 0)
        {
            return "AttributeList: 空列表";
        }
        
        string info = $"AttributeList: {attributes.Count} 个属性资源\n";
        foreach (var attr in attributes)
        {
            if (attr.IsValid())
            {
                info += $"  {attr.GetDebugInfo()}\n";
            }
        }
        return info;
    }
}

