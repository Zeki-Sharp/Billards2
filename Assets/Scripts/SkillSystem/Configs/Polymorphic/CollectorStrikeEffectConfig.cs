using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 收集打击效果配置 - 用于 Inspector 配置
/// 
/// 【配置项】：
/// - 每个掉落物的伤害系数
/// - 调试日志开关
/// 
/// 【使用场景】：
/// - 收集者角色的主动技能
/// - 技能配置：Trigger [PlayerPhaseEnd] + Effect [CollectorStrike]
/// 
/// 【伤害计算】：
/// - 总伤害 = damagePerItem × 本回合拾取数量
/// 
/// 【技能升级支持】：
/// - 可以通过技能等级调整 damagePerItem 的值
/// - 例如：Lv1=5, Lv2=8, Lv3=10
/// </summary>
[System.Serializable]
public class CollectorStrikeEffectConfig : EffectBase
{
    [BoxGroup("伤害配置")]
    [LabelText("每个掉落物的伤害")]
    [Tooltip("拾取每个掉落物对应的伤害值（支持技能升级调整）")]
    [MinValue(1f)]
    public float damagePerItem = 10f;
    
    [BoxGroup("调试")]
    [LabelText("显示调试日志")]
    [Tooltip("是否在Console中显示触发日志")]
    public bool showDebugLog = true;
    
    #region EffectBase 实现
    
    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        // 创建效果实例
        CollectorStrikeEffect effect = new CollectorStrikeEffect();
        
        // 配置参数（角色ID会在实例化后通过 SetTarget 设置）
        effect.Configure(damagePerItem, "", showDebugLog);
        
        effect.Initialize();
        
        if (showDebugLog)
        {
            Debug.Log($"[CollectorStrikeEffectConfig] ✅ 创建收集打击效果：伤害系数={damagePerItem}");
        }
        
        return effect;
    }
    
    public override string GetDebugInfo()
    {
        return $"收集打击 - 每个掉落物伤害:{damagePerItem}";
    }
    
    #endregion
    
    #region Inspector 帮助信息
    
    [BoxGroup("说明")]
    [InfoBox("【伤害计算公式】\n总伤害 = damagePerItem × 本回合拾取的掉落物数量\n\n" +
             "【触发条件】\n- 必须在玩家回合结束时（PlayerPhaseEnd）\n" +
             "- 本回合至少拾取了1个掉落物\n" +
             "- 场上存在至少1个敌人\n\n" +
             "【目标选择】\n自动选择距离收集者角色最近的敌人\n\n" +
             "【技能升级示例】\n" +
             "Lv1: damagePerItem = 5\n" +
             "Lv2: damagePerItem = 8\n" +
             "Lv3: damagePerItem = 10", 
             InfoMessageType.Info)]
    [HideInInspector]
    public bool infoDisplay;
    
    #endregion
}

