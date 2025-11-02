using UnityEngine;
using System;

/// <summary>
/// 装饰器：条件判断
/// 只有满足条件时才执行子行为
/// </summary>
public class ConditionalDecorator : BaseMovementBehavior
{
    private IMovementBehavior childBehavior;
    private BehaviorConditionConfig condition;
    
    // 旧的配置字段（兼容性保留）
    [System.Obsolete("使用 ConditionConfig 替代")]
    private Func<Transform, Transform, EnemyRuntimeState, bool> customCondition;
    
    /// <summary>
    /// 构造函数（代码配置用）
    /// </summary>
    public ConditionalDecorator(IMovementBehavior child, BehaviorConditionConfig config)
    {
        childBehavior = child;
        condition = config;
    }
    
    /// <summary>
    /// 执行条件装饰行为
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        targetPosition = enemyTransform.position;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return BehaviorStatus.Failure;
        }
        
        if (childBehavior == null)
        {
            Debug.LogError("[ConditionalDecorator] childBehavior 未设置");
            return BehaviorStatus.Failure;
        }
        
        if (condition == null)
        {
            Debug.LogError("[ConditionalDecorator] condition 未设置");
            return BehaviorStatus.Failure;
        }
        
        // 检查条件
        bool conditionMet = condition.Evaluate(enemyTransform, playerTransform, runtimeState);
        
        // 如果条件不满足，返回失败
        if (!conditionMet)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Failure; // 条件不满足
        }
        
        // 条件满足，执行子行为
        return childBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
    }
}

