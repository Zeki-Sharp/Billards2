using UnityEngine;

/// <summary>
/// 掉落物品效果 - 根据技能配置掉落物品
/// 用于实现"击杀掉落回血物"等技能效果
/// </summary>
public class DropItemEffect : IEffect
{
    public string EffectName => "DropItemEffect";
    
    // 瞬时效果，总是可以执行
    public bool CanExecute => true;
    
    /// <summary>
    /// 设置是否允许执行（空实现，瞬时效果总是可以执行）
    /// </summary>
    public void SetCanExecute(bool canExecute)
    {
        // 瞬时效果不需要控制执行权限
    }
    
    private ItemConfig dropItemConfig;
    private float dropChance;
    private DropRangeConfig dropRangeConfig;
    private ItemSpawner itemSpawner;
    
    /// <summary>
    /// 设置掉落配置
    /// </summary>
    /// <param name="itemConfig">要掉落的物品配置</param>
    /// <param name="chance">掉落概率</param>
    /// <param name="rangeConfig">掉落范围配置</param>
    public void SetDropConfig(ItemConfig itemConfig, float chance, DropRangeConfig rangeConfig)
    {
        dropItemConfig = itemConfig;
        dropChance = chance;
        dropRangeConfig = rangeConfig;
    }
    
    /// <summary>
    /// 初始化效果
    /// </summary>
    public void Initialize()
    {
        // 延迟查找ItemSpawner，在需要时才查找
        // 因为初始化时场景可能还没有完全加载
        
        // 检查配置
        if (dropItemConfig == null)
        {
            Debug.LogError($"[{EffectName}] 初始化时掉落物品配置为空！");
        }
    }
    
    /// <summary>
    /// 获取ItemSpawner（延迟查找）
    /// </summary>
    private ItemSpawner GetItemSpawner()
    {
        if (itemSpawner == null)
        {
            itemSpawner = Object.FindFirstObjectByType<ItemSpawner>();
            if (itemSpawner == null)
            {
                Debug.LogError($"[{EffectName}] 无法找到ItemSpawner，请确保场景中有ItemSpawner组件");
            }
        }
        return itemSpawner;
    }
    
    /// <summary>
    /// 执行掉落物品效果
    /// </summary>
    /// <param name="eventData">事件数据，期望是DeathData</param>
    /// <returns>效果是否执行成功</returns>
    public bool ExecuteEffect(object eventData)
    {
        // 检查配置是否完整
        if (dropItemConfig == null)
        {
            Debug.LogError($"[{EffectName}] 掉落物品配置为空，无法执行掉落");
            return false;
        }
        
        // 获取ItemSpawner（延迟查找）
        ItemSpawner spawner = GetItemSpawner();
        if (spawner == null)
        {
            Debug.LogError($"[{EffectName}] 无法获取ItemSpawner，无法执行掉落");
            return false;
        }
        
        // 概率判定
        if (Random.Range(0f, 1f) > dropChance)
        {
            return false; // 概率判定失败，静默跳过
        }
        
        // 获取掉落位置
        Vector3 dropPosition = GetDropPosition(eventData);
        if (dropPosition == Vector3.zero)
        {
            Debug.LogError($"[{EffectName}] 无法获取有效的掉落位置");
            return false;
        }
        
        // 执行掉落
        try
        {
            spawner.Spawn(dropItemConfig, dropPosition);
            Debug.Log($"[{EffectName}] ✅ 成功掉落物品: {dropItemConfig.itemName}");
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[{EffectName}] 掉落物品失败: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 获取掉落位置
    /// </summary>
    /// <param name="eventData">事件数据</param>
    /// <returns>掉落位置</returns>
    private Vector3 GetDropPosition(object eventData)
    {
        Vector3 basePosition = Vector3.zero;
        
        // 从DeathData中获取敌人死亡位置
        if (eventData is DeathData deathData)
        {
            basePosition = deathData.Position;
        }
        else
        {
            Debug.LogWarning($"[{EffectName}] 事件数据类型不是DeathData: {eventData?.GetType()}");
            return Vector3.zero;
        }
        
        // 使用掉落范围配置计算最终位置
        if (dropRangeConfig != null)
        {
            return dropRangeConfig.GetRandomPosition(basePosition);
        }
        else
        {
            // 如果没有范围配置，直接使用基础位置
            return basePosition;
        }
    }
    
    /// <summary>
    /// 重置效果状态
    /// </summary>
    public void Reset()
    {
        // 掉落效果是瞬时效果，不需要重置状态
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    /// <returns>调试信息</returns>
    public string GetDebugInfo()
    {
        string itemName = dropItemConfig != null ? dropItemConfig.itemName : "空";
        string rangeInfo = dropRangeConfig != null ? dropRangeConfig.GetDebugInfo() : "无范围配置";
        return $"掉落物品: {itemName}, 概率: {dropChance:P0}, {rangeInfo}";
    }
}
