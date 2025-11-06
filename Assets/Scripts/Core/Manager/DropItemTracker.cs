using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 掉落物追踪管理器 - 追踪每个角色本回合拾取的掉落物数量
/// 
/// 【核心职责】：
/// - 追踪每个角色本回合拾取的掉落物数量
/// - 监听拾取事件并更新计数
/// - 回合结束时自动重置计数
/// 
/// 【使用场景】：
/// - 收集者角色："收集打击"技能需要读取拾取数量
/// - 未来可能的成就系统、统计系统
/// 
/// 【设计说明】：
/// - 单例 Manager，跨场景保留
/// - 只追踪"当前回合"的拾取数量
/// - 回合结束时自动清空
/// 
/// 【执行顺序】：SYSTEM 层 (-50)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class DropItemTracker : SingletonManager<DropItemTracker>
{
    #region 私有字段
    
    /// <summary>
    /// 本回合每个角色拾取的掉落物数量
    /// Key: characterID, Value: 拾取数量
    /// </summary>
    private Dictionary<string, int> currentTurnPickups = new Dictionary<string, int>();
    
    /// <summary>
    /// 历史总拾取数量（可选，用于统计）
    /// </summary>
    private Dictionary<string, int> totalPickups = new Dictionary<string, int>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugLog = true;
    
    #endregion
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugLog;
    
    protected override void OnManagerCreated()
    {
        // 订阅游戏流程事件
        GameEventBus.OnGameFlowStateChanged += OnGameFlowStateChanged;
        GameEventBus.OnItemPickedUp += OnItemPickedUp;
        GameEventBus.OnGameRestart += ResetAllData;
        
        if (showDebugLog)
        {
            Debug.Log("[DropItemTracker] 单例创建成功，已订阅事件");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅
        GameEventBus.OnGameFlowStateChanged -= OnGameFlowStateChanged;
        GameEventBus.OnItemPickedUp -= OnItemPickedUp;
        GameEventBus.OnGameRestart -= ResetAllData;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 物品拾取事件处理
    /// </summary>
    private void OnItemPickedUp(string characterID, ItemConfig itemConfig, Vector3 position)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogWarning("[DropItemTracker] 拾取事件中角色ID为空，无法追踪");
            return;
        }
        
        // 更新本回合拾取计数
        if (!currentTurnPickups.ContainsKey(characterID))
        {
            currentTurnPickups[characterID] = 0;
        }
        currentTurnPickups[characterID]++;
        
        // 更新总拾取计数
        if (!totalPickups.ContainsKey(characterID))
        {
            totalPickups[characterID] = 0;
        }
        totalPickups[characterID]++;
        
        if (showDebugLog)
        {
            string itemName = itemConfig != null ? itemConfig.itemName : "未知物品";
            Debug.Log($"[DropItemTracker] {characterID} 拾取 {itemName}，本回合: {currentTurnPickups[characterID]}, 总计: {totalPickups[characterID]}");
        }
    }
    
    /// <summary>
    /// 游戏流程状态变化处理
    /// </summary>
    private void OnGameFlowStateChanged(GameFlowState newState)
    {
        // ✅ 修复：在玩家回合开始时清空上一回合的拾取计数
        // （而不是在回合结束时清空，避免和 CollectorStrikeEffect 冲突）
        if (newState == GameFlowState.PlayerPhaseStart)
        {
            if (showDebugLog && currentTurnPickups.Count > 0)
            {
                Debug.Log($"[DropItemTracker] PlayerPhaseStart - 清空上一回合拾取计数:");
                foreach (var kvp in currentTurnPickups)
                {
                    if (kvp.Value > 0)
                    {
                        Debug.Log($"  - {kvp.Key}: {kvp.Value} 个");
                    }
                }
            }
            
            ResetCurrentTurnPickups();
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取角色本回合拾取的掉落物数量
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>拾取数量</returns>
    public int GetCurrentTurnPickups(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            return 0;
        }
        
        return currentTurnPickups.TryGetValue(characterID, out int count) ? count : 0;
    }
    
    /// <summary>
    /// 获取角色历史总拾取数量
    /// </summary>
    /// <param name="characterID">角色ID</param>
    /// <returns>总拾取数量</returns>
    public int GetTotalPickups(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            return 0;
        }
        
        return totalPickups.TryGetValue(characterID, out int count) ? count : 0;
    }
    
    /// <summary>
    /// 重置当前回合拾取计数
    /// </summary>
    public void ResetCurrentTurnPickups()
    {
        if (showDebugLog && currentTurnPickups.Count > 0)
        {
            Debug.Log("[DropItemTracker] 重置本回合拾取计数");
            foreach (var kvp in currentTurnPickups)
            {
                if (kvp.Value > 0)
                {
                    Debug.Log($"  - {kvp.Key}: {kvp.Value} 个");
                }
            }
        }
        
        currentTurnPickups.Clear();
    }
    
    /// <summary>
    /// 重置所有数据（游戏重启时）
    /// </summary>
    public void ResetAllData()
    {
        currentTurnPickups.Clear();
        totalPickups.Clear();
        
        if (showDebugLog)
        {
            Debug.Log("[DropItemTracker] 重置所有追踪数据");
        }
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = "[DropItemTracker] 当前状态：\n";
        info += "━━━━━━━━━━━━━━━━\n";
        info += "本回合拾取：\n";
        
        if (currentTurnPickups.Count == 0)
        {
            info += "  （无）\n";
        }
        else
        {
            foreach (var kvp in currentTurnPickups)
            {
                info += $"  {kvp.Key}: {kvp.Value} 个\n";
            }
        }
        
        info += "━━━━━━━━━━━━━━━━\n";
        info += "历史总计：\n";
        
        if (totalPickups.Count == 0)
        {
            info += "  （无）\n";
        }
        else
        {
            foreach (var kvp in totalPickups)
            {
                info += $"  {kvp.Key}: {kvp.Value} 个\n";
            }
        }
        
        return info;
    }
    
    #endregion
}

