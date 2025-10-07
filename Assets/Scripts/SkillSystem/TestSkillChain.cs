using UnityEngine;

/// <summary>
/// 技能链路测试脚本 - 技能系统第一阶段最小验证
/// 测试：CollisionTrigger + CountCondition + StatModifierEffect
/// 目标：碰撞3次后攻击力+50%
/// </summary>
public class TestSkillChain : MonoBehaviour
{
    [Header("技能组件")]
    private ITrigger collisionTrigger;
    private ICondition countCondition;
    private IEffect statModifierEffect;
    
    [Header("测试参数")]
    public int requiredCollisions = 2; // 修改为2次碰撞
    public float damageMultiplier = 1.5f; // +50%
    
    void Start()
    {
        InitializeSkillChain();
        SubscribeToEvents();
    }
    
    /// <summary>
    /// 初始化技能链路
    /// </summary>
    void InitializeSkillChain()
    {
        // 创建触发器
        collisionTrigger = new CollisionTrigger();
        collisionTrigger.Initialize();
        
        // 创建条件
        countCondition = new CountCondition();
        ((CountCondition)countCondition).SetRequiredCount(requiredCollisions);
        countCondition.Initialize();
        
        // 创建效果
        statModifierEffect = new StatModifierEffect();
        ((StatModifierEffect)statModifierEffect).SetModifier("Damage", damageMultiplier);
        // 创建移除条件：玩家回合结束时移除
        var removalCondition = new OnPlayerPhaseEndedCondition();
        removalCondition.Initialize();
        ((StatModifierEffect)statModifierEffect).SetRemovalCondition(removalCondition);
        statModifierEffect.Initialize();
        
        Debug.Log($"[TestSkillChain] 技能链路初始化完成");
        Debug.Log($"- 触发器: {collisionTrigger.TriggerName}");
        Debug.Log($"- 条件: {countCondition.ConditionName} (需要{requiredCollisions}次碰撞)");
        Debug.Log($"- 效果: {statModifierEffect.EffectName} (攻击力x{damageMultiplier})");
    }
    
    /// <summary>
    /// 订阅攻击事件
    /// </summary>
    void SubscribeToEvents()
    {
        GameEventBus.OnAttack += HandleAttackEvent;
        GameEventBus.OnChargingStarted += OnNewShotStarted;
        Debug.Log("[TestSkillChain] 已订阅攻击事件和发射开始事件");
    }
    
    /// <summary>
    /// 处理攻击事件
    /// </summary>
    void HandleAttackEvent(AttackData attackData)
    {
        Debug.Log($"[TestSkillChain] 收到攻击事件: {attackData.AttackType} at {attackData.Position}");
        
        // 第一步：检查触发器是否检测到事件
        bool eventDetected = collisionTrigger.CheckEvent(attackData);
        if (!eventDetected)
        {
            Debug.Log("[TestSkillChain] 触发器未检测到碰撞事件，跳过");
            return;
        }
        
        Debug.Log("[TestSkillChain] 触发器检测到碰撞事件");
        
        // 第二步：检查条件是否满足
        bool conditionMet = countCondition.CheckCondition(attackData);
        if (!conditionMet)
        {
            Debug.Log("[TestSkillChain] 条件未满足，技能不触发");
            return;
        }
        
        Debug.Log("[TestSkillChain] 条件满足，开始执行技能效果");
        
        // 第三步：执行效果
        bool effectExecuted = statModifierEffect.ExecuteEffect(attackData);
        if (effectExecuted)
        {
            Debug.Log("[TestSkillChain] 技能效果执行成功！攻击力已提升");
        }
        else
        {
            Debug.LogError("[TestSkillChain] 技能效果执行失败");
        }
    }
    
    /// <summary>
    /// 处理新发射开始事件 - 重置技能状态
    /// </summary>
    void OnNewShotStarted()
    {
        Debug.Log("[TestSkillChain] 检测到新发射开始，重置技能状态");
        
        // 重置所有技能组件状态
        collisionTrigger?.Reset();
        countCondition?.Reset();
        statModifierEffect?.Reset();
        
        Debug.Log("[TestSkillChain] 技能状态重置完成，可以重新触发");
    }
    
    /// <summary>
    /// 重置技能链路（用于测试）
    /// </summary>
    [ContextMenu("重置技能链路")]
    public void ResetSkillChain()
    {
        collisionTrigger?.Reset();
        countCondition?.Reset();
        statModifierEffect?.Reset();
        
        Debug.Log("[TestSkillChain] 技能链路已重置");
    }
    
    void OnDestroy()
    {
        // 取消订阅
        GameEventBus.OnAttack -= HandleAttackEvent;
        GameEventBus.OnChargingStarted -= OnNewShotStarted;
        Debug.Log("[TestSkillChain] 已取消事件订阅");
    }
}
