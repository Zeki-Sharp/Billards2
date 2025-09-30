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
        return movementType == MovementType.FollowPlayer || movementType == MovementType.Flee;
    }
    
    /// <summary>
    /// 获取所有支持的移动类型
    /// </summary>
    /// <returns>支持的移动类型数组</returns>
    public static MovementType[] GetSupportedMovementTypes()
    {
        return new MovementType[] { MovementType.FollowPlayer, MovementType.Flee };
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
        return attackType == AttackType.Melee || attackType == AttackType.Ranged;
    }
    
    /// <summary>
    /// 获取所有支持的攻击类型
    /// </summary>
    /// <returns>支持的攻击类型数组</returns>
    public static AttackType[] GetSupportedAttackTypes()
    {
        return new AttackType[] { AttackType.Melee, AttackType.Ranged };
    }
}
