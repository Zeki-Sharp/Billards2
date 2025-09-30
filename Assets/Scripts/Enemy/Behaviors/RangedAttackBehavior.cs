using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 远程攻击行为
/// 攻击范围投射到玩家附近位置，位置独立于敌人，即使敌人被撞击也不会改变攻击位置
/// </summary>
public class RangedAttackBehavior : BaseAttackBehavior
{
    private Vector2 projectedPosition; // 保存投射位置
    private Vector3 originalLocalPosition; // 保存原始本地位置
    private Transform originalParent; // 保存原始父物体
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public override void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange)
    {
        if (!ValidateAttackParams(enemyTransform, playerTransform, enemyData, attackRange))
        {
            return;
        }
        
        // 检测玩家是否在检测范围内
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        if (distanceToPlayer > enemyData.rangedConfig.detectionRange)
        {
            Debug.Log($"RangedAttackBehavior: 玩家不在检测范围内 ({distanceToPlayer} > {enemyData.rangedConfig.detectionRange})");
            return;
        }
        
        // 保存原始父子关系和本地位置
        originalParent = attackRange.transform.parent;
        originalLocalPosition = attackRange.transform.localPosition;
        
        Debug.Log($"RangedAttackBehavior: 保存原始状态 - 父物体: {(originalParent != null ? originalParent.name : "null")}，localPosition: {originalLocalPosition}");
        
        // 计算投射位置
        projectedPosition = CalculateProjectionPosition(enemyTransform.position, playerTransform.position, enemyData.rangedConfig);
        
        // 解除父子关系，使用世界坐标
        attackRange.transform.SetParent(null);
        
        // 设置攻击范围到投射位置
        attackRange.transform.position = projectedPosition;
        
        // 显示攻击预告（这会激活 GameObject 并设置方向）
        attackRange.ShowTelegraph();
        
        Debug.Log($"RangedAttackBehavior: 显示远程攻击预告，投射位置: {projectedPosition}，AttackRange实际位置: {attackRange.transform.position}");
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
        
        Debug.Log($"RangedAttackBehavior: 攻击阶段开始 - AttackRange位置: {attackRange.transform.position}，父物体: {(attackRange.transform.parent != null ? attackRange.transform.parent.name : "null")}");
        
        // 使用预告阶段保存的朝向
        attackRange.ApplyTelegraphedDirection();
        
        Debug.Log($"RangedAttackBehavior: ApplyTelegraphedDirection后 - AttackRange位置: {attackRange.transform.position}");
        
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
        if (attackRange == null || originalParent == null)
        {
            return;
        }
        
        Debug.Log($"RangedAttackBehavior: 开始清理 - AttackRange位置: {attackRange.transform.position}");
        
        // 恢复父子关系
        attackRange.transform.SetParent(originalParent);
        
        // 恢复原始本地位置
        attackRange.transform.localPosition = originalLocalPosition;
        
        Debug.Log($"RangedAttackBehavior: 清理完成 - 恢复到原始父物体: {originalParent.name}，localPosition: {originalLocalPosition}");
    }
    
    /// <summary>
    /// 计算投射位置
    /// </summary>
    private Vector2 CalculateProjectionPosition(Vector2 enemyPosition, Vector2 playerPosition, RangedAttackConfig config)
    {
        // 基础位置：玩家位置向敌人方向偏移指定距离
        Vector2 directionToPlayer = (playerPosition - enemyPosition).normalized;
        Vector2 basePosition = playerPosition - directionToPlayer * config.projectionDistance;
        
        // 添加随机偏移
        if (config.useRandomOffset)
        {
            Vector2 randomOffset = Random.insideUnitCircle * config.randomOffsetRange;
            basePosition += randomOffset;
        }
        
        Debug.Log($"RangedAttackBehavior: 计算投射位置 - 玩家: {playerPosition}, 投射: {basePosition}");
        
        return basePosition;
    }
}
