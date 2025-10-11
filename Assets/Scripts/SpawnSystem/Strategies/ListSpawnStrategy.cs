using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 列表生成策略 - 从预设列表中获取生成内容
/// 
/// 【核心功能】：
/// - 提前配置好完整的生成列表
/// - 每次调用返回这个预设列表的副本
/// - 适合需要精确控制每波次内容的场景
/// 
/// 【适用场景】：
/// - 敌人波次生成（Wave1: 敌人A x3, 敌人B x2）
/// - 道具池随机选择
/// - 预设的奖励列表
/// </summary>
/// <typeparam name="T">生成对象的数据类型</typeparam>
[System.Serializable]
public class ListSpawnStrategy<T> : ISpawnStrategy<T>
{
    [Header("列表配置")]
    [Tooltip("预设的生成对象列表")]
    public List<T> spawnList = new List<T>();
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的数据列表（副本）</returns>
    public List<T> GetSpawnList()
    {
        if (spawnList == null)
        {
            Debug.LogError("[ListSpawnStrategy] spawnList为空！");
            return new List<T>();
        }
        
        // 返回副本，避免外部修改影响原列表
        List<T> result = new List<T>(spawnList);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ListSpawnStrategy] 返回生成列表，数量: {result.Count}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    public int GetSpawnCount()
    {
        if (spawnList == null)
        {
            return 0;
        }
        
        return spawnList.Count;
    }
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfig()
    {
        if (spawnList == null)
        {
            Debug.LogError("[ListSpawnStrategy] spawnList未初始化");
            return false;
        }
        
        if (spawnList.Count == 0)
        {
            Debug.LogWarning("[ListSpawnStrategy] spawnList为空列表");
            return false;
        }
        
        // 检查列表中是否有空元素
        for (int i = 0; i < spawnList.Count; i++)
        {
            if (spawnList[i] == null)
            {
                Debug.LogError($"[ListSpawnStrategy] spawnList[{i}]为空元素");
                return false;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ListSpawnStrategy] 配置验证通过，列表大小: {spawnList.Count}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置生成列表
    /// </summary>
    /// <param name="newList">新的生成列表</param>
    public void SetSpawnList(List<T> newList)
    {
        spawnList = newList != null ? new List<T>(newList) : new List<T>();
        
        if (enableDebugLog)
        {
            Debug.Log($"[ListSpawnStrategy] 设置生成列表，数量: {spawnList.Count}");
        }
    }
    
    /// <summary>
    /// 添加生成对象
    /// </summary>
    /// <param name="item">要添加的对象</param>
    public void AddSpawnItem(T item)
    {
        if (spawnList == null)
        {
            spawnList = new List<T>();
        }
        
        spawnList.Add(item);
        
        if (enableDebugLog)
        {
            Debug.Log($"[ListSpawnStrategy] 添加生成对象，当前数量: {spawnList.Count}");
        }
    }
    
    /// <summary>
    /// 清空生成列表
    /// </summary>
    public void ClearSpawnList()
    {
        if (spawnList != null)
        {
            spawnList.Clear();
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[ListSpawnStrategy] 清空生成列表");
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        if (spawnList == null)
        {
            return "ListSpawnStrategy: spawnList未初始化";
        }
        
        return $"ListSpawnStrategy: 列表大小={spawnList.Count}, 空元素={spawnList.FindAll(x => x == null).Count}";
    }
}
