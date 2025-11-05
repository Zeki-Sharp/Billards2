using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 回合制状态效果配置 - 施加回合制状态（点燃、中毒等）
/// 
/// 【使用场景】：
/// - 点燃：持续造成火焰伤害
/// - 中毒：持续造成毒伤害
/// - 流血：持续造成物理伤害
/// 
/// 【配置示例】：
/// - statusData = BurningStatusData → 施加点燃状态
/// </summary>
[System.Serializable]
public class TurnBasedStatusEffectConfig : EffectBase
{
    [LabelText("状态数据")]
    [Tooltip("要施加的状态配置（如点燃、中毒等）")]
    [Required]
    [AssetSelector(Paths = "Assets/Data/StatusEffects")]
    public TurnBasedStatusData statusData;
    
    [LabelText("显示调试日志")]
    [Tooltip("是否在Console中显示日志")]
    public bool showDebugLog = true;
    
    /// <summary>
    /// 创建效果实例
    /// </summary>
    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var effect = new TurnBasedStatusEffect
        {
            statusData = this.statusData,
            showDebugLog = this.showDebugLog
        };
        
        // 回合制状态由组件自己管理生命周期，不需要移除条件
        return effect;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        if (statusData == null)
        {
            return "回合制状态: 未配置";
        }
        
        return $"回合制状态: {statusData.displayName} ({statusData.baseDurationInTurns}回合，{statusData.baseDamagePerTurn}伤害/回合)";
    }
}

