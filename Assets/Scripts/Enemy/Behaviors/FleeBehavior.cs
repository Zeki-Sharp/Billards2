using UnityEngine;

/// <summary>
/// 逃跑移动行为
/// 敌人会远离玩家方向移动，如果离玩家太远会接近玩家
/// </summary>
public class FleeBehavior : BaseMovementBehavior
{
    private bool isApproaching = false; // 当前是否在接近玩家
    private EnemyLevelConfig cachedLevelConfig; // 缓存等级配置用于获取速度
    
    /// <summary>
    /// 执行逃跑移动
    /// </summary>
    public override Vector2 ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig)
    {
        // 缓存等级配置
        cachedLevelConfig = levelConfig;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return enemyTransform.position;
        }
        
        if (playerTransform == null)
        {
            Debug.LogWarning("FleeBehavior: 玩家Transform为空，无法执行逃跑移动");
            return enemyTransform.position;
        }
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        Vector2 direction;
        Vector2 targetPosition;
        
        // 检查是否需要接近玩家（如果离玩家太远）
        if (levelConfig.fleeConfig.approachWhenFar && distanceToPlayer > levelConfig.fleeConfig.approachDistance)
        {
            Debug.Log($"FleeBehavior: 玩家距离过远 ({distanceToPlayer} > {levelConfig.fleeConfig.approachDistance})，向玩家移动");
            
            // 计算向玩家移动的方向
            direction = (playerTransform.position - enemyTransform.position).normalized;
            currentDirection = direction;
            
            // 使用接近玩家的专用距离参数
            targetPosition = CalculateTargetPosition(enemyTransform.position, direction, levelConfig.fleeConfig.approachMoveDistance);
            
            SetMoving(true);
            isApproaching = true; // 标记为接近模式
            Debug.Log($"FleeBehavior: 接近玩家，方向: {direction}, 目标位置: {targetPosition}, 移动距离: {levelConfig.fleeConfig.approachMoveDistance}");
            
            return targetPosition;
        }
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > levelConfig.fleeConfig.triggerDistance)
        {
            Debug.Log($"FleeBehavior: 玩家距离适中 ({distanceToPlayer} > {levelConfig.fleeConfig.triggerDistance})，保持位置");
            SetMoving(false);
            isApproaching = false;
            return enemyTransform.position;
        }
        
        // 计算远离玩家的方向
        direction = (enemyTransform.position - playerTransform.position).normalized;
        currentDirection = direction;
        
        // 使用逃跑移动的专用距离参数
        targetPosition = CalculateTargetPosition(enemyTransform.position, direction, levelConfig.fleeConfig.moveDistance);
        
        // 设置移动状态
        SetMoving(true);
        isApproaching = false; // 标记为逃跑模式
        
        Debug.Log($"FleeBehavior: 远离玩家移动，方向: {direction}, 目标位置: {targetPosition}, 距离: {distanceToPlayer}");
        
        return targetPosition;
    }
    
    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public override float GetCurrentMoveSpeed()
    {
        if (cachedLevelConfig == null) return 3f;
        
        // 根据当前行为模式返回对应的速度
        return isApproaching ? cachedLevelConfig.fleeConfig.approachSpeed : cachedLevelConfig.fleeConfig.moveSpeed;
    }
}
