using UnityEngine;

/// <summary>
/// 原子行为：保持静止
/// 不进行任何移动，用于间歇移动、等待等场景
/// </summary>
public class IdleBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 执行静止行为
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        // ✅ 目标位置为当前位置（3D转2D：使用 x 和 z，忽略 y）
        Vector3 currentPos = enemyTransform.position;
        targetPosition = new Vector2(currentPos.x, currentPos.z);
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return BehaviorStatus.Failure;
        }
        
        // 设置为静止状态
        runtimeState.isMoving = false;
        runtimeState.currentDirection = Vector2.zero;
        runtimeState.currentMovementState = "Idle";
        runtimeState.targetPosition = targetPosition;
        
        return BehaviorStatus.Success; // 静止也是成功执行
    }
}

