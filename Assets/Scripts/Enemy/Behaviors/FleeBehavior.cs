using UnityEngine;

/// <summary>
/// 逃跑移动行为
/// 敌人会远离玩家方向移动
/// </summary>
public class FleeBehavior : BaseMovementBehavior
{
    /// <summary>
    /// 执行逃跑移动
    /// </summary>
    public override Vector2 ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData)
    {
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
        
        // 如果玩家距离超过触发距离，不逃跑
        if (distanceToPlayer > enemyData.fleeConfig.triggerDistance)
        {
            Debug.Log($"FleeBehavior: 玩家距离过远 ({distanceToPlayer} > {enemyData.fleeConfig.triggerDistance})，不逃跑");
            SetMoving(false);
            return enemyTransform.position;
        }
        
        // 计算远离玩家的方向
        Vector2 direction = (enemyTransform.position - playerTransform.position).normalized;
        currentDirection = direction;
        
        // 使用逃跑移动的专用距离参数
        Vector2 targetPosition = CalculateTargetPosition(enemyTransform.position, direction, enemyData.fleeConfig.moveDistance);
        
        // 设置移动状态
        SetMoving(true);
        
        Debug.Log($"FleeBehavior: 远离玩家移动，方向: {direction}, 目标位置: {targetPosition}, 距离: {distanceToPlayer}");
        
        return targetPosition;
    }
}
