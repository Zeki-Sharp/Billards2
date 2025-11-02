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
        
        // 订阅停止事件
        GameEventBus.OnStopped += HandleStoppedEvent;
        
        if (enableDebugLog)
        {
            Debug.Log("[DamageSystem] 初始化完成，订阅碰撞和停止事件");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅
        GameEventBus.OnCollision -= HandleCollisionEvent;
        GameEventBus.OnStopped -= HandleStoppedEvent;
        
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
    /// 获取实体的伤害配置（支持向上查找父级）
    /// </summary>
    private DamageProfile GetDamageProfile(GameObject entity)
    {
        if (entity == null) return null;
        
        // 先尝试从当前对象获取
        if (entityProfiles.TryGetValue(entity, out DamageProfile profile))
        {
            Debug.Log($"[DamageSystem] 在 {entity.name} 找到伤害配置: {profile.profileName}");
            return profile;
        }
        
        // 如果没有，尝试从父级获取
        Transform current = entity.transform.parent;
        while (current != null)
        {
            if (entityProfiles.TryGetValue(current.gameObject, out profile))
            {
                Debug.Log($"[DamageSystem] 在父级 {current.name} 找到伤害配置: {profile.profileName}");
                return profile;
            }
            current = current.parent;
        }
        
        Debug.LogWarning($"[DamageSystem] ⚠️ {entity.name} 及其所有父级都未注册伤害配置");
        return null;
    }
    
    #endregion
    
    #region 碰撞事件处理
    
    /// <summary>
    /// 处理碰撞事件
    /// </summary>
    private void HandleCollisionEvent(CollisionEvent evt)
    {
        if (!systemEnabled)
        {
            return;
        }
        
        totalCollisions++;
        
        // 获取 source 的伤害配置
        DamageProfile profile = GetDamageProfile(evt.Source);
        
        if (profile == null)
        {
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
    
    /// <summary>
    /// 处理停止事件（球停止范围攻击）
    /// </summary>
    private void HandleStoppedEvent(StoppedEvent evt)
    {
        if (!systemEnabled) return;
        
        // 获取 source 的伤害配置
        DamageProfile profile = GetDamageProfile(evt.Source);
        
        if (profile == null) return;
        
        // 遍历规则，检查匹配
        foreach (var rule in profile.rules)
        {
            if (rule == null) continue;
            if (rule.triggerType != DamageTriggerType.Stopped) continue;
            
            // 对于 Stopped 类型，需要范围检测
            ProcessStoppedDamage(rule, evt);
        }
    }
    
    /// <summary>
    /// 处理停止伤害（范围检测）
    /// </summary>
    private void ProcessStoppedDamage(DamageRuleConfig rule, StoppedEvent evt)
    {
        // 确定攻击范围
        float range = rule.attackRange;
        
        // 如果规则未配置范围，从 PlayerData 读取
        if (range <= 0f)
        {
            var playerBehavior = evt.Source.GetComponent<PlayerBehavior>();
            if (playerBehavior != null && playerBehavior.PlayerData != null)
            {
                range = playerBehavior.PlayerData.areaRadius;
            }
            else
            {
                Debug.LogWarning($"[DamageSystem] Stopped 规则 '{rule.ruleName}' 未配置范围，且无法从 PlayerData 读取");
                return;
            }
        }
        
        // 使用 Physics2D.OverlapCircleAll 检测范围内的目标
        Collider2D[] colliders = Physics2D.OverlapCircleAll(evt.StoppedPosition, range);
        
        foreach (var collider in colliders)
        {
            GameObject target = collider.gameObject;
            
            // 检查目标标签
            if (!string.IsNullOrEmpty(rule.targetTag))
            {
                if (!target.CompareTag(rule.targetTag)) continue;
            }
            
            // 检查来源标签
            if (!string.IsNullOrEmpty(rule.sourceTag))
            {
                if (!evt.Source.CompareTag(rule.sourceTag)) continue;
            }
            
            // 创建模拟的碰撞事件用于伤害计算
            CollisionEvent collisionEvt = new CollisionEvent
            {
                Source = evt.Source,
                Target = target,
                ContactPoint = evt.StoppedPosition,
                ContactNormal = (target.transform.position - (Vector3)evt.StoppedPosition).normalized,
                Velocity = 0f,
                CollisionTime = evt.StoppedTime
            };
            
            ProcessDamage(rule, collisionEvt);
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
                return false;
            }
        }
        
        // 检查来源标签
        if (!string.IsNullOrEmpty(rule.sourceTag))
        {
            if (!source.CompareTag(rule.sourceTag))
            {
                return false;
            }
        }
        
        // 检查攻击者状态要求
        if (!string.IsNullOrEmpty(rule.requireSourceState))
        {
            // 尝试获取 Blackboard，如果没有则向上查找父级
            Blackboard blackboard = GetBlackboard(source);
            
            if (blackboard == null)
            {
                return false;
            }
            
            bool stateActive = blackboard.Get<bool>(rule.requireSourceState);
            
            if (!stateActive)
            {
                return false;
            }
        }
        
        // 检查目标状态要求
        if (!string.IsNullOrEmpty(rule.requireTargetState))
        {
            // 尝试获取 Blackboard，如果没有则向上查找父级
            Blackboard blackboard = GetBlackboard(target);
            
            if (blackboard == null)
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：{target.name} 及其父级无 Blackboard");
                }
                return false;
            }
            
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
        
        // 检查目标"不应处于"的状态要求（例如：陷阱无敌、无敌技能）
        if (!string.IsNullOrEmpty(rule.requireTargetNotState))
        {
            Blackboard blackboard = GetBlackboard(target);
            
            // 如果目标没有 Blackboard，认为没有该状态，规则通过
            if (blackboard != null)
            {
                // 尝试获取状态值，如果状态存在且为 true，则规则不匹配
                if (blackboard.TryGet<bool>(rule.requireTargetNotState, out bool stateValue) && stateValue)
                {
                    if (showRuleMatching)
                    {
                        Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：目标处于 '{rule.requireTargetNotState}' 状态（无敌）");
                    }
                    return false;
                }
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
        
        return true;
    }
    
    /// <summary>
    /// 获取 Blackboard，如果当前对象没有则向上查找父级
    /// </summary>
    private Blackboard GetBlackboard(GameObject obj)
    {
        if (obj == null) return null;
        
        // 使用 TryGetBlackboard，不自动创建
        Blackboard blackboard = obj.TryGetBlackboard();
        
        if (blackboard != null)
        {
            return blackboard;
        }
        
        // 如果没有，向上查找父级
        Transform current = obj.transform.parent;
        while (current != null)
        {
            blackboard = current.gameObject.TryGetBlackboard();
            if (blackboard != null)
            {
                return blackboard;
            }
            
            current = current.parent;
        }
        
        return null;
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
        
        // 4. 确定攻击者（SelfDamage 时，攻击者应该是 Target 而不是 Source）
        // 例如：玩家撞陷阱，Source=Player, Target=AttackRange, selfDamage=true
        // → 伤害目标是 Player，攻击者应该是 AttackRange（敌人）
        GameObject attacker = rule.selfDamage ? evt.Target : evt.Source;
        
        // 5. 创建 AttackData（兼容现有 DamageProcessor）
        AttackData attackData = new AttackData
        {
            Attacker = attacker,  // ✅ 修复：使用正确的攻击者
            Target = damageTarget,
            Damage = baseDamage,
            AttackType = rule.triggerType.ToString(),
            Position = evt.ContactPoint,
            Direction = evt.ContactNormal,
            AttackTime = Time.time,
            AttackerTag = attacker.tag,  // ✅ 使用攻击者的 Tag
            TargetTag = damageTarget.tag,
            HitSpeed = evt.Velocity
        };
        
        // 6. 调用 DamageProcessor 应用修改器
        DamageProcessor.Instance.ProcessDamage(ref attackData);
        
        // 7. 发布最终伤害事件
        PublishDamageEvent(attackData, rule, evt);
        
        totalDamageEvents++;
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

