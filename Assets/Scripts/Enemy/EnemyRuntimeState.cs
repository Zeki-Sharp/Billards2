using UnityEngine;

/// <summary>
/// 敌人运行时状态数据
/// 将运行时状态从 EnemyBehavior 分离，支持状态保存/恢复
/// </summary>
[System.Serializable]
public class EnemyRuntimeState
{
    #region 阶段与状态
    
    /// <summary>
    /// 当前阶段（Telegraph、Attack、Move）
    /// </summary>
    public string currentPhase = "";
    
    /// <summary>
    /// 当前移动状态
    /// </summary>
    public string currentMovementState = "";
    
    /// <summary>
    /// 当前攻击状态
    /// </summary>
    public string currentAttackState = "";
    
    #endregion
    
    #region 时间相关
    
    /// <summary>
    /// 上次行动时间
    /// </summary>
    public float lastActionTime = 0f;
    
    /// <summary>
    /// 上次攻击时间
    /// </summary>
    public float lastAttackTime = 0f;
    
    /// <summary>
    /// 上次移动时间
    /// </summary>
    public float lastMoveTime = 0f;
    
    #endregion
    
    #region 移动相关
    
    /// <summary>
    /// 是否正在移动
    /// </summary>
    public bool isMoving = false;
    
    /// <summary>
    /// 当前移动方向
    /// </summary>
    public Vector2 currentDirection = Vector2.zero;
    
    /// <summary>
    /// 目标位置
    /// </summary>
    public Vector2 targetPosition = Vector2.zero;
    
    /// <summary>
    /// 间歇移动：当前回合计数
    /// </summary>
    public int intervalCurrentRound = 0;
    
    /// <summary>
    /// 间歇移动：是否处于静止阶段
    /// </summary>
    public bool intervalIsInIdlePhase = false;
    
    /// <summary>
    /// 当前阶段的移动配置（临时存储，由 PhaseAtomicBehaviorWrapper 设置）
    /// </summary>
    [System.NonSerialized]
    public MoveTowardsConfig currentMoveTowardsConfig = null;
    
    /// <summary>
    /// 当前阶段的远离配置（临时存储，由 PhaseAtomicBehaviorWrapper 设置）
    /// </summary>
    [System.NonSerialized]
    public MoveAwayConfig currentMoveAwayConfig = null;
    
    #endregion
    
    #region 生存状态
    
    /// <summary>
    /// 是否已死亡
    /// </summary>
    public bool isDead = false;
    
    /// <summary>
    /// 是否处于陷阱模式
    /// </summary>
    public bool isTrapMode = false;
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 重置运行时状态
    /// </summary>
    public void Reset()
    {
        currentPhase = "";
        currentMovementState = "";
        currentAttackState = "";
        lastActionTime = 0f;
        lastAttackTime = 0f;
        lastMoveTime = 0f;
        isMoving = false;
        currentDirection = Vector2.zero;
        targetPosition = Vector2.zero;
        intervalCurrentRound = 0;
        intervalIsInIdlePhase = false;
        isDead = false;
        isTrapMode = false;
        currentMoveTowardsConfig = null;
        currentMoveAwayConfig = null;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"Phase: {currentPhase}, Moving: {isMoving}, Dead: {isDead}, " +
               $"Direction: {currentDirection}, Trap: {isTrapMode}";
    }
    
    #endregion
}

