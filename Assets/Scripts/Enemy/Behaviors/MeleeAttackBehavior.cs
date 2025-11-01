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
    public override void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return;
        }
        
        // ✅ 新伤害系统：不在 Telegraph 阶段设置 CanAttack
        // 因为 Telegraph 是最后一个阶段，应该在 Attack 阶段设置
        
        // 近战攻击：AttackRange 在预制体中已经是敌人子物体，保持预制体中的父子关系和位置
        // 只需要显示攻击预告，不改变任何位置
        attackRange.ShowTelegraph();
        
        Debug.Log($"MeleeAttackBehavior: 显示近战攻击预告，AttackRange作为子物体跟随敌人");
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public override void ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, levelConfig, attackRange))
        {
            return;
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
    }
    
    /// <summary>
    /// 清理攻击状态（Move 阶段开始时调用）
    /// </summary>
    public override void CleanupAttack(Transform enemyTransform, AttackRange attackRange)
    {
        // ✅ 新伤害系统：在 Move 阶段清理 CanAttack 状态
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        blackboard.Set("CanAttack", false);
    }
}
