using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 注册伤害修改器效果配置 - 用于状态条件增伤技能
/// 
/// 【使用场景】：
/// - 点燃惩戒：对点燃目标造成额外伤害
/// - 剧毒强化：对中毒目标造成额外伤害
/// - 任何"对特定状态目标增伤"的被动技能
/// 
/// 【配置步骤】：
/// 1. 在 SkillConfig 的 effectConfig 中选择此类型
/// 2. 拖拽要检测的状态数据（如 BurningStatusData）
/// 3. 选择增伤模式（百分比/固定值）
/// 4. 设置增伤数值
/// </summary>
[System.Serializable]
public class RegisterDamageModifierEffectConfig : EffectBase
{
    [BoxGroup("状态检测")]
    [LabelText("目标状态")]
    [Tooltip("要检测的状态数据（拖拽 TurnBasedStatusData SO）")]
    [Required]
    [AssetsOnly]
    public TurnBasedStatusData targetStatusData;
    
    [BoxGroup("伤害增加")]
    [LabelText("增伤模式")]
    [Tooltip("百分比：伤害 × 倍率，固定值：伤害 + 固定值")]
    public DamageIncreaseType increaseType = DamageIncreaseType.Percentage;
    
    [BoxGroup("伤害增加")]
    [LabelText("伤害倍率")]
    [Tooltip("百分比模式使用（如 1.5 = +50%伤害）")]
    [ShowIf("increaseType", DamageIncreaseType.Percentage)]
    [MinValue(1.0f)]
    public float damageMultiplier = 1.5f;
    
    [BoxGroup("伤害增加")]
    [LabelText("固定伤害加成")]
    [Tooltip("固定值模式使用（如 +10 点伤害）")]
    [ShowIf("increaseType", DamageIncreaseType.Fixed)]
    [MinValue(0f)]
    public float fixedDamageBonus = 10f;
    
    [BoxGroup("调试")]
    [LabelText("显示日志")]
    [Tooltip("是否在 Console 显示调试日志")]
    public bool showDebugLog = true;
    
    /// <summary>
    /// 创建效果实例
    /// </summary>
    public override IEffect CreateEffect(IEffectRemovalCondition effectRemovalCondition = null)
    {
        var effect = new RegisterDamageModifierEffect
        {
            targetStatusData = this.targetStatusData,
            increaseType = this.increaseType,
            damageMultiplier = this.damageMultiplier,
            fixedDamageBonus = this.fixedDamageBonus,
            showDebugLog = this.showDebugLog
        };
        
        effect.Initialize();
        return effect;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public override string GetDebugInfo()
    {
        string statusName = targetStatusData != null ? targetStatusData.displayName : "未配置";
        string increaseDesc = increaseType == DamageIncreaseType.Percentage 
            ? $"×{damageMultiplier} (+{(damageMultiplier - 1) * 100:F0}%)"
            : $"+{fixedDamageBonus}";
        
        return $"注册伤害修改器 - 检测状态:{statusName}, 增伤:{increaseDesc}";
    }
    
    /// <summary>
    /// 验证配置
    /// </summary>
    [Button("验证配置")]
    [GUIColor(0.4f, 0.8f, 1f)]
    private void ValidateConfiguration()
    {
        bool isValid = true;
        
        if (targetStatusData == null)
        {
            Debug.LogError("RegisterDamageModifierEffectConfig: 未配置 targetStatusData！");
            isValid = false;
        }
        
        if (increaseType == DamageIncreaseType.Percentage && damageMultiplier <= 0f)
        {
            Debug.LogError($"RegisterDamageModifierEffectConfig: 百分比模式下 damageMultiplier 应该 > 0，当前: {damageMultiplier}");
            isValid = false;
        }
        
        if (increaseType == DamageIncreaseType.Fixed && fixedDamageBonus <= 0f)
        {
            Debug.LogError($"RegisterDamageModifierEffectConfig: 固定值模式下 fixedDamageBonus 应该 > 0，当前: {fixedDamageBonus}");
            isValid = false;
        }
        
        if (isValid)
        {
            Debug.Log($"✅ 配置验证通过！{GetDebugInfo()}");
        }
    }
}

