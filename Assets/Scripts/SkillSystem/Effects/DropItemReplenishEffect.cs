using UnityEngine;
using System.Linq;

/// <summary>
/// 掉落物补充效果 - 收集者角色被动技能
/// 
/// 【核心功能】：
/// - 在玩家回合开始时，确保场上有指定数量的掉落物
/// - 如果数量不足，自动生成到目标数量
/// - 生成的掉落物只能被收集者角色拾取
/// 
/// 【使用场景】：
/// - 收集者角色的被动技能
/// - Trigger: PhaseStateTrigger [PlayerPhaseStart]
/// - Effect: DropItemReplenishEffect
/// 
/// 【配置参数】：
/// - targetItemCount: 目标掉落物数量（默认3）
/// - itemConfig: 生成的掉落物配置
/// - collectorCharacterID: 收集者角色ID（只有他能拾取）
/// - spawnRangeConfig: 生成范围配置（带碰撞检测）
/// </summary>
public class DropItemReplenishEffect : IEffect
{
    // 配置字段
    private int targetItemCount = 3;
    private ItemConfig itemConfig;
    private string collectorCharacterID;
    private bool showDebugLog = false;
    private bool canExecute = true;
    
    // ✅ 使用 SpawnRangeConfig（带碰撞检测）
    private SpawnRangeConfig spawnRangeConfig;
    
    // ItemSpawner 引用（延迟查找）
    private ItemSpawner itemSpawner;
    
    public string EffectName => "DropItemReplenishEffect";
    public bool CanExecute => canExecute;
    
    #region IEffect 实现
    
    public void Initialize()
    {
        // 初始化逻辑（如果需要）
    }
    
    public void SetCanExecute(bool value)
    {
        canExecute = value;
    }
    
    public void SetTarget(string characterID)
    {
        collectorCharacterID = characterID;
    }
    
    public bool ExecuteEffect(SkillArgs args)
    {
        // 验证配置
        if (itemConfig == null)
        {
            Debug.LogError($"[{EffectName}] ItemConfig 未配置！");
            return false;
        }
        
        if (string.IsNullOrEmpty(collectorCharacterID))
        {
            Debug.LogWarning($"[{EffectName}] 收集者角色ID未设置！");
            return false;
        }
        
        // 获取场上当前的掉落物数量
        int currentItemCount = GetCurrentItemCount();
        
        // 如果数量已满足，无需生成
        if (currentItemCount >= targetItemCount)
        {
            if (showDebugLog)
            {
                Debug.Log($"[{EffectName}] 掉落物数量已满足，无需补充");
            }
            return true;
        }
        
        // 计算需要生成的数量
        int needSpawnCount = targetItemCount - currentItemCount;
        
        if (showDebugLog)
        {
            Debug.Log($"[{EffectName}] 需要补充 {needSpawnCount} 个掉落物");
        }
        
        // 生成掉落物
        for (int i = 0; i < needSpawnCount; i++)
        {
            SpawnCollectorItem();
        }
        
        return true;
    }
    
    public void RemoveEffect()
    {
        // 掉落物补充是瞬时效果，无需清理
    }
    
    #endregion
    
    #region 配置方法
    
    /// <summary>
    /// 配置效果参数
    /// </summary>
    public void Configure(int targetCount, ItemConfig config, string collectorID, SpawnRangeConfig rangeConfig, bool debugLog = false)
    {
        targetItemCount = targetCount;
        itemConfig = config;
        collectorCharacterID = collectorID;
        spawnRangeConfig = rangeConfig;
        showDebugLog = debugLog;
    }
    
    #endregion
    
    #region 私有方法
    
    /// <summary>
    /// 获取场上当前的掉落物数量
    /// </summary>
    int GetCurrentItemCount()
    {
        // 查找所有 ItemPickup 组件
        var allItems = Object.FindObjectsByType<ItemPickup>(FindObjectsSortMode.None);
        
        // 只检查收集者专属的掉落物
        int count = allItems.Count(item => 
            item.itemConfig != null &&
            item.itemConfig.pickupRestriction == ItemPickupRestriction.SpecificCharacter &&
            item.itemConfig.restrictedCharacterName == collectorCharacterID
        );
        
        return count;
    }
    
    /// <summary>
    /// 生成收集者专属掉落物（使用 ItemSpawner 系统）
    /// </summary>
    void SpawnCollectorItem()
    {
        // 延迟获取 ItemSpawner
        if (itemSpawner == null)
        {
            itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
            
            if (itemSpawner == null)
            {
                Debug.LogError($"[{EffectName}] 未找到 ItemSpawner！无法生成道具");
                return;
            }
        }
        
        // 验证 SpawnRangeConfig 配置
        if (spawnRangeConfig == null)
        {
            Debug.LogError($"[{EffectName}] SpawnRangeConfig 未配置！无法生成道具");
            return;
        }
        
        // 使用 ItemSpawner 的 Spawn 方法（自动碰撞检测）
        itemSpawner.Spawn(itemConfig, null, spawnRangeConfig);
    }
    
    /// <summary>
    /// 获取 ItemSpawner 实例
    /// </summary>
    ItemSpawner GetItemSpawner()
    {
        if (itemSpawner == null)
        {
            itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
        }
        return itemSpawner;
    }
    
    #endregion
}

