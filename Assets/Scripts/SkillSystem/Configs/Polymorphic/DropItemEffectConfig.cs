using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 掉落物品效果配置 - 掉落可拾取的物品
/// </summary>
[System.Serializable]
public class DropItemEffectConfig : EffectBase
{
    [BoxGroup("掉落物品配置")]
    [LabelText("掉落物品配置")]
    [Tooltip("要掉落的物品配置")]
    [Required]
    public ItemConfig dropItemConfig;

    [BoxGroup("掉落物品配置")]
    [LabelText("掉落概率")]
    [Tooltip("掉落此物品的概率（0-1）")]
    [Range(0f, 1f)]
    public float dropChance = 1.0f;

    [BoxGroup("掉落物品配置")]
    [LabelText("掉落范围配置")]
    [Tooltip("掉落位置的范围配置")]
    public DropRangeConfig dropRangeConfig = new DropRangeConfig();

    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var dropItemEffect = new DropItemEffect();
        
        // 设置掉落物品专用参数
        dropItemEffect.SetDropConfig(dropItemConfig, dropChance, dropRangeConfig);
        
        // 掉落效果是瞬时效果，不需要移除条件
        return dropItemEffect;
    }

    public override string GetDebugInfo()
    {
        return $"掉落物品: {dropItemConfig?.itemName ?? "未设置"} (概率:{dropChance:P0})";
    }
}

