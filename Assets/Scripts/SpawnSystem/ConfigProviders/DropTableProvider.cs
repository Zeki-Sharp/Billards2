using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector;
using System.Linq;

/// <summary>
/// 掉落条件类型
/// </summary>
public enum DropConditionType
{
    Always,         // 总是掉落
    SkillRequired   // 需要特定技能
}

/// <summary>
/// 掉落表配置提供者 - 管理敌人类型与掉落表的映射关系
/// 配置层：负责掉落配置的管理和查询
/// </summary>
[CreateAssetMenu(fileName = "DropTableProvider", menuName = "Game/Drop Table Provider")]
public class DropTableProvider : ScriptableObject, SpawnConfigProvider<ItemConfig>
{
    [Header("掉落表配置")]
    [LabelText("掉落表列表")]
    [Tooltip("不同敌人类型的掉落表配置")]
    [ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "enemyType")]
    public List<DropTableConfig> dropTables = new List<DropTableConfig>();
    
    [Header("全局设置")]
    [LabelText("全局掉落率倍数")]
    [Tooltip("所有掉落的概率都会乘以这个倍数")]
    [Range(0f, 2f)]
    public float globalDropRateMultiplier = 1f;
    
    [LabelText("最大掉落数量")]
    [Tooltip("单次掉落的最大道具数量")]
    [MinValue(1)]
    public int maxDropCount = 5;
    
    [Header("默认掉落范围配置")]
    [LabelText("默认掉落范围")]
    [Tooltip("当DropTableConfig中没有配置掉落范围时使用的默认范围")]
    public SpawnRangeConfig defaultDropRange = new SpawnRangeConfig();
    
    [Header("调试设置")]
    [LabelText("启用调试日志")]
    public bool enableDebugLog = true;
    
    /// <summary>
    /// 获取指定敌人类型的掉落表
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    /// <returns>掉落表配置，如果未找到返回null</returns>
    public DropTableConfig GetDropTable(EnemyType enemyType)
    {
        foreach (var dropTable in dropTables)
        {
            if (dropTable.enemyType == enemyType)
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[DropTableProvider] 找到掉落表: {enemyType}");
                }
                return dropTable;
            }
        }
        
        if (enableDebugLog)
        {
            Debug.LogWarning($"[DropTableProvider] 未找到敌人类型 {enemyType} 的掉落表");
        }
        return null;
    }
    
    /// <summary>
    /// 获取指定敌人类型的掉落物品列表
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    /// <param name="activeSkills">当前激活的技能名称集合（可选）</param>
    /// <returns>掉落物品列表</returns>
    public List<ItemConfig> GetItemsToDrop(EnemyType enemyType, HashSet<string> activeSkills = null)
    {
        var dropTable = GetDropTable(enemyType);
        if (dropTable == null)
        {
            return new List<ItemConfig>();
        }
        
        return GetItemsToDrop(dropTable, activeSkills);
    }
    
    /// <summary>
    /// 根据掉落表获取掉落物品列表
    /// </summary>
    /// <param name="dropTable">掉落表配置</param>
    /// <param name="activeSkills">当前激活的技能名称集合（可选）</param>
    /// <returns>掉落物品列表</returns>
    public List<ItemConfig> GetItemsToDrop(DropTableConfig dropTable, HashSet<string> activeSkills = null)
    {
        if (dropTable == null)
        {
            Debug.LogError("[DropTableProvider] 掉落表配置为空");
            return new List<ItemConfig>();
        }
        
        List<ItemConfig> itemsToDrop = new List<ItemConfig>();
        
        // 限制最大掉落数量
        int maxDrops = Mathf.Min(dropTable.maxDropCount, maxDropCount);
        
        // 按权重排序掉落条目
        var sortedEntries = new List<ItemDropEntry>(dropTable.dropEntries);
        sortedEntries.Sort((a, b) => b.weight.CompareTo(a.weight));
        
        // 执行概率抽取
        foreach (var entry in sortedEntries)
        {
            if (itemsToDrop.Count >= maxDrops)
            {
                break;
            }
            
            if (entry.itemConfig == null)
            {
                Debug.LogWarning("[DropTableProvider] 掉落条目中的道具配置为空，跳过");
                continue;
            }
            
            // 检查掉落条件
            if (!entry.CheckDropCondition(activeSkills))
            {
                if (enableDebugLog)
                {
                    Debug.Log($"[DropTableProvider] 掉落条件不满足，跳过: {entry.itemConfig.itemName} (需要技能: {entry.requiredSkillName})");
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
                    Debug.Log($"[DropTableProvider] 掉落道具: {entry.itemConfig.itemName} (概率: {finalDropChance:P1}){conditionInfo}");
                }
            }
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[DropTableProvider] 掉落结果: {itemsToDrop.Count} 个道具");
        }
        
        return itemsToDrop;
    }
    
    /// <summary>
    /// 验证掉落表配置
    /// </summary>
    /// <returns>是否有效</returns>
    public bool ValidateDropTables()
    {
        bool isValid = true;
        
        foreach (var dropTable in dropTables)
        {
            if (dropTable == null)
            {
                Debug.LogError("[DropTableProvider] 发现空的掉落表配置");
                isValid = false;
                continue;
            }
            
            if (dropTable.dropEntries == null || dropTable.dropEntries.Count == 0)
            {
                Debug.LogWarning($"[DropTableProvider] 掉落表 {dropTable.enemyType} 没有配置掉落条目");
                continue;
            }
            
            foreach (var entry in dropTable.dropEntries)
            {
                if (entry.itemConfig == null)
                {
                    Debug.LogError($"[DropTableProvider] 掉落表 {dropTable.enemyType} 中有空的道具配置");
                    isValid = false;
                }
                
                if (entry.dropChance < 0f || entry.dropChance > 1f)
                {
                    Debug.LogError($"[DropTableProvider] 掉落表 {dropTable.enemyType} 中有无效的概率值: {entry.dropChance}");
                    isValid = false;
                }
            }
        }
        
        return isValid;
    }
    
    /// <summary>
    /// 获取默认掉落范围配置
    /// </summary>
    /// <returns>默认掉落范围配置</returns>
    public SpawnRangeConfig GetDefaultDropRange()
    {
        return defaultDropRange;
    }
    
    /// <summary>
    /// 获取指定敌人类型的掉落范围配置
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    /// <returns>掉落范围配置，如果未找到则返回默认配置</returns>
    public SpawnRangeConfig GetDropRange(EnemyType enemyType)
    {
        var dropTable = GetDropTable(enemyType);
        if (dropTable != null && dropTable.dropRange != null)
        {
            return dropTable.dropRange;
        }
        
        // 返回默认配置
        return defaultDropRange;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        string info = $"DropTableProvider: {dropTables.Count} 个掉落表\n";
        info += $"全局掉落率倍数: {globalDropRateMultiplier}\n";
        info += $"最大掉落数量: {maxDropCount}\n";
        info += $"默认掉落范围: {defaultDropRange.GetDebugInfo()}\n\n";
        
        foreach (var dropTable in dropTables)
        {
            if (dropTable != null)
            {
                info += $"- {dropTable.enemyType}: {dropTable.dropEntries.Count} 个掉落条目\n";
                if (dropTable.dropRange != null)
                {
                    info += $"  掉落范围: {dropTable.dropRange.GetDebugInfo()}\n";
                }
            }
        }
        
        return info;
    }
    
    /// <summary>
    /// 在Inspector中验证配置
    /// </summary>
    [ContextMenu("验证掉落表配置")]
    public void ValidateConfig()
    {
        bool isValid = ValidateDropTables();
        
        if (isValid)
        {
            Debug.Log("[DropTableProvider] ✅ 掉落表配置验证通过");
        }
        else
        {
            Debug.LogError("[DropTableProvider] ❌ 掉落表配置验证失败，请检查配置");
        }
    }
    
    #region SpawnConfigProvider<ItemConfig> 接口实现
    
    /// <summary>
    /// 获取生成数据列表（接口实现）
    /// 注意：这个方法对于掉落系统来说不太适用，因为掉落是基于敌人类型的
    /// 这里返回一个空列表，实际使用 GetItemsToDrop(EnemyType) 方法
    /// </summary>
    /// <returns>空的ItemConfig列表</returns>
    public List<ItemConfig> GetSpawnData()
    {
        // 掉落系统不使用这个方法，因为掉落是基于敌人类型的
        // 实际使用 GetItemsToDrop(EnemyType) 方法
        return new List<ItemConfig>();
    }
    
    /// <summary>
    /// 判断是否应该生成（接口实现）
    /// 对于掉落系统，总是返回true，因为掉落是基于敌人死亡事件触发的
    /// </summary>
    /// <returns>总是返回true</returns>
    public bool ShouldSpawn()
    {
        // 掉落系统基于事件触发，不基于时间判断
        return true;
    }
    
    /// <summary>
    /// 获取生成数量（接口实现）
    /// 对于掉落系统，返回全局最大掉落数量
    /// </summary>
    /// <returns>最大掉落数量</returns>
    public int GetSpawnCount()
    {
        return maxDropCount;
    }
    
    /// <summary>
    /// 初始化配置提供者（接口实现）
    /// </summary>
    public void Initialize()
    {
        // ScriptableObject不需要特殊的初始化逻辑
        // 配置在编辑器中已经设置好了
        if (enableDebugLog)
        {
            Debug.Log($"[DropTableProvider] 初始化完成，包含 {dropTables.Count} 个掉落表");
        }
    }
    
    /// <summary>
    /// 重置配置提供者状态（接口实现）
    /// </summary>
    public void Reset()
    {
        // ScriptableObject不需要重置状态
        // 掉落表配置是静态的
        if (enableDebugLog)
        {
            Debug.Log("[DropTableProvider] 重置完成");
        }
    }
    
    #endregion
}

/// <summary>
/// 掉落表配置 - 单个敌人类型的掉落配置
/// </summary>
[System.Serializable]
public class DropTableConfig
{
    [LabelText("敌人类型")]
    [Tooltip("适用此掉落表的敌人类型")]
    public EnemyType enemyType;
    
    [LabelText("最大掉落数量")]
    [Tooltip("此敌人类型单次掉落的最大道具数量")]
    [MinValue(1)]
    public int maxDropCount = 1;
    
    [LabelText("掉落条目")]
    [Tooltip("此敌人类型可能掉落的道具列表")]
    [ListDrawerSettings(ShowIndexLabels = true)]
    public List<ItemDropEntry> dropEntries = new List<ItemDropEntry>();
    
    [Header("掉落范围配置")]
    [LabelText("掉落范围")]
    [Tooltip("此敌人类型的道具掉落范围配置（为空时使用Provider的默认配置）")]
    public SpawnRangeConfig dropRange;
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        string info = $"{enemyType}: {dropEntries.Count} 个掉落条目，最大掉落 {maxDropCount} 个";
        if (dropRange != null)
        {
            info += $"\n掉落范围: {dropRange.GetDebugInfo()}";
        }
        return info;
    }
}

/// <summary>
/// 掉落条目 - 单个道具的掉落配置
/// </summary>
[System.Serializable]
public class ItemDropEntry
{
    [LabelText("道具配置")]
    [Tooltip("要掉落的道具配置")]
    public ItemConfig itemConfig;
    
    [LabelText("掉落概率")]
    [Tooltip("掉落此道具的概率（0-1）")]
    [Range(0f, 1f)]
    public float dropChance = 0.3f;
    
    [LabelText("权重")]
    [Tooltip("掉落优先级权重（数值越大优先级越高）")]
    [MinValue(1)]
    public int weight = 1;
    
    [Header("掉落条件")]
    [LabelText("掉落条件类型")]
    [Tooltip("掉落此道具的条件类型")]
    public DropConditionType conditionType = DropConditionType.Always;
    
    [LabelText("需要技能名称")]
    [Tooltip("当条件类型为SkillRequired时，需要激活的技能名称")]
    [ShowIf("conditionType", DropConditionType.SkillRequired)]
    [ValueDropdown("GetSpawnSkillNames")]
    public string requiredSkillName = "";
    
    /// <summary>
    /// 获取所有可用的Spawn技能名称（用于下拉选择）
    /// </summary>
    private IEnumerable<ValueDropdownItem<string>> GetSpawnSkillNames()
    {
        var skillManager = Object.FindObjectOfType<SkillManager>();
        if (skillManager == null)
        {
            return new List<ValueDropdownItem<string>>();
        }
        
        var spawnSkills = skillManager.GetSpawnSkillNames();
        return spawnSkills.Select(skillName => new ValueDropdownItem<string>(skillName, skillName));
    }
    
    /// <summary>
    /// 检查掉落条件是否满足
    /// </summary>
    /// <param name="activeSkills">当前激活的技能名称集合</param>
    /// <returns>是否满足掉落条件</returns>
    public bool CheckDropCondition(HashSet<string> activeSkills)
    {
        switch (conditionType)
        {
            case DropConditionType.Always:
                Debug.Log($"[ItemDropEntry] 检查掉落条件 - 类型: Always, 道具: {itemConfig?.itemName}, 结果: true");
                return true;
            case DropConditionType.SkillRequired:
                bool hasSkill = !string.IsNullOrEmpty(requiredSkillName) && 
                               activeSkills != null && 
                               activeSkills.Contains(requiredSkillName);
                Debug.Log($"[ItemDropEntry] 检查掉落条件 - 类型: SkillRequired, 道具: {itemConfig?.itemName}, 需要技能: {requiredSkillName}, 激活技能数: {activeSkills?.Count ?? 0}, 结果: {hasSkill}");
                return hasSkill;
            default:
                return true;
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        string itemName = itemConfig != null ? itemConfig.itemName : "空";
        string conditionInfo = conditionType == DropConditionType.SkillRequired ? 
            $" (需要技能: {requiredSkillName})" : "";
        return $"{itemName}: {dropChance:P0} (权重: {weight}){conditionInfo}";
    }
}