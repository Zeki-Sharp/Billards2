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
    public override void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, attackRange))
        {
            return;
        }
        
        // 近战攻击：AttackRange 在预制体中已经是敌人子物体，保持预制体中的父子关系和位置
        // 只需要显示攻击预告，不改变任何位置
        attackRange.ShowTelegraph();
        
        Debug.Log($"MeleeAttackBehavior: 显示近战攻击预告，AttackRange作为子物体跟随敌人");
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public override void ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange, MMFeedbacks attackEffect)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, attackRange))
        {
            return;
        }
        
        // 使用预告阶段保存的朝向
        attackRange.ApplyTelegraphedDirection();
        
        Debug.Log($"MeleeAttackBehavior: 执行近战攻击");
        
        // 播放攻击特效
        PlayAttackEffect(attackEffect, enemyTransform.name);
        
        // 获取攻击范围内的目标
        var targets = attackRange.GetTargetsInRange();
        
        foreach (var target in targets)
        {
            if (target.CompareTag("Player"))
            {
                DealDamageToPlayer(target, enemyData, enemyTransform);
            }
        }
    }
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    public override void CleanupAttack(Transform enemyTransform, AttackRange attackRange)
    {
        // 近战攻击不需要清理，AttackRange 始终作为敌人子物体，保持预制体中的原始配置
        // 不做任何操作
        Debug.Log($"MeleeAttackBehavior: 近战攻击无需清理");
    }
}
