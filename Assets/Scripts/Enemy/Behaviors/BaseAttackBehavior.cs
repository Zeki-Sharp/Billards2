using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 攻击行为抽象基类
/// 提供通用的攻击行为实现
/// </summary>
public abstract class BaseAttackBehavior : IAttackBehavior
{
    /// <summary>
    /// 执行预告阶段 - 抽象方法，由子类实现
    /// </summary>
    public abstract void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange);
    
    /// <summary>
    /// 执行攻击阶段 - 抽象方法，由子类实现
    /// </summary>
    public abstract void ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange, MMFeedbacks attackEffect);
    
    /// <summary>
    /// 清理攻击状态 - 抽象方法，由子类实现
    /// </summary>
    public abstract void CleanupAttack(Transform enemyTransform, AttackRange attackRange);
    
    /// <summary>
    /// 验证攻击参数
    /// </summary>
    protected bool ValidateAttackParams(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange)
    {
        if (enemyTransform == null)
        {
            Debug.LogError("BaseAttackBehavior: 敌人Transform为空");
            return false;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("BaseAttackBehavior: 玩家Transform为空");
            return false;
        }
        
        if (enemyData == null)
        {
            Debug.LogError("BaseAttackBehavior: EnemyData为空");
            return false;
        }
        
        if (attackRange == null)
        {
            Debug.LogError("BaseAttackBehavior: AttackRange为空");
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    protected void DealDamageToPlayer(GameObject playerObject, EnemyData enemyData, Transform enemyTransform)
    {
        if (playerObject == null || enemyData == null)
        {
            return;
        }
        
        // 从 EnemyData 读取伤害值
        float damage = enemyData.damage;
        
        // 只发布攻击事件，让 DamageProcessor 统一处理伤害应用
        if (enemyTransform != null)
        {
            enemyTransform.gameObject.PublishAttack("EnemyAttack", enemyTransform.position, playerObject, damage);
        }
        
        Debug.Log($"BaseAttackBehavior: 发布攻击事件（类型：EnemyAttack），伤害: {damage}");
    }
    
    /// <summary>
    /// 播放攻击特效
    /// </summary>
    protected void PlayAttackEffect(MMFeedbacks attackEffect, string attackerName)
    {
        if (attackEffect != null)
        {
            Debug.Log($"BaseAttackBehavior ({attackerName}): 播放攻击特效");
            attackEffect.PlayFeedbacks();
        }
        else
        {
            Debug.LogWarning($"BaseAttackBehavior ({attackerName}): 攻击特效为空");
        }
    }
}
