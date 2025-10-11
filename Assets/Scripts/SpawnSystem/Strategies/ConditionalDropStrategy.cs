using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 条件掉落策略 - 基于条件动态决定生成内容
/// 
/// 【核心功能】：
/// - 配置掉落表（支持条件判定）
/// - 检查技能条件（如需要特定技能才能掉落）
/// - 保持现有概率机制（可配置为100%必掉）
/// - 适合基于条件的掉落生成
/// 
/// 【适用场景】：
/// - 击杀掉落
/// - 条件触发的道具生成
/// - 技能解锁的特殊掉落
/// 
/// 【设计说明】：
/// - 这是现有DropTableProvider逻辑的策略化封装
/// - 保持现有的概率机制和技能检查逻辑
/// - 重点在于"条件判定"而非"概率"
/// </summary>
/// <typeparam name="T">生成对象的数据类型（通常是ItemConfig）</typeparam>
[System.Serializable]
public class ConditionalDropStrategy<T> : ISpawnStrategy<T>
{
    [Header("掉落配置")]
    [Tooltip("掉落表配置")]
    public DropTableConfig dropTable;
    
    [Tooltip("当前激活的技能名称集合")]
    public HashSet<string> activeSkills;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的数据列表</returns>
    public List<T> GetSpawnList()
    {
        if (dropTable == null)
        {
            Debug.LogError("[ConditionalDropStrategy] dropTable为空！");
            return new List<T>();
        }
        
        List<T> result = new List<T>();
        
        // 限制最大掉落数量
        int maxDrops = dropTable.maxDropCount;
        
        // 按权重排序掉落条目
        var sortedEntries = new List<ItemDropEntry>(dropTable.dropEntries);
        sortedEntries.Sort((a, b) => b.weight.CompareTo(a.weight));
        
        // 执行概率抽取
        foreach (var entry in sortedEntries)
        {
            if (result.Count >= maxDrops)
            {
                break;
            }
            
            if (entry.itemConfig == null)
            {
                Debug.LogWarning("[ConditionalDropStrategy] 掉落条目中的道具配置为空，跳过");
                continue;
            }
            
            // 检查掉落条件
            if (!entry.CheckDropCondition(activeSkills))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[ConditionalDropStrategy] 掉落条件不满足，跳过: {entry.itemConfig.itemName} (需要技能: {entry.requiredSkillName})");
                }
                continue;
            }
            
            // 概率判定（保持现有逻辑）
            if (Random.Range(0f, 1f) <= entry.dropChance)
            {
                // 将ItemConfig转换为T类型
                T item = ConvertToT(entry.itemConfig);
                if (item != null)
                {
                    result.Add(item);
                    
                    if (enableDebugLog)
                    {
                        string conditionInfo = entry.conditionType == DropConditionType.SkillRequired ? 
                            $" (技能条件: {entry.requiredSkillName})" : "";
                        Debug.Log($"[ConditionalDropStrategy] 掉落道具: {entry.itemConfig.itemName} (概率: {entry.dropChance:P1}){conditionInfo}");
                    }
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ConditionalDropStrategy] 掉落结果: {result.Count} 个道具");
        }
        
        return result;
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    public int GetSpawnCount()
    {
        return dropTable?.maxDropCount ?? 0;
    }
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfig()
    {
        if (dropTable == null)
        {
            Debug.LogError("[ConditionalDropStrategy] dropTable未设置");
            return false;
        }
        
        if (dropTable.dropEntries == null || dropTable.dropEntries.Count == 0)
        {
            Debug.LogWarning("[ConditionalDropStrategy] 掉落表没有配置掉落条目");
            return false;
        }
        
        // 验证掉落条目
        foreach (var entry in dropTable.dropEntries)
        {
            if (entry.itemConfig == null)
            {
                Debug.LogError("[ConditionalDropStrategy] 掉落表中有空的道具配置");
                return false;
            }
            
            if (entry.dropChance < 0f || entry.dropChance > 1f)
            {
                Debug.LogError($"[ConditionalDropStrategy] 无效的概率值: {entry.dropChance}");
                return false;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[ConditionalDropStrategy] 配置验证通过，掉落条目数: {dropTable.dropEntries.Count}");
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置掉落表
    /// </summary>
    /// <param name="table">掉落表配置</param>
    public void SetDropTable(DropTableConfig table)
    {
        dropTable = table;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ConditionalDropStrategy] 设置掉落表: {dropTable?.enemyType}");
        }
    }
    
    /// <summary>
    /// 设置激活的技能
    /// </summary>
    /// <param name="skills">技能名称集合</param>
    public void SetActiveSkills(HashSet<string> skills)
    {
        activeSkills = skills;
        
        if (enableDebugLog)
        {
            Debug.Log($"[ConditionalDropStrategy] 设置激活技能: {activeSkills?.Count ?? 0} 个");
        }
    }
    
    /// <summary>
    /// 清空激活技能
    /// </summary>
    public void ClearActiveSkills()
    {
        activeSkills?.Clear();
        
        if (enableDebugLog)
        {
            Debug.Log("[ConditionalDropStrategy] 清空激活技能");
        }
    }
    
    /// <summary>
    /// 将ItemConfig转换为T类型
    /// 这是一个泛型转换方法，需要根据实际使用情况进行实现
    /// </summary>
    /// <param name="itemConfig">道具配置</param>
    /// <returns>转换后的对象</returns>
    private T ConvertToT(ItemConfig itemConfig)
    {
        // 如果T就是ItemConfig类型，直接返回
        if (typeof(T) == typeof(ItemConfig))
        {
            return (T)(object)itemConfig;
        }
        
        // 其他类型转换可以根据需要扩展
        // 这里提供一个基础的实现
        try
        {
            return (T)(object)itemConfig;
        }
        catch (System.InvalidCastException)
        {
            Debug.LogError($"[ConditionalDropStrategy] 无法将ItemConfig转换为{typeof(T).Name}");
            return default(T);
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        if (dropTable == null)
        {
            return "ConditionalDropStrategy: dropTable未设置";
        }
        
        return $"ConditionalDropStrategy: 敌人类型={dropTable.enemyType}, 掉落条目={dropTable.dropEntries.Count}, 激活技能={activeSkills?.Count ?? 0}";
    }
}
