using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Blackboard（黑板）系统 - 共享数据存储
/// 
/// 【核心职责】：
/// - 提供键值对存储，行为间共享数据
/// - 支持类型安全的 Get/Set 操作
/// - 用于状态共享（如 isDashing、lastPlayerPosition）
/// 
/// 【设计原则】：
/// - 基础设施层，不依赖其他系统
/// - 轻量级实现，无性能开销
/// - 类型安全，避免装箱拆箱
/// 
/// 【参考】：GC2 Beliefs 系统
/// </summary>
public class Blackboard
{
    #region 数据存储
    
    private Dictionary<string, object> data = new Dictionary<string, object>();
    
    #endregion
    
    #region 基础操作
    
    /// <summary>
    /// 设置键值
    /// </summary>
    public void Set<T>(string key, T value)
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogWarning("[Blackboard] 尝试设置空键");
            return;
        }
        
        data[key] = value;
    }
    
    /// <summary>
    /// 获取键值
    /// </summary>
    public T Get<T>(string key, bool suppressWarning = false)
    {
        if (string.IsNullOrEmpty(key))
        {
            if (!suppressWarning)
            {
                Debug.LogWarning("[Blackboard] 尝试获取空键");
            }
            return default(T);
        }
        
        if (data.TryGetValue(key, out object value))
        {
            try
            {
                return (T)value;
            }
            catch (InvalidCastException)
            {
                if (!suppressWarning)
                {
                    Debug.LogWarning($"[Blackboard] 类型转换失败：键 '{key}' 期望类型 {typeof(T).Name}，实际类型 {value?.GetType().Name}");
                }
                return default(T);
            }
        }
        
        return default(T);
    }
    
    /// <summary>
    /// 尝试获取键值
    /// </summary>
    public bool TryGet<T>(string key, out T value, bool suppressWarning = false)
    {
        value = default(T);
        
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }
        
        if (data.TryGetValue(key, out object objValue))
        {
            try
            {
                value = (T)objValue;
                return true;
            }
            catch (InvalidCastException)
            {
                if (!suppressWarning)
                {
                    Debug.LogWarning($"[Blackboard] 类型转换失败：键 '{key}' 期望类型 {typeof(T).Name}，实际类型 {objValue?.GetType().Name}");
                }
                return false;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查键是否存在
    /// </summary>
    public bool ContainsKey(string key)
    {
        return !string.IsNullOrEmpty(key) && data.ContainsKey(key);
    }
    
    /// <summary>
    /// 移除键
    /// </summary>
    public bool Remove(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }
        
        return data.Remove(key);
    }
    
    /// <summary>
    /// 清空所有数据
    /// </summary>
    public void Clear()
    {
        data.Clear();
    }
    
    #endregion
    
    #region 调试和工具
    
    /// <summary>
    /// 获取所有键
    /// </summary>
    public IEnumerable<string> GetAllKeys()
    {
        return data.Keys;
    }
    
    /// <summary>
    /// 获取数据数量
    /// </summary>
    public int Count => data.Count;
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        if (data.Count == 0)
        {
            return "[Blackboard] 空";
        }
        
        string info = $"[Blackboard] 数据项数量: {data.Count}\n";
        foreach (var kvp in data)
        {
            info += $"  - {kvp.Key}: {kvp.Value} ({kvp.Value?.GetType().Name ?? "null"})\n";
        }
        
        return info;
    }
    
    #endregion
}

