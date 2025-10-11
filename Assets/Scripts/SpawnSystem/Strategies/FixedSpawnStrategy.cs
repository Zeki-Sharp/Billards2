using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 固定生成策略 - 生成固定种类和数量的对象
/// 
/// 【核心功能】：
/// - 配置固定的"种类"和"数量"
/// - 每次调用生成N个相同的对象
/// - 适合重复性、固定规则的生成
/// 
/// 【适用场景】：
/// - 技能每回合生成固定道具
/// - 定时刷新的固定内容
/// - 事件触发的固定奖励
/// - 每回合生成2个治疗药水
/// </summary>
/// <typeparam name="T">生成对象的数据类型</typeparam>
[System.Serializable]
public class FixedSpawnStrategy<T> : ISpawnStrategy<T>
{
    [Header("固定配置")]
    [Tooltip("要生成的对象类型")]
    public T itemToSpawn;
    
    [Tooltip("每次生成的数量")]
    [MinValue(1)]
    public int spawnCount = 2;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的数据列表</returns>
    public List<T> GetSpawnList()
    {
        if (itemToSpawn == null)
        {
            Debug.LogError("[FixedSpawnStrategy] itemToSpawn为空！");
            return new List<T>();
        }
        
        if (spawnCount <= 0)
        {
            Debug.LogError($"[FixedSpawnStrategy] spawnCount无效: {spawnCount}");
            return new List<T>();
        }
        
        List<T> result = new List<T>();
        
        // 生成指定数量的相同对象
        for (int i = 0; i < spawnCount; i++)
        {
            result.Add(itemToSpawn);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[FixedSpawnStrategy] 生成固定对象，数量: {result.Count}");
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    public int GetSpawnCount()
    {
        return spawnCount;
    }
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfig()
    {
        if (itemToSpawn == null)
        {
            Debug.LogError("[FixedSpawnStrategy] itemToSpawn未设置");
            return false;
        }
        
        if (spawnCount <= 0)
        {
            Debug.LogError($"[FixedSpawnStrategy] spawnCount无效: {spawnCount}，必须大于0");
            return false;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[FixedSpawnStrategy] 配置验证通过，生成数量: {spawnCount}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置生成对象类型
    /// </summary>
    /// <param name="item">要生成的对象</param>
    public void SetItemToSpawn(T item)
    {
        itemToSpawn = item;
        
        if (enableDebugLog)
        {
            Debug.Log($"[FixedSpawnStrategy] 设置生成对象类型");
        }
    }
    
    /// <summary>
    /// 设置生成数量
    /// </summary>
    /// <param name="count">生成数量</param>
    public void SetSpawnCount(int count)
    {
        if (count <= 0)
        {
            Debug.LogError($"[FixedSpawnStrategy] 无效的生成数量: {count}");
            return;
        }
        
        spawnCount = count;
        
        if (enableDebugLog)
        {
            Debug.Log($"[FixedSpawnStrategy] 设置生成数量: {spawnCount}");
        }
    }
    
    /// <summary>
    /// 重置策略配置
    /// </summary>
    public void Reset()
    {
        itemToSpawn = default(T);
        spawnCount = 2;
        
        if (enableDebugLog)
        {
            Debug.Log("[FixedSpawnStrategy] 重置策略配置");
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string itemInfo = itemToSpawn != null ? itemToSpawn.ToString() : "null";
        return $"FixedSpawnStrategy: 对象={itemInfo}, 数量={spawnCount}";
    }
}
