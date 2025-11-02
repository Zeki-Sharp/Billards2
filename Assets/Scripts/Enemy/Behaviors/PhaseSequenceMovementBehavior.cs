using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 阶段序列移动行为
/// 基于阶段序列和原子行为组合的统一实现
/// 支持顺序执行（Sequential）和条件选择（Conditional）两种模式
/// 使用 SequenceBehavior/SelectorBehavior + RepeatDecorator/ConditionalDecorator 管理阶段
/// </summary>
public class PhaseSequenceMovementBehavior : BaseMovementBehavior
{
    // Blackboard Key 常量
    private const string BEHAVIOR_TREE_KEY = "PhaseSequence_BehaviorTree";
    private const string IS_INITIALIZED_KEY = "PhaseSequence_Initialized";
    
    /// <summary>
    /// 执行阶段序列移动
    /// </summary>
    public override BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
    {
        // 默认目标位置为当前位置
        targetPosition = enemyTransform.position;
        
        // 验证参数
        if (!ValidateMovementParams(enemyTransform, playerTransform, enemyData))
        {
            Debug.LogWarning($"[PhaseSequence] {enemyTransform.name}: 参数验证失败");
            return BehaviorStatus.Failure;
        }
        
        if (levelConfig.phaseSequenceConfig == null || levelConfig.phaseSequenceConfig.phases == null || levelConfig.phaseSequenceConfig.phases.Length == 0)
        {
            Debug.LogError($"[PhaseSequence] {enemyTransform.name}: 配置无效（phases 为空）");
            return BehaviorStatus.Failure;
        }
        
        // 获取 Blackboard
        var blackboard = enemyTransform.gameObject.GetBlackboard();
        
        // 初始化行为树（仅第一次执行）
        if (!blackboard.TryGet<bool>(IS_INITIALIZED_KEY, out var isInitialized) || !isInitialized)
        {
            InitializeBehaviorTree(enemyTransform, levelConfig, blackboard);
        }
        
        // 获取缓存的行为树（可能是 Sequence 或 Selector）
        if (!blackboard.TryGet<IMovementBehavior>(BEHAVIOR_TREE_KEY, out var rootBehavior) || rootBehavior == null)
        {
            Debug.LogError($"[PhaseSequence] {enemyTransform.name}: 行为树未初始化");
            return BehaviorStatus.Failure;
        }
        
        // 执行行为树
        BehaviorStatus status = rootBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
        
        // 处理循环逻辑
        if (status == BehaviorStatus.Success && levelConfig.phaseSequenceConfig.loopPhases)
        {
            // 检查是否所有阶段都已完成（通过检查序列索引）
            if (blackboard.TryGet<int>("SequenceIndex", out var currentIndex) && currentIndex >= levelConfig.phaseSequenceConfig.phases.Length)
            {
                // 所有阶段完成，重置循环
                ResetBehaviorTree(blackboard);
                InitializeBehaviorTree(enemyTransform, levelConfig, blackboard);
            }
        }
        
        return status;
    }
    
    /// <summary>
    /// 初始化行为树
    /// 根据配置创建 SequenceBehavior + RepeatDecorator + AtomicBehavior 组合
    /// </summary>
    private void InitializeBehaviorTree(Transform enemyTransform, EnemyLevelConfig levelConfig, Blackboard blackboard)
    {
        var config = levelConfig.phaseSequenceConfig;
        
        // 根据选择模式创建不同的根行为
        IMovementBehavior rootBehavior;
        
        if (config.selectionMode == PhaseSelectionMode.Sequential)
        {
            // Sequential 模式：使用 Sequence + Repeat
            rootBehavior = BuildSequentialBehavior(config, levelConfig);
        }
        else // Conditional 模式
        {
            // Conditional 模式：使用 Selector + Conditional
            rootBehavior = BuildConditionalBehavior(config, levelConfig);
        }
        
        // 缓存到 Blackboard
        blackboard.Set(BEHAVIOR_TREE_KEY, rootBehavior);
        blackboard.Set(IS_INITIALIZED_KEY, true);
    }
    
    /// <summary>
    /// 构建顺序执行行为树（Sequential 模式）
    /// </summary>
    private IMovementBehavior BuildSequentialBehavior(PhaseSequenceConfig config, EnemyLevelConfig levelConfig)
    {
        var sequenceBehavior = new SequenceBehavior();
        
        // 为每个阶段创建行为
        for (int i = 0; i < config.phases.Length; i++)
        {
            var phase = config.phases[i];
            
            // 创建原子行为
            IMovementBehavior atomicBehavior = CreateAtomicBehavior(phase, levelConfig);
            
            if (atomicBehavior == null)
            {
                Debug.LogWarning($"[PhaseSequence] Phase {i}: 无法创建行为类型 {phase.behaviorType}");
                continue;
            }
            
            // 使用 RepeatDecorator 包装原子行为
            var repeatDecorator = new RepeatDecorator();
            repeatDecorator.SetChildBehavior(atomicBehavior);
            repeatDecorator.SetRepeatCount(phase.roundCount);
            
            // 添加到序列中
            sequenceBehavior.AddChildBehavior(repeatDecorator);
        }
        
        return sequenceBehavior;
    }
    
    /// <summary>
    /// 构建条件选择行为树（Conditional 模式）
    /// </summary>
    private IMovementBehavior BuildConditionalBehavior(PhaseSequenceConfig config, EnemyLevelConfig levelConfig)
    {
        var selectorBehavior = new SelectorBehavior();
        
        // 为每个阶段创建行为
        foreach (var phase in config.phases)
        {
            // 创建原子行为
            IMovementBehavior atomicBehavior = CreateAtomicBehavior(phase, levelConfig);
            
            if (atomicBehavior == null)
            {
                Debug.LogWarning($"[PhaseSequence] 无法创建行为类型 {phase.behaviorType}");
                continue;
            }
            
            // 如果有条件，使用 ConditionalDecorator 包装
            if (phase.condition != null)
            {
                var conditionalBehavior = new ConditionalDecorator(atomicBehavior, phase.condition);
                selectorBehavior.AddChildBehavior(conditionalBehavior);
            }
            else
            {
                // 无条件（Always），直接添加
                selectorBehavior.AddChildBehavior(atomicBehavior);
            }
        }
        
        return selectorBehavior;
    }
    
    /// <summary>
    /// 创建原子行为实例（带配置包装器）
    /// </summary>
    private IMovementBehavior CreateAtomicBehavior(MovementPhaseConfig phaseConfig, EnemyLevelConfig levelConfig)
    {
        switch (phaseConfig.behaviorType)
        {
            case PhaseMovementType.Idle:
                return new IdleBehavior();
                
            case PhaseMovementType.MoveTowards:
                // 使用包装器传递阶段配置
                return new PhaseAtomicBehaviorWrapper(
                    new MoveTowardsBehavior(),
                    phaseConfig.moveTowardsConfig,
                    null
                );
                
            case PhaseMovementType.MoveAway:
                // 使用包装器传递阶段配置
                return new PhaseAtomicBehaviorWrapper(
                    new MoveAwayBehavior(),
                    null,
                    phaseConfig.moveAwayConfig
                );
                
            default:
                Debug.LogError($"[PhaseSequence] 未知的行为类型: {phaseConfig.behaviorType}");
                return null;
        }
    }
    
    /// <summary>
    /// 原子行为包装器
    /// 用于临时替换 levelConfig 中的配置
    /// </summary>
    private class PhaseAtomicBehaviorWrapper : IMovementBehavior
    {
        private readonly IMovementBehavior innerBehavior;
        private readonly MoveTowardsConfig moveTowardsConfig;
        private readonly MoveAwayConfig moveAwayConfig;
        
        public PhaseAtomicBehaviorWrapper(IMovementBehavior inner, MoveTowardsConfig moveTowards, MoveAwayConfig moveAway)
        {
            innerBehavior = inner;
            moveTowardsConfig = moveTowards;
            moveAwayConfig = moveAway;
        }
        
        public BehaviorStatus ExecuteMovement(Transform enemyTransform, Transform playerTransform, EnemyData enemyData, EnemyLevelConfig levelConfig, EnemyRuntimeState runtimeState, out Vector2 targetPosition)
        {
            // 临时替换配置
            var originalMoveTowards = levelConfig.moveTowardsConfig;
            var originalMoveAway = levelConfig.moveAwayConfig;
            
            if (moveTowardsConfig != null)
            {
                levelConfig.moveTowardsConfig = moveTowardsConfig;
            }
            
            if (moveAwayConfig != null)
            {
                levelConfig.moveAwayConfig = moveAwayConfig;
            }
            
            // 执行内部行为
            var result = innerBehavior.ExecuteMovement(enemyTransform, playerTransform, enemyData, levelConfig, runtimeState, out targetPosition);
            
            // 恢复原配置
            levelConfig.moveTowardsConfig = originalMoveTowards;
            levelConfig.moveAwayConfig = originalMoveAway;
            
            return result;
        }
    }
    
    /// <summary>
    /// 重置行为树
    /// 清理 Blackboard 中的行为树缓存，准备下一轮循环
    /// </summary>
    private void ResetBehaviorTree(Blackboard blackboard)
    {
        // 移除缓存的行为树
        blackboard.Remove(BEHAVIOR_TREE_KEY);
        blackboard.Set(IS_INITIALIZED_KEY, false);
        
        // 清理 SequenceBehavior 和 RepeatDecorator 的内部状态
        // 这些会在重新初始化时自动清理
    }
}

