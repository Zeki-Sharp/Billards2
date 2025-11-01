using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 组合器：选择执行行为
/// 按顺序尝试所有子行为，任一成功即成功
/// 适用于需要尝试多个备选方案的场景（如 Flee 行为）
/// </summary>
public class SelectorBehavior : BaseMovementBehavior
{
    [Header("组合器配置")]
    [Tooltip("子行为列表（按优先级顺序）")]
    [SerializeField] private List<BaseMovementBehavior> childBehaviors = new List<BaseMovementBehavior>();
    
    // 当前执行索引（存储在 RuntimeState 中）
    private const string SELECTOR_INDEX_KEY = "SelectorBehavior_CurrentIndex";
    
    /// <summary>
    /// 执行选择行为
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
            Debug.LogWarning("[SelectorBehavior] 没有子行为");
            return BehaviorStatus.Failure; // 空选择器视为失败
        }
        
        // 获取当前执行索引
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        int currentIndex = blackboard.TryGet<int>(SELECTOR_INDEX_KEY, out var index) ? index : 0;
        
        // 从当前索引开始尝试所有子行为
        for (int i = currentIndex; i < childBehaviors.Count; i++)
        {
            BaseMovementBehavior currentBehavior = childBehaviors[i];
            if (currentBehavior == null)
            {
                Debug.LogWarning($"[SelectorBehavior] 子行为 [{i}] 为空");
                continue; // 跳过空行为
            }
            
            BehaviorStatus childStatus = currentBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
            
            // 如果子行为成功，整个选择器成功
            if (childStatus == BehaviorStatus.Success)
            {
                ResetSelector(blackboard);
                return BehaviorStatus.Success;
            }
            
            // 如果子行为还在运行，保存索引并继续运行
            if (childStatus == BehaviorStatus.Running)
            {
                blackboard.Set(SELECTOR_INDEX_KEY, i);
                return BehaviorStatus.Running;
            }
            
            // 子行为失败，尝试下一个
            // (继续循环)
        }
        
        // 所有子行为都失败，选择器失败
        ResetSelector(blackboard);
        return BehaviorStatus.Failure;
    }
    
    /// <summary>
    /// 重置选择器索引
    /// </summary>
    private void ResetSelector(Blackboard blackboard)
    {
        blackboard.Set(SELECTOR_INDEX_KEY, 0);
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

