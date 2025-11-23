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
    public Vector2 ContactPoint;        // 碰撞点（2D，向后兼容）
    public Vector2 ContactNormal;       // 碰撞法线
    public float Velocity;              // 碰撞时速度
    public float CollisionTime;         // 碰撞时间戳
    
    // ✅ 3D扩展：真实的3D碰撞点（可选，用于特效定位）
    public Vector3? ContactPoint3D;     // 3D碰撞点（nullable，如果为null则使用ContactPoint）

    /// <summary>
    /// ✅ 3D适配：从 Trigger 碰撞创建碰撞事件
    /// 用于 OnTriggerEnter 的场景（如 AttackRange 3D化后）
    /// </summary>
    public static CollisionEvent CreateFromTrigger(GameObject source, Collider targetCollider)
    {
        Rigidbody rb = source.GetComponent<Rigidbody>();
        Vector3 sourcePos = source.transform.position;
        Vector3 targetPos = targetCollider.transform.position;
        
        // ✅ 计算真实的3D接触点（用于特效定位）
        Vector3 contactPoint3D = targetCollider.ClosestPoint(sourcePos);
        
        // 保留XZ平面投影用于向后兼容（逻辑计算可能仍需要2D）
        Vector2 contactPoint = new Vector2(contactPoint3D.x, contactPoint3D.z);
        
        // 计算法线（XZ 平面，用于逻辑计算）
        Vector3 normal3D = (sourcePos - targetPos);
        normal3D.y = 0f; // 只考虑 XZ 平面（用于逻辑计算）
        normal3D.Normalize();
        Vector2 contactNormal = new Vector2(normal3D.x, normal3D.z);
        
        return new CollisionEvent
        {
            Source = source,
            Target = targetCollider.gameObject,
            ContactPoint = contactPoint,        // 2D投影（向后兼容）
            ContactPoint3D = contactPoint3D,    // ✅ 真实的3D碰撞点（用于特效）
            ContactNormal = contactNormal,
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
    public Vector2 StoppedPosition;     // 停止位置（2D 投影，兼容旧逻辑）
    public float StoppedTime;           // 停止时间戳
    
    // ✅ 3D 扩展：真实世界坐标
    public Vector3? StoppedPosition3D;
    
    // 【三角形攻击】轨迹数据（可选）
    public Vector2? LaunchPosition;     // 发射起点（nullable）
    public Vector2? FirstCollisionPoint; // 第一碰撞点（nullable）
    public bool HasCollision;           // 是否发生碰撞
    
    // ✅ 3D 扩展：真实轨迹
    public Vector3? LaunchPosition3D;
    public Vector3? FirstCollisionPoint3D;
    
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
            HasCollision = false,
            StoppedPosition3D = null,
            LaunchPosition3D = null,
            FirstCollisionPoint3D = null
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
            HasCollision = firstCollisionPos.HasValue,
            StoppedPosition3D = null,
            LaunchPosition3D = null,
            FirstCollisionPoint3D = null
        };
    }
    
    /// <summary>
    /// ✅ 3D 版本：记录真实的 3D 坐标
    /// </summary>
    public static StoppedEvent CreateWithTrajectory3D(
        GameObject source,
        Vector3 stoppedPos3D,
        Vector3? launchPos3D,
        Vector3? firstCollisionPos3D)
    {
        return new StoppedEvent
        {
            Source = source,
            StoppedPosition = new Vector2(stoppedPos3D.x, stoppedPos3D.z),
            StoppedTime = Time.time,
            LaunchPosition = launchPos3D.HasValue ? new Vector2(launchPos3D.Value.x, launchPos3D.Value.z) : (Vector2?)null,
            FirstCollisionPoint = firstCollisionPos3D.HasValue ? new Vector2(firstCollisionPos3D.Value.x, firstCollisionPos3D.Value.z) : (Vector2?)null,
            HasCollision = firstCollisionPos3D.HasValue,
            StoppedPosition3D = stoppedPos3D,
            LaunchPosition3D = launchPos3D,
            FirstCollisionPoint3D = firstCollisionPos3D
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
    public Vector2 HitPosition;         // 击中位置（2D，向后兼容，用于旧逻辑）
    public Vector2 HitDirection;        // 击中方向（2D，向后兼容）
    public float VelocityAtHit;         // 击中时速度
    
    // 3D 扩展：真实的 3D 击中位置（可选，用于特效定位）
    public Vector3? HitPosition3D;      // 如果为 null，则使用 HitPosition 的 XZ 投影
    
    // 附加效果
    public float KnockbackForce;        // 击退力度
    public float StunDuration;          // 眩晕时长
    public bool CanBeBlocked;           // 是否可被格挡
    
    // 调试信息
    public string RuleName;             // 触发的规则名称
    public float EventTime;             // 事件时间戳
}

#endregion

