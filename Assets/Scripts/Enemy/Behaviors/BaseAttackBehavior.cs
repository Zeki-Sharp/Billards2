using UnityEngine;
using MoreMountains.Feedbacks;

/// <summary>
/// 攻击行为抽象基类
/// 提供通用的攻击行为实现
/// 状态管理已迁移到 EnemyRuntimeState
/// </summary>
public abstract class BaseAttackBehavior : IAttackBehavior
{
    /// <summary>
    /// 执行预告阶段 - 抽象方法，由子类实现
    /// </summary>
    public abstract BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState);
    
    /// <summary>
    /// 执行攻击阶段 - 抽象方法，由子类实现
    /// </summary>
    public abstract BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MMFeedbacks attackEffect, EnemyRuntimeState runtimeState);
    
    /// <summary>
    /// 清理攻击状态 - 抽象方法，由子类实现
    /// </summary>
    public abstract BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState);
    
    /// <summary>
    /// 验证攻击参数
    /// </summary>
    protected bool ValidateAttackParams(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange)
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
        
        if (levelConfig == null)
        {
            Debug.LogError("BaseAttackBehavior: EnemyLevelConfig为空");
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
