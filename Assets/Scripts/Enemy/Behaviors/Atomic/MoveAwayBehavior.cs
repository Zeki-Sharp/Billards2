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
        // ✅ 默认目标位置为当前位置（3D转2D：使用 x 和 z，忽略 y）
        Vector3 enemyPos3D = enemyTransform.position;
        Vector2 enemyPos2D = new Vector2(enemyPos3D.x, enemyPos3D.z);
        targetPosition = enemyPos2D;
        
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
        
        // 从配置中读取参数（优先使用阶段配置，回退到内置默认值）
        MoveAwayConfig config = runtimeState.currentMoveAwayConfig;
        float actualTriggerDistance = config?.triggerDistance ?? triggerDistance;
        float actualMoveDistance = config?.moveDistance ?? moveDistance;
        
        // ✅ 计算与玩家的距离（3D转2D：使用 x 和 z）
        Vector3 playerPos3D = playerTransform.position;
        Vector2 playerPos2D = new Vector2(playerPos3D.x, playerPos3D.z);
        float distanceToPlayer = Vector2.Distance(enemyPos2D, playerPos2D);
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > actualTriggerDistance)
        {
            runtimeState.isMoving = false;
            runtimeState.currentDirection = Vector2.zero;
            runtimeState.currentMovementState = "MoveAway_Safe";
            return BehaviorStatus.Success; // 安全距离，无需逃离
        }
        
        // ✅ 计算远离玩家的方向（2D XZ 平面）
        Vector2 direction = (enemyPos2D - playerPos2D).normalized;
        runtimeState.currentDirection = direction;
        
        // ✅ 计算目标位置（2D XZ 平面）
        targetPosition = CalculateTargetPosition(enemyPos2D, direction, actualMoveDistance);
        runtimeState.targetPosition = targetPosition;
        runtimeState.isMoving = true;
        runtimeState.currentMovementState = "MoveAway_Fleeing";
        
        return BehaviorStatus.Success; // 本回合逃离完成
    }
}

