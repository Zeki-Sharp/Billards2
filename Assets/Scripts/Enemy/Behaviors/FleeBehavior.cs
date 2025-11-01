using UnityEngine;

/// <summary>
/// 逃跑移动行为
/// 敌人会远离玩家方向移动，如果离玩家太远会接近玩家
/// </summary>
public class FleeBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 执行逃跑移动
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
            Debug.LogWarning("FleeBehavior: 玩家Transform为空，无法执行逃跑移动");
            return BehaviorStatus.Failure;
        }
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        Vector2 direction;
        
        // 检查是否需要接近玩家（如果离玩家太远）
        if (levelConfig.fleeConfig.approachWhenFar && distanceToPlayer > levelConfig.fleeConfig.approachDistance)
        {
            // 计算向玩家移动的方向
            direction = (playerTransform.position - enemyTransform.position).normalized;
            runtimeState.currentDirection = direction;
            
            // 使用接近玩家的专用距离参数
            targetPosition = CalculateTargetPosition(enemyTransform.position, direction, levelConfig.fleeConfig.approachMoveDistance);
            runtimeState.targetPosition = targetPosition;
            runtimeState.isMoving = true;
            runtimeState.currentMovementState = "Approaching"; // 标记为接近模式
            
            return BehaviorStatus.Success;
        }
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > levelConfig.fleeConfig.triggerDistance)
        {
            runtimeState.isMoving = false;
            runtimeState.currentDirection = Vector2.zero;
            runtimeState.currentMovementState = "Idle";
            return BehaviorStatus.Success; // 保持位置也是成功
        }
        
        // 计算远离玩家的方向
        direction = (enemyTransform.position - playerTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        // 使用逃跑移动的专用距离参数
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, levelConfig.fleeConfig.moveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "Fleeing"; // 标记为逃跑模式
        
        return BehaviorStatus.Success;
    }
}
