using UnityEngine;

/// <summary>
/// 新伤害系统事件数据结构
/// 与现有 AttackData 并行，逐步迁移
/// </summary>

#region 枚举定义

/// <summary>
/// 伤害触发类型
/// </summary>
public enum DamageTriggerType
{
    Collision,  // 碰撞触发
    Stopped,    // 停止触发
    Interval,   // 间隔触发
    Skill       // 技能触发
}

/// <summary>
/// 伤害类型
/// </summary>
public enum DamageType
{
    Physical,   // 物理伤害
    Magical,    // 魔法伤害
    True        // 真实伤害（无视护甲）
}

#endregion

#region 碰撞事件

/// <summary>
/// 碰撞事件 - 统一的物理碰撞事件
/// 替代分散的碰撞检测逻辑
/// </summary>
public struct CollisionEvent
{
    public GameObject Source;           // 碰撞发起方
    public GameObject Target;           // 碰撞目标
    public Vector2 ContactPoint;        // 碰撞点
    public Vector2 ContactNormal;       // 碰撞法线
    public float Velocity;              // 碰撞时速度
    public float CollisionTime;         // 碰撞时间戳
    
    /// <summary>
    /// 创建碰撞事件（物理碰撞）
    /// </summary>
    public static CollisionEvent Create(GameObject source, Collision2D collision)
    {
        Rigidbody2D rb = source.GetComponent<Rigidbody2D>();
        
        // ✅ 修复：使用实际碰撞的 Collider 的 GameObject
        // collision.gameObject 返回有 Rigidbody2D 的父级（如 EnemyItem）
        // collision.collider.gameObject 返回实际碰撞的 Collider 所属的 GameObject（如 AttackRange/Image）
        GameObject targetObject = collision.collider != null ? collision.collider.gameObject : collision.gameObject;
        
        return new CollisionEvent
        {
            Source = source,
            Target = targetObject,  // ✅ 使用实际碰撞的 Collider 的 GameObject
            ContactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : Vector2.zero,
            ContactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.zero,
            Velocity = rb != null ? rb.linearVelocity.magnitude : 0f,
            CollisionTime = Time.time
        };
    }
    
    /// <summary>
    /// 从 Trigger 碰撞创建碰撞事件
    /// 用于 OnTriggerEnter2D 的场景（如 AttackRange）
    /// </summary>
    public static CollisionEvent CreateFromTrigger(GameObject source, Collider2D targetCollider)
    {
        Rigidbody2D rb = source.GetComponent<Rigidbody2D>();
        Vector2 sourcePos = source.transform.position;
        Vector2 targetPos = targetCollider.transform.position;
        
        return new CollisionEvent
        {
            Source = source,
            Target = targetCollider.gameObject,
            ContactPoint = targetCollider.ClosestPoint(sourcePos),
            ContactNormal = (sourcePos - targetPos).normalized,
            Velocity = rb != null ? rb.linearVelocity.magnitude : 0f,
            CollisionTime = Time.time
        };
    }
}

#endregion

#region 停止事件

/// <summary>
/// 停止事件 - 用于球停止后的范围攻击
/// 【三角形攻击扩展】：添加了轨迹数据，支持动态形状范围攻击
/// </summary>
public struct StoppedEvent
{
    public GameObject Source;           // 停止的对象（玩家）
    public Vector2 StoppedPosition;     // 停止位置
    public float StoppedTime;           // 停止时间戳
    
    // 【三角形攻击】轨迹数据（可选）
    public Vector2? LaunchPosition;     // 发射起点（nullable）
    public Vector2? FirstCollisionPoint; // 第一碰撞点（nullable）
    public bool HasCollision;           // 是否发生碰撞
    
    /// <summary>
    /// 创建停止事件（简单版本，向后兼容）
    /// </summary>
    public static StoppedEvent Create(GameObject source, Vector2 position)
    {
        return new StoppedEvent
        {
            Source = source,
            StoppedPosition = position,
            StoppedTime = Time.time,
            LaunchPosition = null,
            FirstCollisionPoint = null,
            HasCollision = false
        };
    }
    
    /// <summary>
    /// 创建停止事件（带轨迹数据，用于三角形攻击）
    /// </summary>
    public static StoppedEvent CreateWithTrajectory(
        GameObject source, 
        Vector2 stoppedPos, 
        Vector2? launchPos, 
        Vector2? firstCollisionPos)
    {
        return new StoppedEvent
        {
            Source = source,
            StoppedPosition = stoppedPos,
            StoppedTime = Time.time,
            LaunchPosition = launchPos,
            FirstCollisionPoint = firstCollisionPos,
            HasCollision = firstCollisionPos.HasValue
        };
    }
}

#endregion

#region 伤害事件

/// <summary>
/// 伤害事件 - 最终伤害传递
/// 从 DamageSystem → IDamageable
/// </summary>
public struct DamageEvent
{
    // 基础信息
    public GameObject Source;           // 伤害来源（攻击者）
    public GameObject Target;           // 伤害目标（受击者）
    public float FinalDamage;           // 最终伤害值
    
    // 伤害类型
    public DamageType Type;             // 伤害类型
    public DamageTriggerType TriggerType; // 触发类型
    
    // 上下文信息
    public Vector2 HitPosition;         // 击中位置
    public Vector2 HitDirection;        // 击中方向
    public float VelocityAtHit;         // 击中时速度
    
    // 附加效果
    public float KnockbackForce;        // 击退力度
    public float StunDuration;          // 眩晕时长
    public bool CanBeBlocked;           // 是否可被格挡
    
    // 调试信息
    public string RuleName;             // 触发的规则名称
    public float EventTime;             // 事件时间戳
}

#endregion

