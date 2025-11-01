using UnityEngine;

/// <summary>
/// 移动行为接口
/// 定义敌人移动行为的标准契约
/// 状态管理通过 EnemyRuntimeState 进行，不再由接口提供状态查询方法
/// </summary>
public interface IMovementBehavior
{
    /// <summary>
    /// 执行移动行为
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据（共享配置）</param>
    /// <param name="levelConfig">等级配置</param>
    /// <param name="runtimeState">运行时状态（传入/传出）</param>
    /// <param name="targetPosition">移动目标位置（输出）</param>
    /// <returns>行为执行状态</returns>
    BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition);
}
