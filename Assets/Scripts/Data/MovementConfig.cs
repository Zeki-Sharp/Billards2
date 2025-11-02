using UnityEngine;

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
