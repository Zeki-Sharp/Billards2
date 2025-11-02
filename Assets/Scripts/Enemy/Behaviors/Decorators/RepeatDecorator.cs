using UnityEngine;

/// <summary>
/// 装饰器：重复执行行为 N 次
/// 用于实现间歇移动等需要重复执行的场景
/// </summary>
public class RepeatDecorator : BaseMovementBehavior
{
    [Header("装饰器配置")]
    [Tooltip("要装饰的子行为")]
    private IMovementBehavior childBehavior;
    
    [Tooltip("重复次数（回合数）")]
    [SerializeField] private int repeatCount = 3;
    
    // 当前执行次数（存储在 RuntimeState 中）
    private const string REPEAT_COUNT_KEY = "RepeatDecorator_CurrentCount";
    
    /// <summary>
    /// 执行重复行为
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
            Debug.LogError("[RepeatDecorator] childBehavior 未设置");
            return BehaviorStatus.Failure;
        }
        
        // 获取当前执行次数（从 Blackboard 中读取）
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        int currentCount = blackboard.TryGet<int>(REPEAT_COUNT_KEY, out var count) ? count : 0;
        
        // 如果还没开始，初始化计数
        if (currentCount == 0)
        {
            currentCount = 1;
            blackboard.Set(REPEAT_COUNT_KEY, currentCount);
        }
        
        // 执行子行为
        BehaviorStatus childStatus = childBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
        
        // 如果子行为失败，整个装饰器失败
        if (childStatus == BehaviorStatus.Failure)
        {
            ResetCount(blackboard);
            return BehaviorStatus.Failure;
        }
        
        // 如果子行为还在运行中，继续运行
        if (childStatus == BehaviorStatus.Running)
        {
            return BehaviorStatus.Running;
        }
        
        // 子行为成功，检查是否达到重复次数
        if (currentCount >= repeatCount)
        {
            // 完成所有重复，重置计数并返回成功
            ResetCount(blackboard);
            return BehaviorStatus.Success;
        }
        else
        {
            // 还需要继续重复
            currentCount++;
            blackboard.Set(REPEAT_COUNT_KEY, currentCount);
            // 回合制游戏：当前回合的行为已完成，返回 Success
            // 下一回合会继续执行（通过 currentCount 跟踪进度）
            return BehaviorStatus.Success;
        }
    }
    
    /// <summary>
    /// 重置计数
    /// </summary>
    private void ResetCount(Blackboard blackboard)
    {
        blackboard.Set(REPEAT_COUNT_KEY, 0);
    }
    
    /// <summary>
    /// 设置子行为（用于代码配置）
    /// </summary>
    public void SetChildBehavior(IMovementBehavior behavior)
    {
        childBehavior = behavior;
    }
    
    /// <summary>
    /// 设置重复次数（用于代码配置）
    /// </summary>
    public void SetRepeatCount(int count)
    {
        repeatCount = count;
    }
}

