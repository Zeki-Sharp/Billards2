using UnityEngine;

/// <summary>
/// 攻击行为接口
/// 定义敌人攻击行为的标准契约
/// 状态管理通过 EnemyRuntimeState 进行
/// </summary>
public interface IAttackBehavior
{
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据（共享配置）</param>
    /// <param name="levelConfig">等级配置</param>
    /// <param name="attackRange">攻击范围组件</param>
    /// <param name="runtimeState">运行时状态（传入/传出）</param>
    /// <returns>行为执行状态</returns>
    BehaviorStatus ExecuteTelegraph(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, EnemyRuntimeState runtimeState);
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据（共享配置）</param>
    /// <param name="levelConfig">等级配置</param>
    /// <param name="attackRange">攻击范围组件</param>
    /// <param name="attackEffect">攻击特效</param>
    /// <param name="runtimeState">运行时状态（传入/传出）</param>
    /// <returns>行为执行状态</returns>
    BehaviorStatus ExecuteAttack(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, AttackRange attackRange, MoreMountains.Feedbacks.MMFeedbacks attackEffect, EnemyRuntimeState runtimeState);
    
    /// <summary>
    /// 清理攻击状态
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="attackRange">攻击范围组件</param>
    /// <param name="runtimeState">运行时状态（传入/传出）</param>
    /// <returns>行为执行状态</returns>
    BehaviorStatus CleanupAttack(Transform enemyTransform, AttackRange attackRange, EnemyRuntimeState runtimeState);
}
