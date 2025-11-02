using UnityEngine;

/// <summary>
/// 跟随移动配置
/// </summary>
[System.Serializable]
public class FollowMovementConfig
{
    [Header("跟随移动配置")]
    public float moveSpeed = 2f;        // 跟随移动速度
    public float moveDistance = 3f;     // 跟随移动距离
    public float minDistance = 1f;      // 保持的最小距离
}

/// <summary>
/// 逃跑移动配置
/// </summary>
[System.Serializable]
public class FleeMovementConfig
{
    [Header("逃跑移动配置")]
    public float moveSpeed = 2f;          // 逃跑移动速度
    public float moveDistance = 4f;       // 逃跑移动距离
    public float triggerDistance = 3f;    // 触发逃跑的距离（玩家接近到这个距离内时逃跑）
    
    [Header("接近玩家设置")]
    [Tooltip("如果离玩家太远，是否向玩家移动")]
    public bool approachWhenFar = false;
    
    [Tooltip("触发接近的距离（超过这个距离时向玩家移动）")]
    public float approachDistance = 8f;
    
    [Tooltip("接近玩家时的移动速度")]
    public float approachSpeed = 3f;
    
    [Tooltip("接近玩家时的移动距离")]
    public float approachMoveDistance = 3f;
}

/// <summary>
/// 移动方式类型枚举（用于间歇移动）
/// </summary>
public enum IntervalMovementMode
{
    Follow,  // 跟随玩家
    Flee     // 逃离玩家
}

/// <summary>
/// 间歇移动配置
/// </summary>
[System.Serializable]
public class IntervalMovementConfig
{
    [Header("间歇移动配置")]
    [Tooltip("移动方式：跟随玩家或逃离玩家")]
    public IntervalMovementMode movementMode = IntervalMovementMode.Follow;
    
    [Header("回合设置")]
    [Tooltip("静止回合数")]
    public int idleRounds = 2;
    
    [Tooltip("移动回合数")]
    public int moveRounds = 3;
    
    [Tooltip("初始状态：true=先静止，false=先移动")]
    public bool startWithIdle = true;
    
    [Header("移动参数")]
    [Tooltip("移动速度")]
    public float moveSpeed = 2f;
    
    [Tooltip("每次移动的距离")]
    public float moveDistance = 3f;
    
    [Tooltip("与玩家保持的最小距离（仅在跟随模式下生效）")]
    public float minDistance = 1f;
    
    [Tooltip("触发逃跑的距离（仅在逃离模式下生效）")]
    public float triggerDistance = 3f;
}

// ===============================================
// 原子行为配置
// ===============================================

/// <summary>
/// 向目标靠近配置（原子行为）
/// </summary>
[System.Serializable]
public class MoveTowardsConfig
{
    [Header("向目标靠近配置")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 3.0f;
    
    [Tooltip("最小距离（到达此距离后停止）")]
    public float minDistance = 1.0f;
    
    [Tooltip("每回合移动距离")]
    public float moveDistance = 2.0f;
}

/// <summary>
/// 远离目标配置（原子行为）
/// </summary>
[System.Serializable]
public class MoveAwayConfig
{
    [Header("远离目标配置")]
    [Tooltip("移动速度（单位/秒）")]
    public float moveSpeed = 3.0f;
    
    [Tooltip("触发距离（只有目标在此距离内才逃离）")]
    public float triggerDistance = 5.0f;
    
    [Tooltip("每回合逃离距离")]
    public float moveDistance = 2.0f;
}

/// <summary>
/// 保持静止配置（原子行为）
/// </summary>
[System.Serializable]
public class IdleConfig
{
    [Header("静止配置")]
    [Tooltip("此配置类为占位符，Idle行为无需额外参数")]
    public bool placeholder = true;
}

// ===============================================
// 行为序列配置（V2 系统）
// ===============================================

/// <summary>
/// 阶段选择模式
/// 定义如何选择和执行阶段
/// </summary>
public enum PhaseSelectionMode
{
    Sequential,   // 顺序执行（IntervalMovement 模式）
    Conditional   // 条件选择（Flee 模式）
}

/// <summary>
/// 阶段行为类型枚举
/// 用于定义每个阶段使用哪种原子行为
/// </summary>
public enum PhaseMovementType
{
    Idle,           // 静止
    MoveTowards,    // 向目标靠近
    MoveAway        // 远离目标
}

/// <summary>
/// 单个阶段配置（统一版本）
/// 定义一个阶段的行为类型、持续回合数和触发条件
/// </summary>
[System.Serializable]
public class MovementPhaseConfig
{
    [Header("阶段配置")]
    [Tooltip("此阶段使用的移动行为")]
    public PhaseMovementType behaviorType = PhaseMovementType.Idle;
    
    [Tooltip("此阶段持续的回合数（Sequential 模式用）")]
    [UnityEngine.Min(1)]
    public int roundCount = 2;
    
    [Header("条件配置（Conditional 模式用）")]
    [Tooltip("触发此阶段的条件（仅 Conditional 模式需要）")]
    public BehaviorConditionConfig condition = null;
    
    [Header("行为参数")]
    [Tooltip("向目标靠近配置（仅当 behaviorType = MoveTowards 时生效）")]
    [Sirenix.OdinInspector.ShowIf("behaviorType", PhaseMovementType.MoveTowards)]
    public MoveTowardsConfig moveTowardsConfig = new MoveTowardsConfig();
    
    [Tooltip("远离目标配置（仅当 behaviorType = MoveAway 时生效）")]
    [Sirenix.OdinInspector.ShowIf("behaviorType", PhaseMovementType.MoveAway)]
    public MoveAwayConfig moveAwayConfig = new MoveAwayConfig();
}

/// <summary>
/// 阶段序列配置（统一系统）
/// 支持顺序执行（Sequential）和条件选择（Conditional）两种模式
/// 统一 IntervalMovement、Flee、FollowPlayer 等所有移动行为
/// </summary>
[System.Serializable]
public class PhaseSequenceConfig
{
    [Header("选择模式")]
    [Tooltip("阶段选择模式：\n• Sequential = 顺序执行（Phase 1 → Phase 2 → ...）\n• Conditional = 并列选择（每回合重新判断所有 Phase 的条件，执行第一个满足的）")]
    public PhaseSelectionMode selectionMode = PhaseSelectionMode.Sequential;
    
    [Header("阶段序列配置")]
    [Tooltip("移动阶段列表：\n• Sequential 模式：按顺序执行\n• Conditional 模式：并列选择（非序列！），每回合找第一个满足条件的执行")]
    public MovementPhaseConfig[] phases = new MovementPhaseConfig[]
    {
        new MovementPhaseConfig { behaviorType = PhaseMovementType.Idle, roundCount = 2 },
        new MovementPhaseConfig { behaviorType = PhaseMovementType.MoveTowards, roundCount = 3 }
    };
    
    [Header("循环设置")]
    [Tooltip("是否循环执行所有阶段")]
    public bool loopPhases = true;
}
