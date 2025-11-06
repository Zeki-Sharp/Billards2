using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 掉落物补充效果配置 - 用于 Inspector 配置
/// 
/// 【配置项】：
/// - 目标掉落物数量
/// - 掉落物类型（ItemConfig）
/// - 生成区域范围
/// - 调试日志开关
/// 
/// 【使用场景】：
/// - 收集者角色的被动技能
/// - 技能配置：Trigger [PlayerPhaseStart] + Effect [DropItemReplenish]
/// </summary>
[System.Serializable]
public class DropItemReplenishEffectConfig : EffectBase
{
    [BoxGroup("掉落物配置")]
    [LabelText("目标掉落物数量")]
    [Tooltip("场上应该保持的掉落物数量")]
    [MinValue(1)]
    public int targetItemCount = 3;
    
    [BoxGroup("掉落物配置")]
    [LabelText("掉落物配置")]
    [Tooltip("要生成的掉落物类型（必须配置拾取限制）")]
    [Required("必须指定掉落物配置")]
    [AssetsOnly]
    public ItemConfig itemConfig;
    
    [BoxGroup("掉落物配置")]
    [LabelText("生成范围配置")]
    [Tooltip("生成区域配置（带碰撞检测）\n必须启用 Check Obstacles 以避免生成在墙壁上")]
    [Required("必须指定生成范围")]
    [AssetsOnly]
    public SpawnRangeConfig spawnRangeConfig;
    
    [BoxGroup("调试")]
    [LabelText("显示调试日志")]
    [Tooltip("是否在Console中显示补充日志")]
    public bool showDebugLog = true;
    
    #region EffectBase 实现
    
    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        // 验证配置
        if (itemConfig == null)
        {
            Debug.LogError("[DropItemReplenishEffectConfig] ItemConfig 未配置！");
            return null;
        }
        
        if (spawnRangeConfig == null)
        {
            Debug.LogError("[DropItemReplenishEffectConfig] SpawnRangeConfig 未配置！");
            return null;
        }
        
        // 验证掉落物是否配置了拾取限制
        if (itemConfig.pickupRestriction != ItemPickupRestriction.SpecificCharacter)
        {
            Debug.LogError($"[DropItemReplenishEffectConfig] 掉落物 '{itemConfig.itemName}' 必须配置为 SpecificCharacter 限制类型！");
            return null;
        }
        
        if (string.IsNullOrEmpty(itemConfig.restrictedCharacterName))
        {
            Debug.LogWarning($"[DropItemReplenishEffectConfig] 掉落物 '{itemConfig.itemName}' 未指定允许拾取的角色名，需要在技能实例化时设置");
        }
        
        // 创建效果实例
        DropItemReplenishEffect effect = new DropItemReplenishEffect();
        
        // ✅ 配置参数（使用 SpawnRangeConfig）
        effect.Configure(
            targetItemCount,
            itemConfig,
            itemConfig.restrictedCharacterName,
            spawnRangeConfig,
            showDebugLog
        );
        
        effect.Initialize();
        
        if (showDebugLog)
        {
            Debug.Log($"[DropItemReplenishEffectConfig] ✅ 创建掉落物补充效果：" +
                     $"目标数量={targetItemCount}，类型={itemConfig.itemName}");
        }
        
        return effect;
    }
    
    public override string GetDebugInfo()
    {
        string itemName = itemConfig != null ? itemConfig.itemName : "未配置";
        string rangeInfo = spawnRangeConfig != null ? "已配置" : "未配置";
        return $"掉落物补充 - 目标数量:{targetItemCount}, 类型:{itemName}, 范围:{rangeInfo}";
    }
    
    #endregion
}

