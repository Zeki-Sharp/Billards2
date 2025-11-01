using UnityEngine;

/// <summary>
/// 移动行为抽象基类
/// 提供通用的移动行为实现
/// 状态管理已迁移到 EnemyRuntimeState
/// </summary>
public abstract class BaseMovementBehavior : IMovementBehavior
{
    /// <summary>
    /// 执行移动行为 - 抽象方法，由子类实现
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据（共享配置）</param>
    /// <param name="levelConfig">等级配置</param>
    /// <param name="runtimeState">运行时状态（传入/传出）</param>
    /// <param name="targetPosition">移动目标位置（输出）</param>
    /// <returns>行为执行状态</returns>
    public abstract BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition);
    
    /// <summary>
    /// 计算移动目标位置
    /// </summary>
    /// <param name="enemyPosition">敌人当前位置</param>
    /// <param name="direction">移动方向</param>
    /// <param name="moveDistance">移动距离</param>
    /// <returns>目标位置</returns>
    protected Vector2 CalculateTargetPosition(Vector2 enemyPosition, Vector2 direction, float moveDistance)
    {
        return enemyPosition + direction * moveDistance;
    }
    
    /// <summary>
    /// 验证移动参数
    /// </summary>
    /// <param name="enemyTransform">敌人Transform</param>
    /// <param name="playerTransform">玩家Transform</param>
    /// <param name="enemyData">敌人数据</param>
    /// <returns>参数是否有效</returns>
    protected bool ValidateMovementParams(Transform enemyTransform, Transform playerTransform, EnemyData enemyData)
    {
        if (enemyTransform == null)
        {
            Debug.LogError("BaseMovementBehavior: 敌人Transform为空");
            return false;
        }
        
        if (enemyData == null)
        {
            Debug.LogError("BaseMovementBehavior: 敌人数据为空");
            return false;
        }
        
        return true;
    }
}
