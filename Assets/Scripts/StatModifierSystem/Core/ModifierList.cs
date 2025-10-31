using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 修改器列表管理类 - 高效管理单个属性的所有修改器
/// 
/// 【设计理念】：
/// - 维护 Modifier 列表
/// - 缓存总值，O(1) 访问
/// - Add/Remove 时自动更新缓存
/// - 参考 GC2 的 ModifierList 设计
/// 
/// 【职责】：
/// - 管理单个属性的所有修改器
/// - 提供快速的总值访问
/// - 自动维护缓存一致性
/// </summary>
public class ModifierList
{
    #region 私有字段
    
    /// <summary>
    /// 修改器列表
    /// </summary>
    private List<Modifier> modifiers = new List<Modifier>();
    
    /// <summary>
    /// 缓存的总值
    /// </summary>
    private float cachedTotal = 0f;
    
    /// <summary>
    /// 缓存是否有效
    /// </summary>
    private bool isCacheDirty = false;
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 修改器数量
    /// </summary>
    public int Count => modifiers.Count;
    
    /// <summary>
    /// 总修改值（O(1) 访问）
    /// </summary>
    public float Total
    {
        get
        {
            if (isCacheDirty)
            {
                RecalculateTotal();
            }
            return cachedTotal;
        }
    }
    
    #endregion
    
    #region 修改器管理
    
    /// <summary>
    /// 添加修改器
    /// </summary>
    public void Add(Modifier modifier)
    {
        modifiers.Add(modifier);
        cachedTotal += modifier.Value;
        // 不需要标记脏，直接更新了缓存
    }
    
    /// <summary>
    /// 移除修改器
    /// </summary>
    public bool Remove(Modifier modifier)
    {
        int index = FindModifierIndex(modifier);
        if (index >= 0)
        {
            modifiers.RemoveAt(index);
            cachedTotal -= modifier.Value;
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 移除指定索引的修改器
    /// </summary>
    public bool RemoveAt(int index)
    {
        if (index >= 0 && index < modifiers.Count)
        {
            cachedTotal -= modifiers[index].Value;
            modifiers.RemoveAt(index);
            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 清空所有修改器
    /// </summary>
    public void Clear()
    {
        modifiers.Clear();
        cachedTotal = 0f;
        isCacheDirty = false;
    }
    
    #endregion
    
    #region 查询方法
    
    /// <summary>
    /// 检查是否包含修改器
    /// </summary>
    public bool Contains(Modifier modifier)
    {
        return FindModifierIndex(modifier) >= 0;
    }
    
    /// <summary>
    /// 获取所有修改器（只读）
    /// </summary>
    public IReadOnlyList<Modifier> GetAll()
    {
        return modifiers.AsReadOnly();
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 查找修改器索引
    /// </summary>
    private int FindModifierIndex(Modifier modifier)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            // struct 比较需要比较字段值
            if (modifiers[i].StatID == modifier.StatID && 
                Mathf.Approximately(modifiers[i].Value, modifier.Value))
            {
                return i;
            }
        }
        return -1;
    }
    
    /// <summary>
    /// 重新计算总值
    /// </summary>
    private void RecalculateTotal()
    {
        cachedTotal = 0f;
        foreach (var modifier in modifiers)
        {
            cachedTotal += modifier.Value;
        }
        isCacheDirty = false;
    }
    
    #endregion
    
    #region 调试
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (modifiers.Count == 0)
        {
            return "空列表";
        }
        
        string info = $"共 {modifiers.Count} 个修改器，总值: {Total}\n";
        foreach (var modifier in modifiers)
        {
            info += $"  - {modifier.GetDebugInfo()}\n";
        }
        return info;
    }
    
    #endregion
}

