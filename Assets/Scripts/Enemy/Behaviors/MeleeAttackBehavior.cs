using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 近战攻击行为
/// 攻击范围跟随敌人位置，敌人被撞击时攻击位置也会改变
/// </summary>
public class MeleeAttackBehavior : BaseAttackBehavior
{
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return BehaviorStatus.Failure;
        }
        
        // 近战攻击：攻击范围与敌人保持父子关系，只需面向目标玩家
        attackRange.ShowTelegraph(playerTransform.position);
        
        runtimeState.currentAttackState = "Telegraph";
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public override BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect, EnemyRuntimeState runtimeState)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return BehaviorStatus.Failure;
        }
        
        // 使用预告阶段保存的朝向
        attackRange.ApplyTelegraphedDirection();
        
        // ✅ 新伤害系统：在 Attack 阶段设置 CanAttack 状态
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        blackboard.Set("CanAttack", true);
        
        // 播放攻击特效
        PlayAttackEffect(attackEffect, enemyTransform.name);
        
        // ✅ 新伤害系统：主动检测范围内的玩家并发布碰撞事件
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                // 使用 AttackRange 作为 source（Tag = EnemyAttackRange）
                CollisionEvent evt = CollisionEvent.CreateFromTrigger(attackRange.gameObject, target.GetComponent<Collider2D>());
                GameEventBus.PublishCollision(evt);
            }
        }
        
        runtimeState.currentAttackState = "Attacking";
        runtimeState.lastAttackTime = Time.time;
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 清理攻击状态（Move 阶段开始时调用）
    /// </summary>
    public override BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState)
    {
        // ✅ 新伤害系统：在 Move 阶段清理 CanAttack 状态
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        blackboard.Set("CanAttack", false);
        
        // ✅ 统一显隐责任：在清理时隐藏攻击范围
        if (attackRange != null)
        {
            attackRange.HideTelegraph();
        }
        
        runtimeState.currentAttackState = "";
        return BehaviorStatus.Success;
    }
}
