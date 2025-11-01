using UnityEngine;
using System;

/// <summary>
/// 条件类型
/// </summary>
public enum ConditionType
{
    DistanceLessThan,      // 距离小于
    DistanceGreaterThan,   // 距离大于
    DistanceInRange,       // 距离在范围内
    StateEquals,           // 状态等于
    StateNotEquals,        // 状态不等于
    Custom                 // 自定义条件（通过委托）
}

/// <summary>
/// 装饰器：条件判断
/// 只有满足条件时才执行子行为
/// </summary>
public class ConditionalDecorator : BaseMovementBehavior
{
    [Header("装饰器配置")]
    [Tooltip("要装饰的子行为")]
    [SerializeField] private BaseMovementBehavior childBehavior;
    
    [Header("条件配置")]
    [Tooltip("条件类型")]
    [SerializeField] private ConditionType conditionType = ConditionType.DistanceLessThan;
    
    [Tooltip("距离阈值（用于距离条件）")]
    [SerializeField] private float distanceThreshold = 5.0f;
    
    [Tooltip("距离范围最小值（用于 DistanceInRange）")]
    [SerializeField] private float distanceMin = 2.0f;
    
    [Tooltip("距离范围最大值（用于 DistanceInRange）")]
    [SerializeField] private float distanceMax = 5.0f;
    
    [Tooltip("状态键（用于状态条件）")]
    [SerializeField] private string stateKey = "";
    
    [Tooltip("状态期望值（用于状态条件）")]
    [SerializeField] private string stateValue = "";
    
    // 自定义条件委托（代码配置）
    private Func<Transform, Transform, EnemyRuntimeState, bool> customCondition;
    
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
        
        // 检查条件
        bool conditionMet = EvaluateCondition(enemyTransform, playerTransform, runtimeState);
        
        // 如果条件不满足，返回失败（或 Success，取决于设计）
        if (!conditionMet)
        {
            runtimeState.isMoving = false;
            return BehaviorStatus.Failure; // 条件不满足
        }
        
        // 条件满足，执行子行为
        return childBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
    }
    
    /// <summary>
    /// 评估条件是否满足
    /// </summary>
    private bool EvaluateCondition(Transform enemyTransform, Transform playerTransform, EnemyRuntimeState runtimeState)
    {
        switch (conditionType)
        {
            case ConditionType.DistanceLessThan:
                float distLess = Vector2.Distance(enemyTransform.position, playerTransform.position);
                return distLess < distanceThreshold;
                
            case ConditionType.DistanceGreaterThan:
                float distGreater = Vector2.Distance(enemyTransform.position, playerTransform.position);
                return distGreater > distanceThreshold;
                
            case ConditionType.DistanceInRange:
                float distRange = Vector2.Distance(enemyTransform.position, playerTransform.position);
                return distRange >= distanceMin && distRange <= distanceMax;
                
            case ConditionType.StateEquals:
                var blackboard = enemyTransform.gameObject.TryGetBlackboard();
                if (blackboard != null && blackboard.TryGet<string>(stateKey, out var value))
                {
                    return value == stateValue;
                }
                return false;
                
            case ConditionType.StateNotEquals:
                var blackboard2 = enemyTransform.gameObject.TryGetBlackboard();
                if (blackboard2 != null && blackboard2.TryGet<string>(stateKey, out var value2))
                {
                    return value2 != stateValue;
                }
                return false;
                
            case ConditionType.Custom:
                if (customCondition != null)
                {
                    return customCondition(enemyTransform, playerTransform, runtimeState);
                }
                Debug.LogWarning("[ConditionalDecorator] Custom 条件未设置");
                return false;
                
            default:
                return false;
        }
    }
    
    /// <summary>
    /// 设置子行为（用于代码配置）
    /// </summary>
    public void SetChildBehavior(BaseMovementBehavior behavior)
    {
        childBehavior = behavior;
    }
    
    /// <summary>
    /// 设置自定义条件（用于代码配置）
    /// </summary>
    public void SetCustomCondition(Func<Transform, Transform, EnemyRuntimeState, bool> condition)
    {
        customCondition = condition;
        conditionType = ConditionType.Custom;
    }
}

