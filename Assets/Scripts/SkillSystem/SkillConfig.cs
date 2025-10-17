using UnityEngine;
using Sirenix.OdinInspector;
using System.Collections.Generic;
#if UNITY_EDITOR
using System.Linq;
#endif

/// <summary>
/// 技能配置 ScriptableObject - 用于在 Inspector 中配置技能
/// 支持可视化配置，替代硬编码的技能定义
/// </summary>
[CreateAssetMenu(fileName = "SkillConfig", menuName = "Game/Skill Config")]
public class SkillConfig : ScriptableObject
{
    [BoxGroup("技能基本信息")]
    [LabelText("技能名称")]
    [Tooltip("技能名称")]
    public string skillName = "碰撞连击";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能描述")]
    [Tooltip("技能描述")]
    [TextArea(3, 5)]
    public string description = "碰撞敌人2次后，攻击力提升100%";
    
    [BoxGroup("技能基本信息")]
    [LabelText("技能标签")]
    [Tooltip("技能所属的标签，用于区分通用技能和角色专属技能")]
    [ValueDropdown("GetAvailableTags")]
    public string skillTag = "default";
    
    // 技能图标暂时移除，简化配置界面
    // [BoxGroup("技能基本信息")]
    // [LabelText("技能图标")]
    // [Tooltip("技能图标")]
    // public Sprite skillIcon;
    
    [BoxGroup("触发器配置")]
    [LabelText("触发器配置")]
    [Tooltip("什么时候触发技能（如：击杀敌人、碰撞墙壁等）")]
    [Required]
    public TriggerConfig triggerConfig = new TriggerConfig();
    
    [BoxGroup("条件配置")]
    [LabelText("条件配置")]
    [Tooltip("触发条件（如：生命值满血、击杀数量等）")]
    [ShowIf("@triggerConfig.triggerType != TriggerType.AlwaysTrue")]
    [InfoBox("AlwaysTrue触发器不需要条件配置，会始终触发", InfoMessageType.Info, "@triggerConfig.triggerType == TriggerType.AlwaysTrue")]
    [Required]
    public ConditionConfig conditionConfig = new ConditionConfig();
    
    [BoxGroup("效果配置")]
    [LabelText("效果配置")]
    [Tooltip("产生什么效果（如：伤害提升、恢复生命值等）")]
    [Required]
    public SkillEffectConfig effectConfig = new SkillEffectConfig();
    
    [BoxGroup("重置条件配置")]
    [LabelText("重置条件配置")]
    [Tooltip("什么时候可以再次触发技能（所有技能都需要）")]
    [Required]
    public ResetConditionConfig resetConditionConfig = new ResetConditionConfig();
    
    [BoxGroup("效果移除配置")]
    [LabelText("效果移除配置")]
    [Tooltip("持续效果何时移除（仅持续效果需要配置）")]
    [ShowIf("@effectConfig.effectType == SkillEffectType.StatModifier")]
    [InfoBox("只有持续效果（属性修改）才需要配置移除条件", InfoMessageType.Info, "@effectConfig.effectType == SkillEffectType.StatModifier")]
    public EffectRemovalConfig effectRemovalConfig = new EffectRemovalConfig();
    
    [BoxGroup("技能属性")]
    [LabelText("技能类型")]
    [Tooltip("技能类型")]
    public SkillType skillType = SkillType.Passive;
    
    [BoxGroup("技能属性")]
    [LabelText("所需等级")]
    [Tooltip("所需等级")]
    [MinValue(1)]
    public int requiredLevel = 1;
    
    [BoxGroup("技能属性")]
    [LabelText("是否激活")]
    [Tooltip("是否激活")]
    public bool isActive = true;
    
    /// <summary>
    /// 创建技能实例
    /// </summary>
    public virtual SkillInstance CreateSkillInstance()
    {
        if (!isActive)
        {
            Debug.LogWarning($"技能 {skillName} 未激活");
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
            Debug.LogError($"技能 {skillName} 基础组件创建失败");
            return null;
        }
        
        // 持续效果需要效果移除条件
        if (IsPropertyEffect() && effectRemovalCondition == null)
        {
            Debug.LogError($"技能 {skillName} 缺少效果移除配置");
            return null;
        }
        
        var skillInstance = new SkillInstance(this, trigger, condition, effect, resetCondition, effectRemovalCondition);
        Debug.Log($"[SkillConfig] ✅ 技能实例创建成功 - 技能: {skillName}, 重置条件: {resetCondition?.GetType().Name}, 效果移除条件: {effectRemovalCondition?.GetType().Name}");
        return skillInstance;
    }
    
    /// <summary>
    /// 判断是否为持续效果（PropertyEffect）
    /// </summary>
    private bool IsPropertyEffect()
    {
        return effectConfig?.effectType == SkillEffectType.StatModifier;
    }
    
    /// <summary>
    /// 验证配置是否有效
    /// </summary>
    public virtual bool IsValid()
    {
        bool basicValid = !string.IsNullOrEmpty(skillName) && 
                         triggerConfig != null && 
                         conditionConfig != null && 
                         effectConfig != null;
        
        if (!basicValid) return false;
        
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
    public virtual string GetDebugInfo()
    {
        string info = $"技能: {skillName}\n" +
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
    
#if UNITY_EDITOR
    /// <summary>
    /// 获取所有可用的技能标签
    /// </summary>
    private IEnumerable<string> GetAvailableTags()
    {
        var tags = new List<string>();
        
        // 添加固定标签
        tags.Add("default");
        tags.Add("common");
        
        // 尝试从 Resources 加载角色选择数据
        var characterSelectionData = UnityEngine.Resources.Load<CharacterSelectionData>("Data/CharacterSelectionData");
        if (characterSelectionData != null && characterSelectionData.availableCharacters != null)
        {
            // 添加所有角色名称
            foreach (var character in characterSelectionData.availableCharacters)
            {
                if (character != null && !string.IsNullOrEmpty(character.playerName))
                {
                    tags.Add(character.playerName);
                }
            }
        }
        else
        {
            // 如果无法加载角色选择数据，尝试从 Resources/Data/Player 目录加载所有 PlayerData
            var allPlayerData = UnityEngine.Resources.LoadAll<PlayerData>("Data/Player");
            if (allPlayerData != null && allPlayerData.Length > 0)
            {
                foreach (var playerData in allPlayerData)
                {
                    if (playerData != null && !string.IsNullOrEmpty(playerData.playerName))
                    {
                        tags.Add(playerData.playerName);
                    }
                }
            }
        }
        
        return tags.Distinct().OrderBy(t => t); // 去重并排序
    }
#endif
}

/// <summary>
/// 技能类型枚举
/// </summary>
public enum SkillType
{
    Passive,    // 被动技能
    Active,     // 主动技能（暂未实现）
    Ultimate    // 终极技能（暂未实现）
}

/// <summary>
/// 技能实例 - 包含配置和运行时组件
/// </summary>
public class SkillInstance
{
    public SkillConfig config;
    public ITrigger trigger;
    public ICondition condition;
    public IEffect effect;
    
    // 新的分离组件
    public IResetCondition resetCondition;  // 所有技能都有
    public IEffectRemovalCondition effectRemovalCondition; // 只有PropertyEffect有
    
    public SkillInstance(SkillConfig config, ITrigger trigger, ICondition condition, IEffect effect, 
                        IResetCondition resetCondition, 
                        IEffectRemovalCondition effectRemovalCondition = null)
    {
        this.config = config;
        this.trigger = trigger;
        this.condition = condition;
        this.effect = effect;
        this.resetCondition = resetCondition;
        this.effectRemovalCondition = effectRemovalCondition;
        
        // 初始化所有组件
        this.trigger?.Initialize();
        this.condition?.Initialize();
        this.effect?.Initialize();
        this.resetCondition?.Initialize();
        this.effectRemovalCondition?.Initialize();
    }
    
    /// <summary>
    /// 重置技能状态
    /// </summary>
    public void Reset()
    {
        trigger?.Reset();
        condition?.Reset();
        effect?.Reset();
        resetCondition?.Reset();
        effectRemovalCondition?.Reset();
    }
    
    /// <summary>
    /// 处理事件
    /// </summary>
    public bool ProcessEvent(object eventData)
    {
        Debug.Log($"[SkillInstance] 🔍 开始处理事件 - 技能: {config.skillName}, 时间: {Time.time:F2}, 事件类型: {eventData?.GetType().Name}");
        
        // 第一步：检查触发器是否检测到事件
        bool eventDetected = trigger.CheckEvent(eventData);
        if (!eventDetected)
        {
            Debug.Log($"[SkillInstance] ❌ 触发器未检测到事件 - 技能: {config.skillName}, 触发器: {trigger?.GetType().Name}");
            return false;
        }
        Debug.Log($"[SkillInstance] ✅ 触发器检测到事件 - 技能: {config.skillName}, 触发器: {trigger?.GetType().Name}");
        
        // 第二步：检查条件是否满足
        bool conditionMet = condition.CheckCondition(eventData);
        if (!conditionMet)
        {
            Debug.Log($"[SkillInstance] ❌ 条件未满足 - 技能: {config.skillName}, 条件: {condition?.GetType().Name}");
            return false;
        }
        Debug.Log($"[SkillInstance] ✅ 条件满足 - 技能: {config.skillName}, 条件: {condition?.GetType().Name}");
        
        // 第三步：执行效果
        Debug.Log($"[SkillInstance] 🎯 准备执行效果 - 技能: {config.skillName}, 效果: {effect?.GetType().Name}");
        bool effectExecuted = effect.ExecuteEffect(eventData);
        
        if (effectExecuted)
        {
            Debug.Log($"[SkillInstance] 🎯 技能 {config.skillName} 执行成功！");
            
            // 第四步：检查重置条件
            if (resetCondition != null)
            {
                bool shouldReset = resetCondition.ShouldReset(eventData);
                Debug.Log($"[SkillInstance] 🔄 重置条件检查: {shouldReset} - 技能: {config.skillName}");
                
                if (shouldReset)
                {
                    // 立即重置条件
                    condition.Reset();
                    Debug.Log($"[SkillInstance] 🔄 重置条件满足，立即重置触发条件 - {config.skillName}");
                }
            }
            else
            {
                Debug.LogWarning($"[SkillInstance] ⚠️ 重置条件为空 - 技能: {config.skillName}");
            }
        }
        else
        {
            Debug.LogError($"[SkillInstance] ❌ 技能执行失败 - 技能: {config.skillName}");
        }
        
        return effectExecuted;
    }
    
    /// <summary>
    /// 处理回合结束事件
    /// </summary>
    /// <param name="eventData">回合结束事件数据</param>
    public void HandlePhaseEndEvent(object eventData)
    {
        if (resetCondition != null)
        {
            bool shouldReset = resetCondition.ShouldReset(eventData);
            if (shouldReset)
            {
                condition.Reset();  // 重置触发条件
                effect.Reset();     // 重置效果状态
                Debug.Log($"[SkillInstance] 🔄 回合结束，重置触发条件和效果 - {config.skillName}");
            }
        }
    }
}
