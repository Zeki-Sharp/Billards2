using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 死亡掉落策略 - 专门为DeathDropTrigger设计
/// 
/// 【核心功能】：
/// - 从DropTableProvider获取掉落表
/// - 检查技能条件
/// - 执行概率判定
/// - 返回符合条件的掉落道具
/// 
/// 【适用场景】：
/// - 敌人死亡掉落
/// - 基于条件的道具生成
/// </summary>
[System.Serializable]
public class DeathDropStrategy : ISpawnStrategy<ItemConfig>
{
    [Header("掉落配置")]
    [Tooltip("掉落表配置提供者")]
    public DropTableProvider dropTableProvider;
    
    [Tooltip("当前激活的技能名称集合")]
    public HashSet<string> activeSkills;
    
    [Header("调试")]
    [Tooltip("是否启用调试日志")]
    public bool enableDebugLog = false;
    
    // 内部状态
    private List<ItemConfig> currentDropList = new List<ItemConfig>();
    private EnemyType currentEnemyType = EnemyType.Normal;
    
    /// <summary>
    /// 获取要生成的对象列表
    /// </summary>
    /// <returns>生成对象的数据列表</returns>
    public List<ItemConfig> GetSpawnList()
    {
        currentDropList.Clear();
        
        if (dropTableProvider == null)
        {
            Debug.LogError("[DeathDropStrategy] dropTableProvider为空！");
            return new List<ItemConfig>();
        }
        
        // 获取掉落表
        var dropTable = dropTableProvider.GetDropTable(currentEnemyType);
        if (dropTable == null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropStrategy] 未找到敌人类型 {currentEnemyType} 的掉落表");
            }
            return new List<ItemConfig>();
        }
        
        // 执行概率抽取（考虑技能条件）
        currentDropList = dropTableProvider.GetItemsToDrop(dropTable, activeSkills);
        
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropStrategy] 掉落抽取完成，数量: {currentDropList.Count}");
        }
        
        return new List<ItemConfig>(currentDropList);
    }
    
    /// <summary>
    /// 获取生成数量
    /// </summary>
    /// <returns>本次生成的对象总数</returns>
    public int GetSpawnCount()
    {
        return currentDropList.Count;
    }
    
    /// <summary>
    /// 验证策略配置是否有效
    /// </summary>
    /// <returns>配置是否有效</returns>
    public bool ValidateConfig()
    {
        if (dropTableProvider == null)
        {
            Debug.LogError("[DeathDropStrategy] dropTableProvider 未设置！");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 设置当前敌人类型
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    public void SetEnemyType(EnemyType enemyType)
    {
        currentEnemyType = enemyType;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropStrategy] 设置敌人类型: {enemyType}");
        }
    }
    
    /// <summary>
    /// 设置激活技能
    /// </summary>
    /// <param name="skills">激活的技能集合</param>
    public void SetActiveSkills(HashSet<string> skills)
    {
        activeSkills = skills;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropStrategy] 设置激活技能: {(skills != null ? string.Join(", ", skills) : "无")}");
        }
    }
    
    /// <summary>
    /// 更新激活技能（从SkillStateManager获取）
    /// </summary>
    /// <param name="skillStateManager">技能状态管理器</param>
    public void UpdateActiveSkills(SkillStateManager skillStateManager)
    {
        if (skillStateManager != null)
        {
            activeSkills = skillStateManager.GetActiveSkills();
            
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropStrategy] 更新激活技能: {(activeSkills != null ? string.Join(", ", activeSkills) : "无")}");
            }
        }
    }
    
    /// <summary>
    /// 重置策略状态
    /// </summary>
    public void ResetState()
    {
        currentDropList.Clear();
        currentEnemyType = EnemyType.Normal;
        activeSkills = null;
        
        if (enableDebugLog)
        {
            Debug.Log("[DeathDropStrategy] 状态已重置");
        }
    }
}
