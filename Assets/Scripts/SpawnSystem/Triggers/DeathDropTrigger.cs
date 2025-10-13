using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 死亡掉落触发器 - 监听敌人死亡事件，决定掉落时机和内容
/// 决策层：负责掉落决策逻辑
/// </summary>
public class DeathDropTrigger : SpawnTrigger<ItemConfig>
{
    [Header("掉落配置")]
    [Tooltip("掉落表配置提供者")]
    public DropTableProvider dropTableProvider;
    
    [Tooltip("道具生成器")]
    public ItemSpawner itemSpawner;
    
    [Header("掉落设置")]
    [Tooltip("是否启用掉落位置偏移")]
    public bool enableDropPositionOffset = true;
    
    [Tooltip("掉落位置偏移范围")]
    public float dropOffsetRange = 0.5f;
    
    [Header("过滤设置")]
    [Tooltip("是否只处理特定标签的敌人")]
    public bool filterByTag = false;
    
    [Tooltip("目标敌人标签")]
    public string targetEnemyTag = "Enemy";
    
    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool enableDebugLog = true;
    
    /// <summary>
    /// 技能状态管理器引用
    /// </summary>
    private SkillStateManager skillStateManager;
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    protected override void Initialize()
    {
        // 跳过基类的Initialize()，避免configProvider检查
        // base.Initialize();
        
        // 验证必要组件
        if (dropTableProvider == null)
        {
            Debug.LogError("[DeathDropTrigger] DropTableProvider 未设置");
            return;
        }
        
        if (itemSpawner == null)
        {
            Debug.LogError("[DeathDropTrigger] ItemSpawner 未设置");
            return;
        }
        
        // 配置ItemSpawner的掉落设置
        itemSpawner.enableDropPositionOffset = enableDropPositionOffset;
        itemSpawner.dropOffsetRange = dropOffsetRange;
        
        // 查找技能状态管理器
        skillStateManager = FindObjectOfType<SkillStateManager>();
        if (skillStateManager == null)
        {
            Debug.LogWarning("[DeathDropTrigger] 未找到SkillStateManager，条件掉落功能将不可用");
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[DeathDropTrigger] 初始化完成");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    protected override void SubscribeEvents()
    {
        GameEventBus.OnDeath += OnEnemyDeath;
        
        if (enableDebugLog)
        {
            Debug.Log("[DeathDropTrigger] 已订阅死亡事件");
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    protected override void UnsubscribeEvents()
    {
        GameEventBus.OnDeath -= OnEnemyDeath;
        
        if (enableDebugLog)
        {
            Debug.Log("[DeathDropTrigger] 已取消死亡事件订阅");
        }
    }
    
    /// <summary>
    /// 处理敌人死亡事件
    /// </summary>
    /// <param name="deathData">死亡事件数据</param>
    private void OnEnemyDeath(DeathData deathData)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropTrigger] 收到死亡事件: {deathData.target.name} at {deathData.Position}");
        }
        
        // 1. 验证是否为敌人
        if (!IsEnemy(deathData.target))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropTrigger] 目标不是敌人，跳过掉落: {deathData.target.name}");
            }
            return;
        }
        
        // 2. 标签过滤
        if (filterByTag && !deathData.target.CompareTag(targetEnemyTag))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropTrigger] 目标标签不匹配，跳过掉落: {deathData.target.tag}");
            }
            return;
        }
        
        // 3. 查询掉落表
        var dropTable = dropTableProvider.GetDropTable(deathData.enemyType);
        if (dropTable == null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropTrigger] 未找到敌人类型 {deathData.enemyType} 的掉落表");
            }
            return;
        }
        
        // 4. 执行概率抽取（考虑技能条件）
        HashSet<string> activeSkills = skillStateManager?.GetActiveSkills();
        var itemsToDrop = dropTableProvider.GetItemsToDrop(dropTable, activeSkills);
        if (itemsToDrop.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DeathDropTrigger] 概率抽取结果为空，无道具掉落");
            }
            return;
        }
        
        // 5. 生成道具
        SpawnDroppedItems(itemsToDrop, deathData.Position);
        
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropTrigger] 掉落完成: {itemsToDrop.Count} 个道具");
        }
    }
    
    /// <summary>
    /// 判断目标是否为敌人
    /// </summary>
    /// <param name="target">目标对象</param>
    /// <returns>是否为敌人</returns>
    private bool IsEnemy(GameObject target)
    {
        if (target == null) return false;
        
        // 检查是否有EnemyBehavior组件
        var enemyBehavior = target.GetComponent<EnemyBehavior>();
        if (enemyBehavior != null) return true;
        
        // 检查标签
        if (target.CompareTag("Enemy")) return true;
        
        // 检查名称包含"Enemy"
        if (target.name.ToLower().Contains("enemy")) return true;
        
        return false;
    }
    
    /// <summary>
    /// 生成掉落的道具
    /// </summary>
    /// <param name="itemsToDrop">要掉落的道具列表</param>
    /// <param name="deathPosition">死亡位置</param>
    private void SpawnDroppedItems(List<ItemConfig> itemsToDrop, Vector3 deathPosition)
    {
        if (itemsToDrop == null || itemsToDrop.Count == 0)
        {
            Debug.LogWarning("[DeathDropTrigger] 掉落道具列表为空");
            return;
        }
        
        // 转换为数组
        ItemConfig[] itemsArray = itemsToDrop.ToArray();
        
        // 调用ItemSpawner批量生成
        itemSpawner.SpawnItems(itemsArray, deathPosition);
        
        if (enableDebugLog)
        {
            Debug.Log($"[DeathDropTrigger] 在位置 {deathPosition} 生成 {itemsArray.Length} 个道具");
        }
    }
    
    /// <summary>
    /// 手动触发掉落（用于测试）
    /// </summary>
    /// <param name="enemyType">敌人类型</param>
    /// <param name="position">掉落位置</param>
    [ContextMenu("测试掉落")]
    public void TestDrop(EnemyType enemyType = EnemyType.Normal, Vector3 position = default)
    {
        if (position == default)
        {
            position = transform.position;
        }
        
        var dropTable = dropTableProvider.GetDropTable(enemyType);
        if (dropTable == null)
        {
            Debug.LogError($"[DeathDropTrigger] 未找到敌人类型 {enemyType} 的掉落表");
            return;
        }
        
        var itemsToDrop = dropTableProvider.GetItemsToDrop(dropTable);
        SpawnDroppedItems(itemsToDrop, position);
        
        Debug.Log($"[DeathDropTrigger] 测试掉落完成: {itemsToDrop.Count} 个道具");
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        string info = $"DeathDropTrigger:\n";
        info += $"- DropTableProvider: {(dropTableProvider != null ? "已设置" : "未设置")}\n";
        info += $"- ItemSpawner: {(itemSpawner != null ? "已设置" : "未设置")}\n";
        info += $"- 标签过滤: {(filterByTag ? $"启用 ({targetEnemyTag})" : "禁用")}\n";
        info += $"- 位置偏移: {(enableDropPositionOffset ? $"启用 ({dropOffsetRange})" : "禁用")}\n";
        
        return info;
    }
}
