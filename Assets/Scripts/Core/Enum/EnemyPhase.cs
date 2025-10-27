using UnityEngine;

/// <summary>
/// 敌人阶段枚举
/// </summary>
public enum EnemyPhase
{
    None,       // 无阶段
    Attack,     // 攻击阶段
    Move,       // 移动阶段
    Spawn,      // 生成阶段
    Telegraph   // 预告阶段
}
