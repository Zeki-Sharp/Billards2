using UnityEngine;

/// <summary>
/// 原子行为：向目标移动
/// 可配置最小距离限制，避免过度靠近
/// </summary>
public class MoveTowardsBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 最小距离配置（单位）
    /// 如果已经在此距离内，则不移动
    /// </summary>
    [SerializeField] private float minDistance = 1.0f;
    
    /// <summary>
    /// 移动距离配置（单位）
    /// 每回合移动的距离
    /// </summary>
    [SerializeField] private float moveDistance = 2.0f;
    
    /// <summary>
    /// 执行向目标移动
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
            Debug.LogWarning("[MoveTowardsBehavior] 玩家Transform为空");
            return BehaviorStatus.Failure;
        }
        
        // 从配置中读取参数（优先使用原子行为配置，回退到内置默认值）
        float actualMinDistance = levelConfig.moveTowardsConfig?.minDistance ?? minDistance;
        float actualMoveDistance = levelConfig.moveTowardsConfig?.moveDistance ?? moveDistance;
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果已经在最小距离内，不移动
        if (distanceToPlayer <= actualMinDistance)
        {
            runtimeState.isMoving = false;
            runtimeState.currentDirection = Vector2.zero;
            runtimeState.currentMovementState = "MoveTowards_Reached";
            return BehaviorStatus.Success; // 已到达目标
        }
        
        // 计算向玩家移动的方向
        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        // 计算实际移动距离：确保不会超过最小距离
        float clampedMoveDistance = Mathf.Min(actualMoveDistance, distanceToPlayer - actualMinDistance);
        
        // 如果计算出的移动距离太小，不移动
        if (clampedMoveDistance <= 0.01f)
        {
            runtimeState.isMoving = false;
            runtimeState.currentMovementState = "MoveTowards_Reached";
            return BehaviorStatus.Success; // 已到达目标
        }
        
        // 计算目标位置
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, clampedMoveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "MoveTowards_Moving";
        
        return BehaviorStatus.Success; // 本回合移动完成
    }
}

