using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 组合器：顺序执行行为
/// 按顺序执行所有子行为，全部成功才成功
/// 适用于需要顺序执行多个步骤的场景
/// </summary>
public class SequenceBehavior : BaseMovementBehavior
{
    [Header("组合器配置")]
    [Tooltip("子行为列表（按顺序执行）")]
    [SerializeField] private List<BaseMovementBehavior> childBehaviors = new List<BaseMovementBehavior>();
    
    // 当前执行索引（存储在 RuntimeState 中）
    private const string SEQUENCE_INDEX_KEY = "SequenceBehavior_CurrentIndex";
    
    /// <summary>
    /// 执行顺序行为
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        targetPosition = enemyTransform.position;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return BehaviorStatus.Failure;
        }
        
        if (childBehaviors == null || childBehaviors.Count == 0)
        {
            Debug.LogWarning("[SequenceBehavior] 没有子行为");
            return BehaviorStatus.Success; // 空序列视为成功
        }
        
        // 获取当前执行索引
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        int currentIndex = blackboard.TryGet<int>(SEQUENCE_INDEX_KEY, out var index) ? index : 0;
        
        // 如果索引超出范围，重置并返回成功
        if (currentIndex >= childBehaviors.Count)
        {
            ResetSequence(blackboard);
            return BehaviorStatus.Success; // 所有行为已完成
        }
        
        // 执行当前子行为
        BaseMovementBehavior currentBehavior = childBehaviors[currentIndex];
        if (currentBehavior == null)
        {
            Debug.LogWarning($"[SequenceBehavior] 子行为 [{currentIndex}] 为空");
            // 跳过空行为，继续下一个
            currentIndex++;
            blackboard.Set(SEQUENCE_INDEX_KEY, currentIndex);
            return BehaviorStatus.Running;
        }
        
        BehaviorStatus childStatus = currentBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
        
        // 如果子行为失败，整个序列失败
        if (childStatus == BehaviorStatus.Failure)
        {
            ResetSequence(blackboard);
            return BehaviorStatus.Failure;
        }
        
        // 如果子行为还在运行，继续运行
        if (childStatus == BehaviorStatus.Running)
        {
            return BehaviorStatus.Running;
        }
        
        // 子行为成功，移动到下一个
        currentIndex++;
        blackboard.Set(SEQUENCE_INDEX_KEY, currentIndex);
        
        // 检查是否完成所有行为
        if (currentIndex >= childBehaviors.Count)
        {
            ResetSequence(blackboard);
            return BehaviorStatus.Success; // 所有行为完成
        }
        
        // 还有更多行为要执行
        return BehaviorStatus.Running;
    }
    
    /// <summary>
    /// 重置序列索引
    /// </summary>
    private void ResetSequence(Blackboard blackboard)
    {
        blackboard.Set(SEQUENCE_INDEX_KEY, 0);
    }
    
    /// <summary>
    /// 添加子行为（用于代码配置）
    /// </summary>
    public void AddChildBehavior(BaseMovementBehavior behavior)
    {
        if (childBehaviors == null)
        {
            childBehaviors = new List<BaseMovementBehavior>();
        }
        childBehaviors.Add(behavior);
    }
    
    /// <summary>
    /// 设置子行为列表（用于代码配置）
    /// </summary>
    public void SetChildBehaviors(List<BaseMovementBehavior> behaviors)
    {
        childBehaviors = behaviors;
    }
}

