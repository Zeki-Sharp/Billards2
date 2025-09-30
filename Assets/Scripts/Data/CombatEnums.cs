using UnityEngine;

/// <summary>
/// 攻击方式枚举
/// </summary>
public enum AttackType
{
    Melee,    // 近战攻击
    Ranged    // 远程攻击
}

/// <summary>
/// 移动方式枚举
/// </summary>
public enum MovementType
{
    FollowPlayer,   // 追随玩家（现有实现）
    Flee           // 逃跑（远离玩家）
}
