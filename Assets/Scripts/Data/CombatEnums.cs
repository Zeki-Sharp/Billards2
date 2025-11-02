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
/// 移动类型枚举（原子行为）
/// 复杂行为序列请使用 PhaseSequenceConfig
/// </summary>
public enum MovementType
{
    MoveTowards,    // 向目标靠近（原子行为）
    MoveAway,       // 远离目标（原子行为）
    Idle            // 保持静止（原子行为）
}
