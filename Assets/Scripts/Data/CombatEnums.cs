using UnityEngine;

/// <summary>
/// 攻击方式枚举
/// </summary>
public enum AttackType
{
    Melee,    // 近战攻击
    Ranged,   // 远程攻击
    Thorn     // 棘刺攻击（持续性陷阱）
}

/// <summary>
/// 移动方式枚举
/// </summary>
public enum MovementType
{
    FollowPlayer,   // 追随玩家（现有实现）
    Flee,          // 逃跑（远离玩家）
    IntervalMovement, // 间歇移动（交替静止和移动）
    
    // ===== 原子行为 =====
    MoveTowards,    // 向目标靠近（原子行为）
    MoveAway,       // 远离目标（原子行为）
    Idle            // 保持静止（原子行为）
}
