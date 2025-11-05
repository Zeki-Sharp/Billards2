using UnityEngine;
using Sirenix.OdinInspector;

/// <summary>
/// 伤害触发器 - 监听伤害事件，根据条件触发技能
/// 
/// 【核心功能】：
/// - 监听 OnDamage 事件
/// - 过滤伤害类型（Collision, Stopped, Interval）
/// - 过滤目标标签
/// - 检查角色归属
/// 
/// 【使用场景】：
/// - 只在特定类型的伤害时触发效果（如范围攻击触发点燃）
/// - 只对特定目标生效（如只对敌人）
/// 
/// 【配置方式】：
/// - 通过 DamageTriggerConfig 在 Inspector 中配置
/// - DamageTriggerConfig.CreateTrigger() 创建实例
/// </summary>
public class DamageTrigger : ITrigger
{
    // ✅ 由 DamageTriggerConfig 设置
    public DamageTriggerType[] triggerTypes;
    public string targetTag;
    public bool showDebugLog;
    
    public string TriggerName => "DamageTrigger";
    
    // ✅ 多角色系统：技能归属的角色ID
    private string ownerCharacterID;
    
    /// <summary>
    /// ✅ 多角色系统：设置触发器归属的角色ID
    /// </summary>
    public void SetOwner(string characterID)
    {
        ownerCharacterID = characterID;
    }
    
    /// <summary>
    /// 初始化触发器
    /// </summary>
    public void Initialize()
    {
        // 不需要特殊初始化
    }
    
    /// <summary>
    /// 检查是否检测到符合条件的伤害事件
    /// </summary>
    public bool CheckEvent(SkillArgs args)
    {
        // 检查 DamageEvent
        if (args.TryGetEventData<DamageEvent>(out var damageEvent))
        {
            // 1. 检查是否在玩家回合（只有玩家造成的伤害才触发技能）
            var gameFlowController = GameFlowController.Instance;
            if (gameFlowController == null || !gameFlowController.IsPlayerPhase)
            {
                return false;
            }
            
            // 2. 检查伤害类型是否匹配
            bool triggerTypeMatches = false;
            foreach (var triggerType in triggerTypes)
            {
                if (damageEvent.TriggerType == triggerType)
                {
                    triggerTypeMatches = true;
                    break;
                }
            }
            
            if (!triggerTypeMatches)
            {
                if (showDebugLog)
                {
                    Debug.Log($"[DamageTrigger] 伤害类型不匹配：{damageEvent.TriggerType}，需要：{string.Join(",", triggerTypes)}");
                }
                return false;
            }
            
            // 3. ✅ 多角色系统：检查伤害来源是否是归属角色
            if (!TriggerHelper.CheckEventSource(damageEvent, ownerCharacterID, showDebugLog))
            {
                if (showDebugLog)
                {
                    Debug.Log($"[DamageTrigger] 伤害来源不是归属角色：{ownerCharacterID}");
                }
                return false;
            }
            
            // 4. 检查目标标签是否匹配
            if (!string.IsNullOrEmpty(targetTag))
            {
                if (damageEvent.Target == null || !damageEvent.Target.CompareTag(targetTag))
                {
                    if (showDebugLog)
                    {
                        Debug.Log($"[DamageTrigger] 目标标签不匹配：需要{targetTag}");
                    }
                    return false;
                }
            }
            
            if (showDebugLog)
            {
                Debug.Log($"[DamageTrigger] ✅ 触发条件满足：{damageEvent.TriggerType}类型伤害，目标：{damageEvent.Target?.name}");
            }
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 重置触发器状态
    /// </summary>
    public void Reset()
    {
        // 不需要特殊重置逻辑
    }
}

