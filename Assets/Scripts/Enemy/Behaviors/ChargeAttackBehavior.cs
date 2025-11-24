using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 冲刺攻击行为
/// 攻击阶段：空操作（不造成伤害）
/// 预告阶段：显示冲刺方向指示
/// 移动阶段：朝玩家冲刺，碰撞时通过 DamageSystem 自动处理伤害
/// </summary>
public class ChargeAttackBehavior : BaseAttackBehavior
{
    /// <summary>
    /// 执行预告阶段：显示冲刺方向指示
    /// </summary>
    public override BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, 
                                         EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return BehaviorStatus.Failure;
        }
        
        // 显示冲刺方向指示（AttackRange 会自动朝向玩家）
        attackRange.ShowTelegraph(playerTransform.position);
        
        runtimeState.currentAttackState = "Telegraph";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 执行攻击阶段：空操作
    /// 冲刺攻击的伤害在移动阶段通过碰撞自动触发（DamageSystem 处理）
    /// </summary>
    public override BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, 
                                      EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect, EnemyRuntimeState runtimeState)
    {
        // 攻击阶段不做任何事，直接返回成功
        // 伤害会在移动阶段的碰撞中通过 DamageSystem 自动处理
        runtimeState.currentAttackState = "Charging";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    public override BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        // 隐藏攻击范围预告
        if (attackRange != null)
        {
            attackRange.HideTelegraph();
        }
        
        runtimeState.currentAttackState = "";
        return BehaviorStatus.Success;
    }
}

