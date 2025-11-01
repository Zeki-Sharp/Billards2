using UnityEngine;

/// <summary>
/// 原子行为：远离目标移动
/// 可配置触发距离，只有目标在此距离内才触发逃离
/// </summary>
public class MoveAwayBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 触发距离配置（单位）
    /// 只有玩家在此距离内才会触发逃离
    /// </summary>
    [SerializeField] private float triggerDistance = 5.0f;
    
    /// <summary>
    /// 移动距离配置（单位）
    /// 每回合逃离的距离
    /// </summary>
    [SerializeField] private float moveDistance = 2.0f;
    
    /// <summary>
    /// 执行远离目标移动
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
            Debug.LogWarning("[MoveAwayBehavior] 玩家Transform为空");
            return BehaviorStatus.Failure;
        }
        
        // 从配置中读取参数（优先使用原子行为配置，回退到内置默认值）
        float actualTriggerDistance = levelConfig.moveAwayConfig?.triggerDistance ?? triggerDistance;
        float actualMoveDistance = levelConfig.moveAwayConfig?.moveDistance ?? moveDistance;
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > actualTriggerDistance)
        {
            runtimeState.isMoving = false;
            runtimeState.currentDirection = Vector2.zero;
            runtimeState.currentMovementState = "MoveAway_Safe";
            return BehaviorStatus.Success; // 安全距离，无需逃离
        }
        
        // 计算远离玩家的方向
        Vector2 direction = (enemyTransform.position - playerTransform.position).normalized;
        runtimeState.currentDirection = direction;
        
        // 计算目标位置
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, actualMoveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "MoveAway_Fleeing";
        
        return BehaviorStatus.Success; // 本回合逃离完成
    }
}

