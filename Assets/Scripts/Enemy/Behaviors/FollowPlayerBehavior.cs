using UnityEngine;

/// <summary>
/// 跟随玩家移动行为
/// 敌人会向玩家方向移动
/// </summary>
public class FollowPlayerBehavior : BaseMovementBehavior
{
    private EnemyData cachedEnemyData; // 缓存敌人数据用于获取速度
    
    /// <summary>
    /// 执行跟随玩家移动
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
            Debug.LogWarning("FollowPlayerBehavior: 玩家Transform为空，无法执行跟随移动");
            return enemyTransform.position;
        }
        
        // 计算与玩家的距离
        float distanceToPlayer = Vector2.Distance(enemyTransform.position, playerTransform.position);
        
        // 如果已经在最小距离内，不移动
        if (distanceToPlayer <= enemyData.followConfig.minDistance)
        {
            Debug.Log($"FollowPlayerBehavior: 已在最小距离内 ({distanceToPlayer} <= {enemyData.followConfig.minDistance})，不移动");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        // 计算向玩家移动的方向
        Vector2 direction = (playerTransform.position - enemyTransform.position).normalized;
        currentDirection = direction;
        
        // 计算实际移动距离：确保不会超过最小距离
        float actualMoveDistance = Mathf.Min(enemyData.followConfig.moveDistance, distanceToPlayer - enemyData.followConfig.minDistance);
        
        // 如果计算出的移动距离太小，不移动
        if (actualMoveDistance <= 0.01f)
        {
            Debug.Log($"FollowPlayerBehavior: 移动距离太小 ({actualMoveDistance})，不移动");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        // 使用计算出的实际移动距离
        Vector2 targetPosition = CalculateTargetPosition(enemyTransform.position, direction, actualMoveDistance);
        
        // 设置移动状态
        SetMoving(true);
        
        Debug.Log($"FollowPlayerBehavior: 向玩家移动，方向: {direction}, 目标位置: {targetPosition}, 距离: {distanceToPlayer}");
        
        return targetPosition;
    }
    
    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    public override float GetCurrentMoveSpeed()
    {
        if (cachedEnemyData == null) return 3f;
        return cachedEnemyData.followConfig.moveSpeed;
    }
}
