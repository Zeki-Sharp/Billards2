using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 伤害系统 - 规则驱动的伤害判断和计算
/// 
/// 【核心职责】：
/// - 注册实体和其伤害规则配置
/// - 监听碰撞、停止等触发事件
/// - 检查规则条件（标签、状态、速度）
/// - 计算基础伤害
/// - 调用 DamageProcessor 应用修改器
/// - 发布最终伤害事件
/// 
/// 【设计原则】：
/// - 规则驱动：伤害条件由配置定义
/// - 事件驱动：通过 GameEventBus 通信
/// - 职责分离：判断层（本类） + 修改层（DamageProcessor）
/// - 无缓存：通过规则自然过滤，避免重复伤害
/// 
/// 【执行顺序】：SYSTEM 层 (-50)
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class DamageSystem : SingletonManager<DamageSystem>
{
    #region 配置
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool showRuleMatching = false;
    
    [Header("系统设置")]
    [SerializeField] private bool systemEnabled = true;
    
    #endregion
    
    #region 私有字段
    
    // 实体伤害配置注册表
    private Dictionary<GameObject, DamageProfile> entityProfiles = new Dictionary<GameObject, DamageProfile>();
    
    // 统计数据
    private int totalCollisions = 0;
    private int totalDamageEvents = 0;
    
    #endregion
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => false;  // 不跨场景保留，每个场景独立管理
    protected override bool EnableDebugLog => enableDebugLog;
    
    protected override void OnManagerCreated()
    {
        // 订阅碰撞事件
        GameEventBus.OnCollision += HandleCollisionEvent;
        
        if (enableDebugLog)
        {
            Debug.Log("[DamageSystem] 初始化完成，订阅碰撞事件");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅
        GameEventBus.OnCollision -= HandleCollisionEvent;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 销毁，统计: 碰撞{totalCollisions}次，伤害事件{totalDamageEvents}次");
        }
    }
    
    #endregion
    
    #region 实体注册
    
    /// <summary>
    /// 注册实体和其伤害配置
    /// </summary>
    public void RegisterEntity(GameObject entity, DamageProfile damageProfile)
    {
        if (entity == null)
        {
            Debug.LogWarning("[DamageSystem] 尝试注册空实体");
            return;
        }
        
        if (damageProfile == null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] {entity.name} 注册时未提供伤害配置（无攻击能力）");
            }
            return;
        }
        
        entityProfiles[entity] = damageProfile;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 注册实体: {entity.name}, 配置: {damageProfile.profileName}, 规则数: {damageProfile.rules.Count}");
        }
    }
    
    /// <summary>
    /// 注销实体
    /// </summary>
    public void UnregisterEntity(GameObject entity)
    {
        if (entity == null) return;
        
        if (entityProfiles.Remove(entity))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] 注销实体: {entity.name}");
            }
        }
    }
    
    /// <summary>
    /// 获取实体的伤害配置
    /// </summary>
    private DamageProfile GetDamageProfile(GameObject entity)
    {
        if (entity == null) return null;
        
        entityProfiles.TryGetValue(entity, out DamageProfile profile);
        return profile;
    }
    
    #endregion
    
    #region 碰撞事件处理
    
    /// <summary>
    /// 处理碰撞事件
    /// </summary>
    private void HandleCollisionEvent(CollisionEvent evt)
    {
        if (!systemEnabled) return;
        
        totalCollisions++;
        
        if (showRuleMatching)
        {
            Debug.Log($"[DamageSystem] 碰撞事件: {evt.Source.name} → {evt.Target.name}, 速度: {evt.Velocity:F2}");
        }
        
        // 获取 source 的伤害配置
        DamageProfile profile = GetDamageProfile(evt.Source);
        
        if (profile == null)
        {
            if (showRuleMatching)
            {
                Debug.Log($"[DamageSystem] {evt.Source.name} 无伤害配置，跳过");
            }
            return;
        }
        
        // 遍历规则，检查匹配
        foreach (var rule in profile.rules)
        {
            if (rule == null) continue;
            if (rule.triggerType != DamageTriggerType.Collision) continue;
            
            // 检查规则条件
            if (CheckRule(rule, evt.Source, evt.Target, evt.Velocity))
            {
                // 计算并发布伤害
                ProcessDamage(rule, evt);
            }
        }
    }
    
    #endregion
    
    #region 规则检查
    
    /// <summary>
    /// 检查规则是否匹配
    /// </summary>
    private bool CheckRule(DamageRuleConfig rule, GameObject source, GameObject target, float velocity)
    {
        // 检查目标标签
        if (!string.IsNullOrEmpty(rule.targetTag))
        {
            if (!target.CompareTag(rule.targetTag))
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：目标标签 {target.tag} != {rule.targetTag}");
                }
                return false;
            }
        }
        
        // 检查来源标签
        if (!string.IsNullOrEmpty(rule.sourceTag))
        {
            if (!source.CompareTag(rule.sourceTag))
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：来源标签 {source.tag} != {rule.sourceTag}");
                }
                return false;
            }
        }
        
        // 检查攻击者状态要求
        if (!string.IsNullOrEmpty(rule.requireSourceState))
        {
            Blackboard blackboard = source.GetBlackboard();
            bool stateActive = blackboard.Get<bool>(rule.requireSourceState);
            
            if (!stateActive)
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：需要状态 '{rule.requireSourceState}'");
                }
                return false;
            }
        }
        
        // 检查目标状态要求
        if (!string.IsNullOrEmpty(rule.requireTargetState))
        {
            Blackboard blackboard = target.GetBlackboard();
            bool stateActive = blackboard.Get<bool>(rule.requireTargetState);
            
            if (!stateActive)
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：需要目标状态 '{rule.requireTargetState}'");
                }
                return false;
            }
        }
        
        // 检查速度要求
        if (rule.minVelocity > 0f)
        {
            if (velocity < rule.minVelocity)
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：速度 {velocity:F2} < {rule.minVelocity:F2}");
                }
                return false;
            }
        }
        
        if (showRuleMatching)
        {
            Debug.Log($"[DamageSystem] ✅ 规则 '{rule.ruleName}' 匹配成功");
        }
        
        return true;
    }
    
    #endregion
    
    #region 伤害计算
    
    /// <summary>
    /// 处理伤害（规则匹配后）
    /// </summary>
    private void ProcessDamage(DamageRuleConfig rule, CollisionEvent evt)
    {
        // 1. 计算基础伤害
        float baseDamage = rule.baseDamage * rule.damageMultiplier;
        
        // 2. 速度加成
        if (rule.velocityMultiplier > 0f)
        {
            baseDamage += evt.Velocity * rule.velocityMultiplier;
        }
        
        // 3. 确定伤害目标（SelfDamage 检查）
        GameObject damageTarget = rule.selfDamage ? evt.Source : evt.Target;
        
        // 4. 创建 AttackData（兼容现有 DamageProcessor）
        AttackData attackData = new AttackData
        {
            Attacker = evt.Source,
            Target = damageTarget,
            Damage = baseDamage,
            AttackType = rule.triggerType.ToString(),
            Position = evt.ContactPoint,
            Direction = evt.ContactNormal,
            AttackTime = Time.time,
            AttackerTag = evt.Source.tag,
            TargetTag = damageTarget.tag,
            HitSpeed = evt.Velocity
        };
        
        // 5. 调用 DamageProcessor 应用修改器
        DamageProcessor.Instance.ProcessDamage(ref attackData);
        
        // 6. 发布最终伤害事件
        PublishDamageEvent(attackData, rule, evt);
        
        totalDamageEvents++;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 伤害处理: {evt.Source.name} → {damageTarget.name}, " +
                     $"规则: {rule.ruleName}, 最终伤害: {attackData.Damage:F1}");
        }
    }
    
    /// <summary>
    /// 发布伤害事件
    /// </summary>
    private void PublishDamageEvent(AttackData attackData, DamageRuleConfig rule, CollisionEvent evt)
    {
        DamageEvent damageEvt = new DamageEvent
        {
            Source = attackData.Attacker,
            Target = attackData.Target,
            FinalDamage = attackData.Damage,
            Type = rule.damageType,
            TriggerType = rule.triggerType,
            HitPosition = evt.ContactPoint,
            HitDirection = evt.ContactNormal,
            VelocityAtHit = evt.Velocity,
            KnockbackForce = rule.knockbackForce,
            StunDuration = rule.stunDuration,
            CanBeBlocked = rule.canBeBlocked,
            RuleName = rule.ruleName,
            EventTime = Time.time
        };
        
        // 发布新的伤害事件
        GameEventBus.PublishDamage(damageEvt);
    }
    
    #endregion
    
    #region 系统控制
    
    /// <summary>
    /// 启用/禁用系统
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        systemEnabled = enabled;
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 系统 {(enabled ? "启用" : "禁用")}");
        }
    }
    
    /// <summary>
    /// 获取系统状态
    /// </summary>
    public bool IsEnabled => systemEnabled;
    
    #endregion
    
    #region 调试和统计
    
    /// <summary>
    /// 获取统计信息
    /// </summary>
    public string GetStatistics()
    {
        return $"[DamageSystem] 统计信息:\n" +
               $"  - 已注册实体: {entityProfiles.Count}\n" +
               $"  - 总碰撞次数: {totalCollisions}\n" +
               $"  - 总伤害事件: {totalDamageEvents}\n" +
               $"  - 系统状态: {(systemEnabled ? "启用" : "禁用")}";
    }
    
    [ContextMenu("显示统计信息")]
    private void ShowStatistics()
    {
        Debug.Log(GetStatistics());
    }
    
    [ContextMenu("显示已注册实体")]
    private void ShowRegisteredEntities()
    {
        if (entityProfiles.Count == 0)
        {
            Debug.Log("[DamageSystem] 无已注册实体");
            return;
        }
        
        string info = $"[DamageSystem] 已注册实体 ({entityProfiles.Count}):\n";
        foreach (var kvp in entityProfiles)
        {
            if (kvp.Key != null && kvp.Value != null)
            {
                info += $"  - {kvp.Key.name}: {kvp.Value.profileName} ({kvp.Value.rules.Count} 规则)\n";
            }
        }
        
        Debug.Log(info);
    }
    
    #endregion
}

