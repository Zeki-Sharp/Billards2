using UnityEngine;

/// <summary>
/// 行为工厂
/// 负责根据配置创建对应的行为实例
/// </summary>
public static class BehaviorFactory
{
    /// <summary>
    /// 根据MovementType创建对应的移动行为
    /// </summary>
    /// <param name="movementType">移动类型</param>
    /// <returns>移动行为实例</returns>
    public static IMovementBehavior CreateMovementBehavior(MovementType movementType)
    {
        switch (movementType)
        {
            case MovementType.FollowPlayer:
                return new FollowPlayerBehavior();
                
            case MovementType.Flee:
                return new FleeBehavior();
                
            case MovementType.IntervalMovement:
                // 检查是否使用 V2 配置（留空则使用旧系统）
                // 注意：此处无法访问 levelConfig，由调用方决定使用哪个版本
                // 默认返回旧系统，新系统需要手动指定
                return new IntervalMovementBehavior();
                
            // ===== 原子行为 =====
            case MovementType.MoveTowards:
                return new MoveTowardsBehavior();
                
            case MovementType.MoveAway:
                return new MoveAwayBehavior();
                
            case MovementType.Idle:
                return new IdleBehavior();
                
            default:
                Debug.LogError($"BehaviorFactory: 未知的移动类型: {movementType}，使用默认的跟随玩家行为");
                return new FollowPlayerBehavior();
        }
    }
    
    /// <summary>
    /// 验证移动类型是否有效
    /// </summary>
    /// <param name="movementType">移动类型</param>
    /// <returns>是否有效</returns>
    public static bool IsValidMovementType(MovementType movementType)
    {
        return movementType == MovementType.FollowPlayer || 
               movementType == MovementType.Flee || 
               movementType == MovementType.IntervalMovement ||
               movementType == MovementType.MoveTowards ||
               movementType == MovementType.MoveAway ||
               movementType == MovementType.Idle;
    }
    
    /// <summary>
    /// 获取所有支持的移动类型
    /// </summary>
    /// <returns>支持的移动类型数组</returns>
    public static MovementType[] GetSupportedMovementTypes()
    {
        return new MovementType[] { 
            MovementType.FollowPlayer, 
            MovementType.Flee, 
            MovementType.IntervalMovement,
            MovementType.MoveTowards,
            MovementType.MoveAway,
            MovementType.Idle
        };
    }
    
    /// <summary>
    /// 根据AttackType创建对应的攻击行为
    /// </summary>
    /// <param name="attackType">攻击类型</param>
    /// <returns>攻击行为实例</returns>
    public static IAttackBehavior CreateAttackBehavior(AttackType attackType)
    {
        switch (attackType)
        {
            case AttackType.Melee:
                return new MeleeAttackBehavior();
                
            case AttackType.Ranged:
                return new RangedAttackBehavior();
                
            case AttackType.Thorn:
                return new ThornAttackBehavior();
                
            default:
                Debug.LogError($"BehaviorFactory: 未知的攻击类型: {attackType}，使用默认的近战攻击行为");
                return new MeleeAttackBehavior();
        }
    }
    
    /// <summary>
    /// 验证攻击类型是否有效
    /// </summary>
    /// <param name="attackType">攻击类型</param>
    /// <returns>是否有效</returns>
    public static bool IsValidAttackType(AttackType attackType)
    {
        return attackType == AttackType.Melee || attackType == AttackType.Ranged || attackType == AttackType.Thorn;
    }
    
    /// <summary>
    /// 获取所有支持的攻击类型
    /// </summary>
    /// <returns>支持的攻击类型数组</returns>
    public static AttackType[] GetSupportedAttackTypes()
    {
        return new AttackType[] { AttackType.Melee, AttackType.Ranged, AttackType.Thorn };
    }
}
