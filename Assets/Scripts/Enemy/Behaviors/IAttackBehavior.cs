using UnityEngine;

/// <summary>
/// 攻击行为接口
/// 定义敌人攻击行为的标准契约
/// </summary>
public interface IAttackBehavior
{
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据</param>
    /// <param name="attackRange">攻击范围组件</param>
    void ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange);
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据</param>
    /// <param name="attackRange">攻击范围组件</param>
    /// <param name="attackEffect">攻击特效</param>
    void ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, AttackRange attackRange, MoreMountains.Feedbacks.MMFeedbacks attackEffect);
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="attackRange">攻击范围组件</param>
    void CleanupAttack(Transform enemyTransform, AttackRange attackRange);
}
