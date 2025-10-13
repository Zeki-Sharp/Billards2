using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合掉落类型枚举
/// </summary>
public enum TurnDropType
{
    PlayerTurnStart,  // 玩家回合开始
    EnemyTurnStart,   // 敌人回合开始
    // 其他未来类型可以在这里添加...
}

/// <summary>
/// 回合掉落表配置提供者 - 管理回合触发时道具生成的配置
/// 与DropTableProvider结构完全一致，但用于回合触发场景
/// </summary>
[CreateAssetMenu(fileName = "TurnDropTableProvider", menuName = "Game/Turn Drop Table Provider")]
public class TurnDropTableProvider : ScriptableObject
{
    [Header("回合掉落表配置")]
    [LabelText("回合掉落表列表")]
    [Tooltip("不同回合类型的掉落表配置")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "turnDropType")]
    public List<TurnDropTableConfig> turnDropTables = new List<TurnDropTableConfig>();
    
    [Header("全局设置")]
    [LabelText("全局掉落率倍数")]
    [Tooltip("所有掉落的概率都会乘以这个倍数")]
    [Range(0f, 2f)]
    public float globalDropRateMultiplier = 1f;
    
    [LabelText("最大掉落数量")]
    [Tooltip("单次回合掉落的最大道具数量")]
    [MinValue(1)]
    public int maxDropCount = 5;
    
    [Header("调试设置")]
    [LabelText("启用调试日志")]
    public bool enableDebugLog = true;
    
    /// <summary>
    /// 获取指定回合类型的掉落表
    /// </summary>
    /// <param name="turnDropType">回合掉落类型</param>
    /// <returns>回合掉落表配置，如果未找到返回null</returns>
    public TurnDropTableConfig GetTurnDropTable(TurnDropType turnDropType)
    {
        foreach (var dropTable in turnDropTables)
        {
            if (dropTable.turnDropType == turnDropType)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[TurnDropTableProvider] 找到回合掉落表: {turnDropType}");
                }
                return dropTable;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.LogWarning($"[TurnDropTableProvider] 未找到回合类型 {turnDropType} 的掉落表");
        }
        return null;
    }
    
    /// <summary>
    /// 根据回合掉落表获取掉落物品列表
    /// </summary>
    /// <param name="turnDropTable">回合掉落表配置</param>
    /// <param name="activeSkills">当前激活的技能名称集合（可选）</param>
    /// <returns>掉落物品列表</returns>
    public List<ItemConfig> GetItemsToDrop(TurnDropTableConfig turnDropTable, HashSet<string> activeSkills = null)
    {
        if (turnDropTable == null)
        {
            Debug.LogError("[TurnDropTableProvider] 回合掉落表配置为空");
            return new List<ItemConfig>();
        }
        
        List<ItemConfig> itemsToDrop = new List<ItemConfig>();
        
        // 限制最大掉落数量
        int maxDrops = Mathf.Min(turnDropTable.maxDropCount, maxDropCount);
        
        // 按权重排序掉落条目
        var sortedEntries = new List<ItemDropEntry>(turnDropTable.dropEntries);
        sortedEntries.Sort((a, b) => b.weight.CompareTo(a.weight));
        
        // 执行概率抽取（与DropTableProvider逻辑完全一致）
        foreach (var entry in sortedEntries)
        {
            if (itemsToDrop.Count >= maxDrops)
            {
                break;
            }
            
            if (entry.itemConfig == null)
            {
                Debug.LogWarning("[TurnDropTableProvider] 掉落条目中的道具配置为空，跳过");
                continue;
            }
            
            // 检查掉落条件（复用DropTableProvider的逻辑）
            if (!entry.CheckDropCondition(activeSkills))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[TurnDropTableProvider] 掉落条件不满足，跳过: {entry.itemConfig.itemName} (需要技能: {entry.requiredSkillName})");
                }
                continue;
            }
            
            // 应用全局掉落率倍数
            float finalDropChance = entry.dropChance * globalDropRateMultiplier;
            
            if (Random.Range(0f, 1f) <= finalDropChance)
            {
                itemsToDrop.Add(entry.itemConfig);
                
                if (enableDebugLog)
                {
                    string conditionInfo = entry.conditionType == DropConditionType.SkillRequired ? 
                        $" (技能条件: {entry.requiredSkillName})" : "";
                    Debug.Log($"[TurnDropTableProvider] 掉落道具: {entry.itemConfig.itemName} (概率: {finalDropChance:P1}){conditionInfo}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[TurnDropTableProvider] 回合掉落结果: {itemsToDrop.Count} 个道具");
        }
        
        return itemsToDrop;
    }
    
    /// <summary>
    /// 验证回合掉落表配置
    /// </summary>
    /// <returns>是否有效</returns>
    public bool ValidateTurnDropTables()
    {
        bool isValid = true;
        
        foreach (var dropTable in turnDropTables)
        {
            if (dropTable == null)
            {
                Debug.LogError("[TurnDropTableProvider] 发现空的回合掉落表配置");
                isValid = false;
                continue;
            }
            
            if (dropTable.dropEntries == null || dropTable.dropEntries.Count == 0)
            {
                Debug.LogWarning($"[TurnDropTableProvider] 回合掉落表 {dropTable.turnDropType} 没有配置掉落条目");
                continue;
            }
            
            foreach (var entry in dropTable.dropEntries)
            {
                if (entry.itemConfig == null)
                {
                    Debug.LogError($"[TurnDropTableProvider] 回合掉落表 {dropTable.turnDropType} 中有空的道具配置");
                    isValid = false;
                }
                
                if (entry.dropChance < 0f || entry.dropChance > 1f)
                {
                    Debug.LogError($"[TurnDropTableProvider] 回合掉落表 {dropTable.turnDropType} 中有无效的概率值: {entry.dropChance}");
                    isValid = false;
                }
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string info = $"TurnDropTableProvider: {turnDropTables.Count} 个回合掉落表\n";
        info += $"全局掉落率倍数: {globalDropRateMultiplier}\n";
        info += $"最大掉落数量: {maxDropCount}\n\n";
        
        foreach (var dropTable in turnDropTables)
        {
            if (dropTable != null)
            {
                info += $"- {dropTable.turnDropType}: {dropTable.dropEntries.Count} 个掉落条目\n";
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 在Inspector中验证配置
    /// </summary>
    [ContextMenu("验证回合掉落表配置")]
    public void ValidateConfig()
    {
        bool isValid = ValidateTurnDropTables();
        
        if (isValid)
        {
            Debug.Log("[TurnDropTableProvider] ✅ 回合掉落表配置验证通过");
        }
        else
        {
            Debug.LogError("[TurnDropTableProvider] ❌ 回合掉落表配置验证失败，请检查配置");
        }
    }
}

/// <summary>
/// 回合掉落表配置 - 单个回合类型的掉落配置
/// </summary>
[System.Serializable]
public class TurnDropTableConfig
{
    [LabelText("回合掉落类型")]
    [Tooltip("适用此掉落表的回合类型")]
    public TurnDropType turnDropType;
    
    [LabelText("最大掉落数量")]
    [Tooltip("此回合类型单次掉落的最大道具数量")]
    [MinValue(1)]
    public int maxDropCount = 2;
    
    [LabelText("掉落条目")]
    [Tooltip("此回合类型可能掉落的道具列表")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<ItemDropEntry> dropEntries = new List<ItemDropEntry>();
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        return $"{turnDropType}: {dropEntries.Count} 个掉落条目，最大掉落 {maxDropCount} 个";
    }
}
