using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 间歇移动行为 V2
/// 基于阶段序列和原子行为组合的灵活实现
/// 使用 SequenceBehavior + RepeatDecorator 管理阶段切换
/// </summary>
public class IntervalMovementBehavior_V2 : BaseMovementBehavior
{
    // Blackboard Key 常量
    private const string BEHAVIOR_TREE_KEY = "IntervalMovement_BehaviorTree";
    private const string IS_INITIALIZED_KEY = "IntervalMovement_Initialized";
    
    /// <summary>
    /// 执行间歇移动
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        // 默认目标位置为当前位置
        targetPosition = enemyTransform.position;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            return BehaviorStatus.Failure;
        }
        
        if (levelConfig.intervalConfig_V2 == null || levelConfig.intervalConfig_V2.phases == null || levelConfig.intervalConfig_V2.phases.Length == 0)
        {
            Debug.LogError("[IntervalMovementBehavior_V2] 配置无效：phases 为空");
            return BehaviorStatus.Failure;
        }
        
        // 获取 Blackboard
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        
        // 初始化行为树（仅第一次执行）
        if (!blackboard.TryGet<bool>(IS_INITIALIZED_KEY, out var isInitialized) || !isInitialized)
        {
            InitializeBehaviorTree(enemyTransform, levelConfig, blackboard);
        }
        
        // 获取缓存的行为树
        if (!blackboard.TryGet<SequenceBehavior>(BEHAVIOR_TREE_KEY, out var sequenceBehavior) || sequenceBehavior == null)
        {
            Debug.LogError("[IntervalMovementBehavior_V2] 行为树未初始化");
            return BehaviorStatus.Failure;
        }
        
        // 执行行为树
        BehaviorStatus status = sequenceBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
        
        // 处理循环逻辑
        if (status == BehaviorStatus.Success && levelConfig.intervalConfig_V2.loopPhases)
        {
            // 所有阶段完成，重置序列以便下一轮循环
            ResetBehaviorTree(blackboard);
            // 重新初始化
            InitializeBehaviorTree(enemyTransform, levelConfig, blackboard);
            // 继续执行（循环模式）
            return BehaviorStatus.Running;
        }
        
        return status;
    }
    
    /// <summary>
    /// 初始化行为树
    /// 根据配置创建 SequenceBehavior + RepeatDecorator + AtomicBehavior 组合
    /// </summary>
    private void InitializeBehaviorTree(Transform enemyTransform, EnemyLevelConfig levelConfig, Blackboard blackboard)
    {
        var config = levelConfig.intervalConfig_V2;
        
        // 创建主序列行为
        var sequenceBehavior = new SequenceBehavior();
        
        // 为每个阶段创建行为
        for (int i = 0; i < config.phases.Length; i++)
        {
            var phase = config.phases[i];
            
            // 创建原子行为
            BaseMovementBehavior atomicBehavior = CreateAtomicBehavior(phase);
            
            if (atomicBehavior == null)
            {
                Debug.LogWarning($"[IntervalMovementBehavior_V2] Phase {i}: 无法创建行为类型 {phase.behaviorType}");
                continue;
            }
            
            // 使用 RepeatDecorator 包装原子行为
            var repeatDecorator = new RepeatDecorator();
            repeatDecorator.SetChildBehavior(atomicBehavior);
            repeatDecorator.SetRepeatCount(phase.roundCount);
            
            // 添加到序列中
            sequenceBehavior.AddChildBehavior(repeatDecorator);
        }
        
        // 缓存到 Blackboard
        blackboard.Set(BEHAVIOR_TREE_KEY, sequenceBehavior);
        blackboard.Set(IS_INITIALIZED_KEY, true);
    }
    
    /// <summary>
    /// 创建原子行为实例
    /// </summary>
    private BaseMovementBehavior CreateAtomicBehavior(MovementPhaseConfig phaseConfig)
    {
        switch (phaseConfig.behaviorType)
        {
            case PhaseMovementType.Idle:
                return new IdleBehavior();
                
            case PhaseMovementType.MoveTowards:
                var moveTowards = new MoveTowardsBehavior();
                // 注意：参数已通过 levelConfig 传递，不需要在这里设置
                return moveTowards;
                
            case PhaseMovementType.MoveAway:
                var moveAway = new MoveAwayBehavior();
                // 注意：参数已通过 levelConfig 传递，不需要在这里设置
                return moveAway;
                
            default:
                Debug.LogError($"[IntervalMovementBehavior_V2] 未知的行为类型: {phaseConfig.behaviorType}");
                return null;
        }
    }
    
    /// <summary>
    /// 重置行为树
    /// 清理 Blackboard 中的行为树缓存，准备下一轮循环
    /// </summary>
    private void ResetBehaviorTree(Blackboard blackboard)
    {
        // 移除缓存的行为树
        blackboard.Set(BEHAVIOR_TREE_KEY, (SequenceBehavior)null);
        blackboard.Set(IS_INITIALIZED_KEY, false);
        
        // 清理 SequenceBehavior 和 RepeatDecorator 的内部状态
        // 这些会在重新初始化时自动清理
    }
}

