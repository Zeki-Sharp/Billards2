using UnityEngine;

/// <summary>
/// 游戏事件数据结构定义
/// 纯数据结构，不包含业务逻辑
/// 事件触发请使用 GameEventBus
/// </summary>

/// <summary>
/// 特效事件定义（简化版）
/// 用于非攻击相关的特效，如环境特效、UI特效等
/// 攻击相关特效通过 AttackEffectEvent 处理
/// </summary>
public struct EffectEvent
{
    public string EffectType;        // 特效类型：Launch, HoleEnter, UI等
    public Vector3 Position;         // 特效位置
    public Vector3 Direction;        // 特效方向（可选）
    public float Intensity;          // 特效强度（可选）
    public string TargetTag;         // 目标标签（Player, Enemy等）
    public GameObject TargetObject;  // 目标对象
    
}


/// <summary>
/// 攻击数据 - 用于游戏逻辑层
/// 包含攻击相关的所有信息，但不包含表现相关数据
/// </summary>
public struct AttackData
{
    public string AttackType;        // 攻击类型：Hit, Shoot, Skill, Magic等
    public Vector3 Position;         // 攻击位置
    public Vector3 Direction;        // 攻击方向
    public GameObject Attacker;      // 攻击者
    public GameObject Target;        // 目标对象
    public float Damage;             // 伤害值
    public float AttackTime;         // 攻击时间戳
    public string AttackerTag;       // 攻击者标签
    public string TargetTag;         // 目标标签
    
    // 撞墙相关参数（可选）
    public Vector3 HitNormal;        // 撞击法线
    public float HitSpeed;           // 撞击速度
    public float WallHitRotationAngle;    // 墙面撞击旋转角度
    public Vector3 WallHitPositionOffset; // 墙面撞击位置偏移
}

/// <summary>
/// 死亡数据 - 用于游戏逻辑层
/// 包含死亡相关的所有信息，但不包含表现相关数据
/// </summary>
public struct DeathData
{
    public string DeathType;        // 死亡类型：EnemyDeath, PlayerDeath等
    public Vector3 Position;        // 死亡位置
    public Vector3 Direction;       // 死亡方向（可选）
    public GameObject DeadObject;   // 死亡对象
    public string DeadObjectTag;    // 死亡对象标签
    public float DeathTime;         // 死亡时间戳
}

/// <summary>
/// 攻击特效事件 - 纯表现层
/// 用于播放攻击相关的特效、音效等
/// </summary>
public struct AttackEffectEvent
{
    public string AttackType;        // 攻击类型：Hit, Shoot, Skill, Magic等
    public Vector3 Position;         // 攻击位置
    public Vector3 Direction;        // 攻击方向
    public GameObject Attacker;      // 攻击者
    public GameObject Target;        // 目标对象
    public float Damage;             // 伤害值（用于特效强度）
    public string AttackerTag;       // 攻击者标签
    public string TargetTag;         // 目标标签
    
    // 撞墙相关参数（用于特效）
    public Vector3 HitNormal;        // 撞击法线
    public float HitSpeed;           // 撞击速度
    public float WallHitRotationAngle;    // 墙面撞击旋转角度
    public Vector3 WallHitPositionOffset; // 墙面撞击位置偏移
    
}

/// <summary>
/// 死亡特效事件 - 纯表现层
/// 用于播放死亡相关的特效、音效等
/// </summary>
public struct DeathEffectEvent
{
    public string DeathType;        // 死亡类型：EnemyDeath, PlayerDeath等
    public Vector3 Position;        // 死亡位置
    public Vector3 Direction;       // 死亡方向（可选）
    public GameObject DeadObject;   // 死亡对象
    public string DeadObjectTag;    // 死亡对象标签
    
}

