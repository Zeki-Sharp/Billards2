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
    /// 创建碰撞事件
    /// </summary>
    public static CollisionEvent Create(GameObject source, Collision2D collision)
    {
        Rigidbody2D rb = source.GetComponent<Rigidbody2D>();
        
        return new CollisionEvent
        {
            Source = source,
            Target = collision.gameObject,
            ContactPoint = collision.contacts.Length > 0 ? collision.contacts[0].point : Vector2.zero,
            ContactNormal = collision.contacts.Length > 0 ? collision.contacts[0].normal : Vector2.zero,
            Velocity = rb != null ? rb.linearVelocity.magnitude : 0f,
            CollisionTime = Time.time
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

