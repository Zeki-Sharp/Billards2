using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 回合道具生成触发器 - 监听回合开始事件，根据回合类型和技能状态生成道具
/// 只负责事件监听和调用，不包含配置逻辑
/// </summary>
public class TurnItemSpawnTrigger : MonoBehaviour
{
    [Header("依赖引用")]
    [Tooltip("回合掉落表配置提供者")]
    public TurnDropTableProvider turnDropTableProvider;
    
    [Tooltip("道具生成器")]
    public ItemSpawner itemSpawner;
    
    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool enableDebugLog = true;
    
    /// <summary>
    /// 技能状态管理器引用
    /// </summary>
    private SkillStateManager skillStateManager;
    
    void Start()
    {
        InitializeTrigger();
        SubscribeToEvents();
    }
    
    void OnDestroy()
    {
        UnsubscribeFromEvents();
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    void InitializeTrigger()
    {
        // 验证必要组件
        if (turnDropTableProvider == null)
        {
            Debug.LogError("[TurnItemSpawnTrigger] TurnDropTableProvider 未设置");
            return;
        }
        
        if (itemSpawner == null)
        {
            Debug.LogError("[TurnItemSpawnTrigger] ItemSpawner 未设置");
            return;
        }
        
        // 查找技能状态管理器
        skillStateManager = FindFirstObjectByType<SkillStateManager>();
        if (skillStateManager == null)
        {
            Debug.LogWarning("[TurnItemSpawnTrigger] 未找到SkillStateManager，条件掉落功能将不可用");
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[TurnItemSpawnTrigger] 初始化完成");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
        
        if (enableDebugLog)
        {
            Debug.Log("[TurnItemSpawnTrigger] 已订阅游戏流程状态变化事件");
        }
    }
    
    /// <summary>
    /// 取消事件订阅
    /// </summary>
    void UnsubscribeFromEvents()
    {
        GameEventBus.OnGameFlowStateChanged -= OnGameFlowStateChanged;
        
        if (enableDebugLog)
        {
            Debug.Log("[TurnItemSpawnTrigger] 已取消游戏流程状态变化事件订阅");
        }
    }
    
    /// <summary>
    /// 处理游戏流程状态变化事件
    /// </summary>
    /// <param name="newState">新的游戏流程状态</param>
    void OnGameFlowStateChanged(GameFlowState newState)
    {
        // 处理所有回合类型的掉落
        ProcessAllTurnDrops(newState);
    }
    
    /// <summary>
    /// 处理所有回合类型的掉落
    /// </summary>
    /// <param name="gameFlowState">当前游戏流程状态</param>
    void ProcessAllTurnDrops(GameFlowState gameFlowState)
    {
        // 遍历所有回合掉落表
        foreach (var turnDropTable in turnDropTableProvider.turnDropTables)
        {
            if (turnDropTable == null) continue;
            
            // 检查是否应该处理这个回合类型
            bool shouldProcess = ShouldProcessTurnType(turnDropTable.turnDropType, gameFlowState);
            
            if (shouldProcess)
            {
                ProcessTurnDrop(turnDropTable);
            }
        }
    }
    
    /// <summary>
    /// 检查是否应该处理指定的回合类型
    /// </summary>
    /// <param name="turnDropType">回合掉落类型</param>
    /// <param name="gameFlowState">当前游戏流程状态</param>
    /// <returns>是否应该处理</returns>
    bool ShouldProcessTurnType(TurnDropType turnDropType, GameFlowState gameFlowState)
    {
        switch (turnDropType)
        {
            case TurnDropType.PlayerTurnStart:
                return gameFlowState == GameFlowState.PlayerPhase;
            case TurnDropType.EnemyTurnStart:
                return gameFlowState == GameFlowState.EnemyPhase;
            // 未来可以添加其他回合类型
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 处理单个回合掉落表
    /// </summary>
    /// <param name="turnDropTable">回合掉落表配置</param>
    void ProcessTurnDrop(TurnDropTableConfig turnDropTable)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[TurnItemSpawnTrigger] 处理 {turnDropTable.turnDropType} 掉落");
        }
        
        // 获取当前激活的技能（用于条件检查）
        HashSet<string> activeSkills = skillStateManager?.GetActiveSkills();
        
        // 执行概率抽取（考虑技能条件）
        var itemsToDrop = turnDropTableProvider.GetItemsToDrop(turnDropTable, activeSkills);
        if (itemsToDrop.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[TurnItemSpawnTrigger] {turnDropTable.turnDropType} 概率抽取结果为空，无道具掉落");
            }
            return;
        }
        
        // 生成道具
        SpawnDroppedItems(itemsToDrop, turnDropTable);
        
        if (enableDebugLog)
        {
            Debug.Log($"[TurnItemSpawnTrigger] {turnDropTable.turnDropType} 掉落完成: {itemsToDrop.Count} 个道具");
        }
    }
    
    /// <summary>
    /// 生成掉落的道具
    /// </summary>
    /// <param name="itemsToDrop">要掉落的道具列表</param>
    /// <param name="turnDropTable">回合掉落表配置（用于获取生成位置等配置）</param>
    void SpawnDroppedItems(List<ItemConfig> itemsToDrop, TurnDropTableConfig turnDropTable)
    {
        if (itemsToDrop == null || itemsToDrop.Count == 0)
        {
            Debug.LogWarning("[TurnItemSpawnTrigger] 掉落道具列表为空");
            return;
        }
        
        // 转换为数组
        ItemConfig[] itemsArray = itemsToDrop.ToArray();
        
        // 从TurnDropTableConfig获取生成位置（如果配置了的话）
        Vector3 spawnPosition = GetSpawnPosition(turnDropTable);
        
        // 调用ItemSpawner批量生成
        itemSpawner.SpawnItems(itemsArray, spawnPosition);
        
        if (enableDebugLog)
        {
            Debug.Log($"[TurnItemSpawnTrigger] 在位置 {spawnPosition} 生成 {itemsArray.Length} 个道具");
        }
    }
    
    /// <summary>
    /// 获取生成位置
    /// </summary>
    /// <param name="turnDropTable">回合掉落表配置</param>
    /// <returns>生成位置</returns>
    Vector3 GetSpawnPosition(TurnDropTableConfig turnDropTable)
    {
        // 这里可以从TurnDropTableConfig中获取位置配置
        // 或者使用默认位置
        return Vector3.zero; // 默认位置
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        string info = $"TurnItemSpawnTrigger:\n";
        info += $"- TurnDropTableProvider: {(turnDropTableProvider != null ? "已设置" : "未设置")}\n";
        info += $"- ItemSpawner: {(itemSpawner != null ? "已设置" : "未设置")}\n";
        info += $"- 配置的回合类型数量: {(turnDropTableProvider?.turnDropTables?.Count ?? 0)}";
        
        return info;
    }
}
