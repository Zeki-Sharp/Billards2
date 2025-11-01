using UnityEngine;

/// <summary>
/// 间歇移动行为
/// 敌人在一定回合移动，一定回合静止，交替进行
/// 移动时可以选择跟随或逃离玩家
/// 回合状态存储在 EnemyRuntimeState 中
/// </summary>
public class IntervalMovementBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 执行间歇移动
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
            Debug.LogWarning("IntervalMovementBehavior: 玩家Transform为空，无法执行移动");
            return BehaviorStatus.Failure;
        }
        
        // 第一次调用时初始化状态
        if (runtimeState.intervalCurrentRound == 0)
        {
            runtimeState.intervalIsInIdlePhase = levelConfig.intervalConfig.startWithIdle;
        }
        
        // 判断当前是否应该切换阶段
        int phaseRounds = runtimeState.intervalIsInIdlePhase ? levelConfig.intervalConfig.idleRounds : levelConfig.intervalConfig.moveRounds;
        
        runtimeState.intervalCurrentRound++;
        
        // 检查是否达到切换阶段的回合数
        if (runtimeState.intervalCurrentRound > phaseRounds)
        {
            // 切换阶段
            runtimeState.intervalIsInIdlePhase = !runtimeState.intervalIsInIdlePhase;
            runtimeState.intervalCurrentRound = 1; // 重置为新阶段的第一回合
        }
        
        // 如果当前处于静止阶段，不移动
        if (runtimeState.intervalIsInIdlePhase)
        {
            runtimeState.isMoving = false;
            runtimeState.currentDirection = Vector2.zero;
            runtimeState.currentMovementState = "Idle";
            return BehaviorStatus.Success; // 静止也是成功执行
        }
        
        // 移动阶段：根据配置的移动模式执行移动
        if (levelConfig.intervalConfig.movementMode == IntervalMovementMode.Follow)
        {
            // 跟随模式
            return ExecuteFollowMovement(enemyTransform, playerTransform, levelConfig, runtimeState, out targetPosition);
        }
        else
        {
            // 逃离模式
            return ExecuteFleeMovement(enemyTransform, playerTransform, levelConfig, runtimeState, out targetPosition);
        }
    }
    
    /// <summary>
    /// 执行跟随移动逻辑
    /// </summary>
    private BehaviorStatus ExecuteFollowMovement(Transform enemyTransform, Transform playerTransform, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        targetPosition = enemyTransform.position;
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果已经在最小距离内，不移动
        if (distanceToPlayer <= levelConfig.intervalConfig.minDistance)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Success;
        }
        
        // 计算向玩家移动的方向
        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        // 计算实际移动距离：确保不会超过最小距离
        float actualMoveDistance = Mathf.Min(levelConfig.intervalConfig.moveDistance, distanceToPlayer - levelConfig.intervalConfig.minDistance);
        
        // 如果计算出的移动距离太小，不移动
        if (actualMoveDistance <= 0.01f)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Success;
        }
        
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, actualMoveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "Following";
        
        return BehaviorStatus.Success;
    }
    
    /// <summary>
    /// 执行逃离移动逻辑
    /// </summary>
    private BehaviorStatus ExecuteFleeMovement(Transform enemyTransform, Transform playerTransform, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        targetPosition = enemyTransform.position;
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > levelConfig.intervalConfig.triggerDistance)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Success;
        }
        
        // 计算远离玩家的方向
        Vector2 direction = (enemyTransform.position - playerTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, levelConfig.intervalConfig.moveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "Fleeing";
        
        return BehaviorStatus.Success;
    }
}

