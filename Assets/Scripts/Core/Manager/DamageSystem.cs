using UnityEngine;
using System.Collections.Generic;
using System.Linq;

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
    
    // 实体伤害配置注册表（多 Profile 支持）
    private Dictionary<GameObject, List<DamageProfile>> entityMultiProfiles = new Dictionary<GameObject, List<DamageProfile>>();
    
    // 向后兼容：单 Profile 注册表 
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
    /// 注册实体和其伤害配置（单 Profile，向后兼容）
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
    /// 注册实体和其伤害配置（多 Profile 组合）
    /// </summary>
    public void RegisterEntity(GameObject entity, List<DamageProfile> damageProfiles)
    {
        if (entity == null)
        {
            Debug.LogWarning("[DamageSystem] 尝试注册空实体");
            return;
        }
        
        if (damageProfiles == null || damageProfiles.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] {entity.name} 注册时未提供伤害配置（无攻击能力）");
            }
            return;
        }
        
        // 过滤掉空的 Profile
        var validProfiles = damageProfiles.Where(p => p != null).ToList();
        if (validProfiles.Count == 0)
        {
            Debug.LogWarning($"[DamageSystem] {entity.name} 的所有 Profile 都为空");
            return;
        }
        
        entityMultiProfiles[entity] = validProfiles;
        
        if (enableDebugLog)
        {
            int totalRules = validProfiles.Sum(p => p.rules != null ? p.rules.Count : 0);
            string profileNames = string.Join(", ", validProfiles.Select(p => p.profileName));
            Debug.Log($"[DamageSystem] 注册实体: {entity.name}, Profiles: [{profileNames}], 总规则数: {totalRules}");
        }
    }
    
    /// <summary>
    /// 注销实体
    /// </summary>
    public void UnregisterEntity(GameObject entity)
    {
        if (entity == null) return;
        
        bool removed = entityProfiles.Remove(entity) || entityMultiProfiles.Remove(entity);
        
        if (removed && enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 注销实体: {entity.name}");
        }
    }
    
    /// <summary>
    /// 获取实体的所有伤害规则（支持多 Profile 组合和向上查找父级）
    /// </summary>
    private List<DamageRuleConfig> GetAllDamageRules(GameObject entity)
    {
        if (entity == null) return new List<DamageRuleConfig>();
        
        List<DamageRuleConfig> allRules = new List<DamageRuleConfig>();
        
        // 1. 优先尝试从多 Profile 注册表获取
        if (entityMultiProfiles.TryGetValue(entity, out List<DamageProfile> profiles))
        {
            foreach (var profile in profiles)
            {
                if (profile != null && profile.rules != null)
                {
                    allRules.AddRange(profile.rules.Where(r => r != null));
                }
            }
        }
        // 2. 回退到单 Profile 注册表
        else if (entityProfiles.TryGetValue(entity, out DamageProfile singleProfile))
        {
            if (singleProfile != null && singleProfile.rules != null)
            {
                allRules.AddRange(singleProfile.rules.Where(r => r != null));
            }
        }
        // 3. 如果当前对象没有，尝试从父级获取
        else
        {
            Transform current = entity.transform.parent;
            while (current != null && allRules.Count == 0)
            {
                if (entityMultiProfiles.TryGetValue(current.gameObject, out profiles))
                {
                    foreach (var profile in profiles)
                    {
                        if (profile != null && profile.rules != null)
                        {
                            allRules.AddRange(profile.rules.Where(r => r != null));
                        }
                    }
                    break;
                }
                else if (entityProfiles.TryGetValue(current.gameObject, out singleProfile))
                {
                    if (singleProfile != null && singleProfile.rules != null)
                    {
                        allRules.AddRange(singleProfile.rules.Where(r => r != null));
                    }
                    break;
                }
                current = current.parent;
            }
        }
        
        // 按优先级排序
        allRules.Sort((a, b) => a.priority.CompareTo(b.priority));
        
        return allRules;
    }
    
    /// <summary>
    /// 获取实体的伤害配置（单 Profile，向后兼容）
    /// </summary>
    private DamageProfile GetDamageProfile(GameObject entity)
    {
        if (entity == null) return null;
        
        // 先尝试从当前对象获取
        if (entityProfiles.TryGetValue(entity, out DamageProfile profile))
        {
            return profile;
        }
        
        // 如果没有，尝试从父级获取
        Transform current = entity.transform.parent;
        while (current != null)
        {
            if (entityProfiles.TryGetValue(current.gameObject, out profile))
            {
                return profile;
            }
            current = current.parent;
        }
        
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
        
        // ✅ 调试日志：碰撞事件
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 碰撞事件 - Source: {evt.Source?.name} (Tag: {evt.Source?.tag}), Target: {evt.Target?.name} (Tag: {evt.Target?.tag}), 速度: {evt.Velocity:F2}");
        }
        
        // 获取 source 的所有伤害规则（支持多 Profile）
        List<DamageRuleConfig> rules = GetAllDamageRules(evt.Source);
        
        if (rules.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] {evt.Source?.name} 没有配置伤害规则");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] {evt.Source?.name} 有 {rules.Count} 条规则");
        }
        
        // 遍历规则，检查匹配
        int matchedCount = 0;
        foreach (var rule in rules)
        {
            if (rule.triggerType != DamageTriggerType.Collision) continue;
            
            if (showRuleMatching)
            {
                Debug.Log($"[DamageSystem] 检查规则: {rule.ruleName}");
            }
            
            // 检查规则条件
            if (CheckRule(rule, evt.Source, evt.Target, evt.Velocity))
            {
                matchedCount++;
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] ✅ 规则匹配: {rule.ruleName}");
                }
                
                // 计算并发布伤害
                ProcessDamage(rule, evt);
            }
            else if (showRuleMatching)
            {
                Debug.Log($"[DamageSystem] ❌ 规则不匹配: {rule.ruleName}");
            }
        }
        
        if (enableDebugLog && matchedCount == 0)
        {
            Debug.Log($"[DamageSystem] 没有规则匹配此碰撞");
        }
    }
    
    /// <summary>
    /// 处理停止事件（球停止范围攻击）
    /// </summary>
    private void HandleStoppedEvent(StoppedEvent evt)
    {
        if (!systemEnabled) return;
        
        // 获取 source 的所有伤害规则（支持多 Profile）
        List<DamageRuleConfig> rules = GetAllDamageRules(evt.Source);
        
        if (rules.Count == 0) return;
        
        // 遍历规则，检查匹配
        foreach (var rule in rules)
        {
            if (rule.triggerType != DamageTriggerType.Stopped) continue;
            
            // 对于 Stopped 类型，需要范围检测
            ProcessStoppedDamage(rule, evt);
        }
    }
    
    /// <summary>
    /// 处理停止伤害（范围检测）- 根据形状类型选择检测方式
    /// </summary>
    private void ProcessStoppedDamage(DamageRuleConfig rule, StoppedEvent evt)
    {
        // 根据规则的形状类型选择检测方式
        if (rule.rangeShape == RangeShapeType.Triangle)
        {
            ProcessTriangleDamage(rule, evt);
        }
        else // Circle (默认)
        {
            ProcessCircleDamage(rule, evt);
        }
    }
    
    /// <summary>
    /// 处理圆形范围伤害（原有逻辑）
    /// </summary>
    private void ProcessCircleDamage(DamageRuleConfig rule, StoppedEvent evt)
    {
        // 确定攻击范围
        float range = GetStoppedAttackRange(rule, evt.Source);
        if (range <= 0f)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[DamageSystem] 规则 '{rule.ruleName}' 计算到的范围 <= 0，跳过范围伤害");
            }
            return;
        }

        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 使用攻击范围: {range}");
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

    /// <summary>
    /// 获取停止事件的攻击范围，优先使用实时属性
    /// </summary>
    private float GetStoppedAttackRange(DamageRuleConfig rule, GameObject source)
    {
        // 1. 规则自带数值优先
        if (rule.attackRange > 0f)
        {
            return rule.attackRange;
        }

        // 2. 从 PlayerStats（实时属性）读取
        var playerStats = source.GetComponent<PlayerStats>();
        if (playerStats == null && source.transform.parent != null)
        {
            playerStats = source.transform.parent.GetComponent<PlayerStats>();
        }
        if (playerStats == null && source.TryGetComponent(out PlayerBehavior behaviorFromSource) && behaviorFromSource != null)
        {
            playerStats = behaviorFromSource.GetComponent<PlayerStats>();
        }

        if (playerStats != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] 通过 PlayerStats 获取 AreaRadius: {playerStats.FinalAreaRadius} 来自 {playerStats.name}");
            }
            return playerStats.FinalAreaRadius;
        }

        // 3. 回退到 PlayerData 的基础值
        var playerBehavior = source.GetComponent<PlayerBehavior>();
        if (playerBehavior == null && source.transform.parent != null)
        {
            playerBehavior = source.transform.parent.GetComponent<PlayerBehavior>();
        }

        if (playerBehavior?.PlayerData != null)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] 回退使用 PlayerData.AreaRadius: {playerBehavior.PlayerData.areaRadius}");
            }
            return playerBehavior.PlayerData.areaRadius;
        }

        Debug.LogWarning($"[DamageSystem] Stopped 规则 '{rule.ruleName}' 未配置范围，且无法从 PlayerStats 或 PlayerData 读取");
        return 0f;
    }
    
    /// <summary>
    /// 处理三角形范围伤害（新功能）
    /// 使用几何算法检测碰撞体是否与三角形区域相交
    /// </summary>
    private void ProcessTriangleDamage(DamageRuleConfig rule, StoppedEvent evt)
    {
        // 检查是否有轨迹数据
        if (!evt.HasCollision || !evt.LaunchPosition.HasValue || !evt.FirstCollisionPoint.HasValue)
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] 三角形攻击 '{rule.ruleName}' 取消：无碰撞记录");
            }
            return;
        }
        
        // 获取三角形三个顶点
        Vector2 p1 = evt.LaunchPosition.Value;      // 起点
        Vector2 p2 = evt.FirstCollisionPoint.Value; // 第一碰撞点
        Vector2 p3 = evt.StoppedPosition;           // 终点
        
        // 验证三角形有效性
        if (!IsValidTriangle(p1, p2, p3, out float area))
        {
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] 三角形攻击 '{rule.ruleName}' 取消：三角形无效（面积: {area:F3}）");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 三角形攻击 '{rule.ruleName}' 触发：面积 {area:F2}，顶点: [{p1}, {p2}, {p3}]");
        }
        
        // 计算三角形的包围盒（用于粗筛选）
        float minX = Mathf.Min(p1.x, p2.x, p3.x);
        float maxX = Mathf.Max(p1.x, p2.x, p3.x);
        float minY = Mathf.Min(p1.y, p2.y, p3.y);
        float maxY = Mathf.Max(p1.y, p2.y, p3.y);
        Vector2 boxCenter = new Vector2((minX + maxX) / 2, (minY + maxY) / 2);
        Vector2 boxSize = new Vector2(maxX - minX, maxY - minY);
        
        // 粗筛选：使用包围盒检测所有可能的目标
        Collider2D[] colliders = Physics2D.OverlapBoxAll(boxCenter, boxSize, 0f);
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 包围盒内检测到 {colliders.Length} 个碰撞体");
        }
        
        int hitCount = 0;
        foreach (var collider in colliders)
        {
            GameObject target = collider.gameObject;
            
            // 检查目标标签
            if (!string.IsNullOrEmpty(rule.targetTag))
            {
                if (!target.CompareTag(rule.targetTag))
                {
                    if (showRuleMatching)
                    {
                        Debug.Log($"[DamageSystem] 跳过 {target.name}：标签不匹配（需要 {rule.targetTag}）");
                    }
                    continue;
                }
            }
            
            // 检查来源标签
            if (!string.IsNullOrEmpty(rule.sourceTag))
            {
                if (!evt.Source.CompareTag(rule.sourceTag)) continue;
            }
            
            // 精确检测：判断碰撞体是否与三角形区域相交
            if (!IsColliderIntersectTriangle(collider, p1, p2, p3))
            {
                if (showRuleMatching)
                {
                    Debug.Log($"[DamageSystem] {target.name} 不在三角形内");
                }
                continue;
            }
            
            hitCount++;
            
            Vector2 targetPos = target.transform.position;
            
            if (enableDebugLog)
            {
                Debug.Log($"[DamageSystem] ✅ {target.name} 在三角形内，准备造成伤害");
            }
            
            // 创建模拟的碰撞事件用于伤害计算
            CollisionEvent collisionEvt = new CollisionEvent
            {
                Source = evt.Source,
                Target = target,
                ContactPoint = targetPos,
                ContactNormal = (targetPos - evt.StoppedPosition).normalized,
                Velocity = 0f,
                CollisionTime = evt.StoppedTime
            };
            
            ProcessDamage(rule, collisionEvt);
        }
        
        if (enableDebugLog)
        {
            Debug.Log($"[DamageSystem] 三角形攻击命中 {hitCount} 个目标");
        }
    }
    
    #endregion
    
    #region 几何工具方法
    
    /// <summary>
    /// 验证三角形有效性（检查三点是否共线或距离过近）
    /// </summary>
    /// <param name="p1">顶点1</param>
    /// <param name="p2">顶点2</param>
    /// <param name="p3">顶点3</param>
    /// <param name="area">输出：三角形面积</param>
    /// <returns>是否为有效三角形</returns>
    private bool IsValidTriangle(Vector2 p1, Vector2 p2, Vector2 p3, out float area)
    {
        // 使用向量叉积计算三角形面积
        // Area = 0.5 * |AB × AC|
        Vector2 AB = p2 - p1;
        Vector2 AC = p3 - p1;
        
        // 2D向量叉积的z分量
        float crossProduct = AB.x * AC.y - AB.y * AC.x;
        area = Mathf.Abs(crossProduct) * 0.5f;
        
        // 面积阈值：小于0.1的三角形视为无效（三点接近共线）
        const float MIN_AREA = 0.1f;
        return area >= MIN_AREA;
    }
    
    /// <summary>
    /// 判断碰撞体是否与三角形区域相交
    /// </summary>
    /// <param name="collider">目标碰撞体</param>
    /// <param name="p1">三角形顶点1</param>
    /// <param name="p2">三角形顶点2</param>
    /// <param name="p3">三角形顶点3</param>
    /// <returns>是否相交</returns>
    private bool IsColliderIntersectTriangle(Collider2D collider, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        // 策略：检查碰撞体的多个采样点是否有任何一个在三角形内
        // 这样可以处理各种形状的碰撞体（圆形、方形、多边形等）
        
        Vector2 center = collider.bounds.center;
        float radius = Mathf.Max(collider.bounds.extents.x, collider.bounds.extents.y);
        
        // 1. 检查碰撞体中心点
        if (IsPointInTriangle(center, p1, p2, p3))
        {
            return true;
        }
        
        // 2. 检查碰撞体边界的8个采样点（上下左右 + 四个角）
        Vector2[] samplePoints = new Vector2[]
        {
            center + new Vector2(radius, 0),        // 右
            center + new Vector2(-radius, 0),       // 左
            center + new Vector2(0, radius),        // 上
            center + new Vector2(0, -radius),       // 下
            center + new Vector2(radius, radius),   // 右上
            center + new Vector2(-radius, radius),  // 左上
            center + new Vector2(radius, -radius),  // 右下
            center + new Vector2(-radius, -radius)  // 左下
        };
        
        foreach (var point in samplePoints)
        {
            if (IsPointInTriangle(point, p1, p2, p3))
            {
                return true;
            }
        }
        
        // 3. 检查三角形的三个顶点是否在碰撞体内（反向检测）
        if (collider.OverlapPoint(p1) || collider.OverlapPoint(p2) || collider.OverlapPoint(p3))
        {
            return true;
        }
        
        // 如果所有检测都不通过，认为不相交
        return false;
    }
    
    /// <summary>
    /// 判断点是否在三角形内（重心坐标法）
    /// </summary>
    private bool IsPointInTriangle(Vector2 point, Vector2 p1, Vector2 p2, Vector2 p3)
    {
        Vector2 v0 = p3 - p1;
        Vector2 v1 = p2 - p1;
        Vector2 v2 = point - p1;
        
        float dot00 = Vector2.Dot(v0, v0);
        float dot01 = Vector2.Dot(v0, v1);
        float dot02 = Vector2.Dot(v0, v2);
        float dot11 = Vector2.Dot(v1, v1);
        float dot12 = Vector2.Dot(v1, v2);
        
        float invDenom = 1f / (dot00 * dot11 - dot01 * dot01);
        float u = (dot11 * dot02 - dot01 * dot12) * invDenom;
        float v = (dot00 * dot12 - dot01 * dot02) * invDenom;
        
        return (u >= 0) && (v >= 0) && (u + v <= 1);
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
        
        // 检查回合要求
        if (!IsTurnRequirementSatisfied(rule.turnRequirement))
        {
            if (showRuleMatching)
            {
                Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：当前回合不满足 {rule.turnRequirement}");
            }
            return false;
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
        
        // 检查攻击者"不应处于"的状态要求（例如：排除主动攻击，实现反弹伤害）
        if (!string.IsNullOrEmpty(rule.requireSourceNotState))
        {
            Blackboard blackboard = GetBlackboard(source);
            
            // 如果来源没有 Blackboard，认为没有该状态，规则通过
            if (blackboard != null)
            {
                // 尝试获取状态值，如果状态存在且为 true，则规则不匹配
                if (blackboard.TryGet<bool>(rule.requireSourceNotState, out bool stateValue) && stateValue)
                {
                    if (showRuleMatching)
                    {
                        Debug.Log($"[DamageSystem] 规则 '{rule.ruleName}' 不匹配：来源处于 '{rule.requireSourceNotState}' 状态");
                    }
                    return false;
                }
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
    
    /// <summary>
    /// 检查当前回合是否满足规则要求
    /// </summary>
    private bool IsTurnRequirementSatisfied(DamageTurnRequirement requirement)
    {
        if (requirement == DamageTurnRequirement.Any)
        {
            return true;
        }
        
        var flowController = GameFlowController.Instance;
        if (flowController == null)
        {
            // 若没有流程控制器，默认视为不满足以避免错误结算
            if (showRuleMatching)
            {
                Debug.LogWarning("[DamageSystem] 无法获取 GameFlowController，回合要求判定失败");
            }
            return false;
        }
        
        switch (requirement)
        {
            case DamageTurnRequirement.PlayerTurn:
                return flowController.IsPlayerPhase;
            case DamageTurnRequirement.EnemyTurn:
                return flowController.IsEnemyPhase;
            default:
                return true;
        }
    }
    
    #endregion
    
    #region 伤害计算
    
    /// <summary>
    /// 处理伤害（规则匹配后）
    /// </summary>
    private void ProcessDamage(DamageRuleConfig rule, CollisionEvent evt)
    {
        // 1. 计算基础伤害（支持从 PlayerData.attackPower 读取）
        float baseValue = rule.GetBaseDamage(evt.Source);
        float baseDamage = baseValue * rule.damageMultiplier;
        
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
        // ✅ 优先使用3D碰撞点（用于特效定位），如果没有则使用2D投影
        Vector3 attackPosition = evt.ContactPoint3D.HasValue 
            ? evt.ContactPoint3D.Value 
            : new Vector3(evt.ContactPoint.x, 0f, evt.ContactPoint.y); // 2D投影转换为3D（Y=0）
        
        AttackData attackData = new AttackData
        {
            Attacker = attacker,  // ✅ 修复：使用正确的攻击者
            Target = damageTarget,
            Damage = baseDamage,
            AttackType = rule.triggerType.ToString(),
            Position = attackPosition,  // ✅ 使用真实的3D碰撞点
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
            HitPosition = evt.ContactPoint,              // 2D 投影（兼容旧逻辑）
            HitPosition3D = evt.ContactPoint3D,         // 3D 真实接触点（用于特效）
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

