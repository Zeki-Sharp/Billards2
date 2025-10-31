using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 技能等级配置 - 定义单个技能等级的所有配置
/// 包含触发器、条件、效果、重置条件等完整配置
/// </summary>
[System.Serializable]
public class SkillLevelConfig
{
    [BoxGroup("等级基本信息")]
    [LabelText("等级")]
    [ReadOnly]
    [Tooltip("等级编号（自动分配，不可手动修改）")]
    public int level = 1;
    
    [BoxGroup("等级基本信息")]
    [LabelText("是否激活")]
    [Tooltip("该等级是否可用")]
    public bool isActive = true;
    
    [BoxGroup("等级基本信息")]
    [LabelText("等级描述")]
    [Tooltip("该等级的特殊说明")]
    [TextArea(2, 3)]
    public string levelDescription = "";
    
    [BoxGroup("触发器配置")]
    [LabelText("触发器配置")]
    [Tooltip("什么时候触发技能（如：击杀敌人、碰撞墙壁等）")]
    [SerializeReference]
    [InlineProperty]
    public TriggerBase triggerConfig;
    
    [BoxGroup("条件配置")]
    [LabelText("条件配置")]
    [Tooltip("触发条件（如：生命值满血、击杀数量等）")]
    [ShowIf("@!(triggerConfig is AlwaysTrueTriggerConfig)")]
    [InfoBox("AlwaysTrue触发器不需要条件配置，会始终触发", InfoMessageType.Info, "@triggerConfig is AlwaysTrueTriggerConfig")]
    [Required]
    public ConditionConfig conditionConfig = new ConditionConfig();
    
    [BoxGroup("效果配置")]
    [LabelText("效果配置")]
    [Tooltip("产生什么效果（如：伤害提升、恢复生命值等）")]
    [SerializeReference]
    public EffectBase effectConfig;
    
    [BoxGroup("重置条件配置")]
    [LabelText("重置条件配置")]
    [Tooltip("什么时候可以再次触发技能（所有技能都需要）")]
    [SerializeReference]
    public ResetConditionBase resetConditionConfig;
    
    [BoxGroup("效果移除配置")]
    [LabelText("效果移除配置")]
    [Tooltip("持续效果何时移除（仅持续效果需要配置）")]
    [ShowIf("@effectConfig is StatModifierEffectConfig")]
    [InfoBox("只有持续效果（属性修改）才需要配置移除条件", InfoMessageType.Info, "@effectConfig is StatModifierEffectConfig")]
    [SerializeReference]
    public EffectRemovalConditionBase effectRemovalConfig;
    
    /// <summary>
    /// 创建技能等级实例
    /// </summary>
    /// <param name="parentSkillName">父技能名称</param>
    /// <returns>技能等级实例</returns>
    public SkillLevelInstance CreateLevelInstance(string parentSkillName)
    {
        if (!isActive)
        {
            Debug.LogWarning($"技能等级 {parentSkillName} Lv{level} 未激活");
            return null;
        }
        
        // 创建组件实例
        var trigger = triggerConfig?.CreateTrigger();
        var condition = conditionConfig?.CreateCondition();
        var resetCondition = resetConditionConfig?.CreateResetCondition();
        
        // 根据效果类型创建相应的移除条件
        IEffectRemovalCondition effectRemovalCondition = null;
        if (IsPropertyEffect())
        {
            effectRemovalCondition = effectRemovalConfig?.CreateEffectRemovalCondition();
        }
        
        var effect = effectConfig?.CreateEffect(effectRemovalCondition);
        
        if (trigger == null || condition == null || effect == null || resetCondition == null)
        {
            Debug.LogError($"技能等级 {parentSkillName} Lv{level} 基础组件创建失败");
            return null;
        }
        
        // 持续效果需要效果移除条件
        if (IsPropertyEffect() && effectRemovalCondition == null)
        {
            Debug.LogError($"技能等级 {parentSkillName} Lv{level} 缺少效果移除配置");
            return null;
        }
        
        var levelInstance = new SkillLevelInstance(this, parentSkillName, trigger, condition, effect, resetCondition, effectRemovalCondition);
        return levelInstance;
    }
    
    /// <summary>
    /// 判断是否为持续效果（PropertyEffect）
    /// </summary>
    private bool IsPropertyEffect()
    {
        return effectConfig is StatModifierEffectConfig;
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public bool IsValid()
    {
        bool basicValid = level >= 0 && 
                         triggerConfig != null && 
                         conditionConfig != null && 
                         effectConfig != null;
        
        if (!basicValid) 
        {
            return false;
        }
        
        // 检查重置条件配置
        bool resetConditionValid = resetConditionConfig != null;
        
        // 检查效果移除配置（仅持续效果需要）
        bool effectRemovalValid = true;
        if (IsPropertyEffect())
        {
            effectRemovalValid = effectRemovalConfig != null;
        }
        
        return resetConditionValid && effectRemovalValid;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"技能等级: Lv{level}\n" +
                     $"- 激活: {isActive}\n" +
                     $"- 描述: {levelDescription}\n" +
                     $"- 触发器: {triggerConfig?.GetDebugInfo()}\n" +
                     $"- 条件: {conditionConfig?.GetDebugInfo()}\n" +
                     $"- 效果: {effectConfig?.GetDebugInfo()}\n" +
                     $"- 重置条件: {resetConditionConfig?.GetDebugInfo()}";
        
        if (IsPropertyEffect())
        {
            info += $"\n- 效果移除: {effectRemovalConfig?.GetDebugInfo()}";
        }
        
        return info;
    }
}
