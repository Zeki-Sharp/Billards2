using UnityEngine;

/// <summary>
/// 间歇移动行为
/// 敌人在一定回合移动，一定回合静止，交替进行
/// 移动时可以选择跟随或逃离玩家
/// </summary>
public class IntervalMovementBehavior : BaseMovementBehavior
{
    private EnemyData cachedEnemyData; // 缓存敌人数据用于获取速度
    private int currentRound = 0;       // 当前回合计数
    private bool isInIdlePhase;         // 当前是否处于静止阶段
    
    /// <summary>
    /// 执行间歇移动
    /// </summary>
    public override Vector2 ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData)
    {
        // 缓存敌人数据
        cachedEnemyData = enemyData;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return enemyTransform.position;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("IntervalMovementBehavior: 玩家Transform为空，无法执行移动");
            return enemyTransform.position;
        }
        
        // 第一次调用时初始化状态
        if (currentRound == 0)
        {
            isInIdlePhase = enemyData.intervalConfig.startWithIdle;
            Debug.Log($"IntervalMovementBehavior: 初始化，初始状态={( isInIdlePhase ? "静止" : "移动")}");
        }
        
        // 判断当前是否应该切换阶段
        int phaseRounds = isInIdlePhase ? enemyData.intervalConfig.idleRounds : enemyData.intervalConfig.moveRounds;
        
        currentRound++;
        Debug.Log($"IntervalMovementBehavior: 当前回合={currentRound}, 阶段={( isInIdlePhase ? "静止" : "移动")}, 阶段总回合={phaseRounds}");
        
        // 检查是否达到切换阶段的回合数
        if (currentRound > phaseRounds)
        {
            // 切换阶段
            isInIdlePhase = !isInIdlePhase;
            currentRound = 1; // 重置为新阶段的第一回合
            Debug.Log($"IntervalMovementBehavior: 切换阶段到={( isInIdlePhase ? "静止" : "移动")}");
        }
        
        // 如果当前处于静止阶段，不移动
        if (isInIdlePhase)
        {
            Debug.Log("IntervalMovementBehavior: 当前处于静止阶段，保持位置");
            SetMoving(false);
            currentDirection = Vector2.zero;
            return enemyTransform.position;
        }
        
        // 移动阶段：根据配置的移动模式执行移动
        Vector2 targetPosition;
        
        if (enemyData.intervalConfig.movementMode == IntervalMovementMode.Follow)
        {
            // 跟随模式
            targetPosition = ExecuteFollowMovement(enemyTransform, playerTransform, enemyData);
        }
        else
        {
            // 逃离模式
            targetPosition = ExecuteFleeMovement(enemyTransform, playerTransform, enemyData);
        }
        
        SetMoving(true);
        return targetPosition;
    }
    
    /// <summary>
    /// 执行跟随移动逻辑
    /// </summary>
    private Vector2 ExecuteFollowMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData)
    {
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果已经在最小距离内，不移动
        if (distanceToPlayer <= enemyData.intervalConfig.minDistance)
        {
            Debug.Log($"IntervalMovementBehavior-Follow: 已在最小距离内 ({distanceToPlayer} <= {enemyData.intervalConfig.minDistance})，不移动");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        // 计算向玩家移动的方向
        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
        currentDirection = direction;
        
        // 计算实际移动距离：确保不会超过最小距离
        float actualMoveDistance = Mathf.Min(enemyData.intervalConfig.moveDistance, distanceToPlayer - enemyData.intervalConfig.minDistance);
        
        // 如果计算出的移动距离太小，不移动
        if (actualMoveDistance <= 0.01f)
        {
            Debug.Log($"IntervalMovementBehavior-Follow: 移动距离太小 ({actualMoveDistance})，不移动");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        Vector2 targetPosition = CalculateTargetPosition(enemyTransform.position, direction, actualMoveDistance);
        Debug.Log($"IntervalMovementBehavior-Follow: 向玩家移动，方向: {direction}, 目标位置: {targetPosition}");
        
        return targetPosition;
    }
    
    /// <summary>
    /// 执行逃离移动逻辑
    /// </summary>
    private Vector2 ExecuteFleeMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData)
    {
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > enemyData.intervalConfig.triggerDistance)
        {
            Debug.Log($"IntervalMovementBehavior-Flee: 玩家距离过远 ({distanceToPlayer} > {enemyData.intervalConfig.triggerDistance})，不移动");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        // 计算远离玩家的方向
        Vector2 direction = (enemyTransform.position - playerTransform.position).normalized;
        currentDirection = direction;
        
        Vector2 targetPosition = CalculateTargetPosition(enemyTransform.position, direction, enemyData.intervalConfig.moveDistance);
        Debug.Log($"IntervalMovementBehavior-Flee: 远离玩家移动，方向: {direction}, 目标位置: {targetPosition}");
        
        return targetPosition;
    }
    
    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public override float GetCurrentMoveSpeed()
    {
        if (cachedEnemyData == null) return 3f;
        return cachedEnemyData.intervalConfig.moveSpeed;
    }
    
    /// <summary>
    /// 重置回合计数（用于敌人重生或重新初始化）
    /// </summary>
    public void ResetRoundCounter()
    {
        currentRound = 0;
        Debug.Log("IntervalMovementBehavior: 回合计数已重置");
    }
}

