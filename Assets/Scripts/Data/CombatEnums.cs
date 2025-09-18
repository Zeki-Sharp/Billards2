using UnityEngine;

/// <summary>
/// 攻击方式枚举 - 已废弃，现在统一使用扇形攻击
/// </summary>
[System.Obsolete("AttackType已废弃，现在统一使用扇形攻击系统")]
public enum AttackType
{
    Contact,    // 接触攻击（已废弃）
    Ranged      // 远程攻击（已废弃）
}

/// <summary>
/// 移动方式枚举
/// </summary>
public enum MovementType
{
    FollowPlayer,   // 追随玩家（现有实现）
    Patrol          // 固定路径巡逻（待实现）
}
