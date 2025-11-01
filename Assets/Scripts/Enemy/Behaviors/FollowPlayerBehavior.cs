using UnityEngine;

/// <summary>
/// 跟随玩家移动行为
/// 敌人会向玩家方向移动
/// </summary>
public class FollowPlayerBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 执行跟随玩家移动
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        // 默认目标位置为当前位置
        targetPosition = enemyTransform.position;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return BehaviorStatus.Failure;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("FollowPlayerBehavior: 玩家Transform为空，无法执行跟随移动");
            return BehaviorStatus.Failure;
        }
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果已经在最小距离内，不移动
        if (distanceToPlayer <= levelConfig.followConfig.minDistance)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Success; // 已到达目标，视为成功
        }
        
        // 计算向玩家移动的方向
        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        // 计算实际移动距离：确保不会超过最小距离
        float actualMoveDistance = Mathf.Min(levelConfig.followConfig.moveDistance, distanceToPlayer - levelConfig.followConfig.minDistance);
        
        // 如果计算出的移动距离太小，不移动
        if (actualMoveDistance <= 0.01f)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Success; // 已到达目标
        }
        
        // 计算目标位置
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, actualMoveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        
        return BehaviorStatus.Success;
    }
}
