using UnityEngine;

/// <summary>
/// 时间管理器 - 管理游戏中不同对象的时间缩放
/// 
/// 【核心职责】：
/// - 管理全局时间缩放设置
/// - 根据游戏状态提供不同对象的时间缩放
/// - 监听游戏流程状态变化
/// 
/// 【设计原则】：
/// - 只影响移动和动画，不影响物理系统
/// - 状态驱动，自动根据游戏流程调整
/// - 可配置，时间缩放值可在Inspector中调整
/// </summary>
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }
    
    [Header("时间缩放设置")]
    [Tooltip("敌人时间缩放（Charging和Transition阶段）")]
    [Range(0.01f, 1f)]
    public float enemyTimeScale = 0.1f;
    
    [Tooltip("玩家时间缩放（通常为1.0）")]
    [Range(0.01f, 1f)]
    public float playerTimeScale = 1f;
    
    [Header("时停阶段控制")]
    [Tooltip("在Charging阶段是否启用敌人时停")]
    public bool enableEnemyTimeStopInCharging = true;
    
    [Tooltip("在Transition阶段是否启用敌人时停")]
    public bool enableEnemyTimeStopInTransition = true;
    
    [Tooltip("在Normal阶段是否启用敌人时停")]
    public bool enableEnemyTimeStopInNormal = false;
    
    [Header("时停对象控制")]
    [Tooltip("是否影响敌人移动")]
    public bool affectEnemyMovement = true;
    
    [Tooltip("是否影响敌人动画")]
    public bool affectEnemyAnimation = true;
    
    [Tooltip("是否影响敌人攻击间隔")]
    public bool affectEnemyAttackInterval = true;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    private GameFlowController gameFlowController;
    
    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("发现多个TimeManager实例，销毁重复的实例");
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 获取GameFlowController引用
        gameFlowController = GameFlowController.Instance;
        if (gameFlowController == null)
        {
            Debug.LogError("TimeManager: 未找到GameFlowController实例！");
        }
    }
    
    #region 公共方法
    
    /// <summary>
    /// 获取敌人时间缩放
    /// </summary>
    public float GetEnemyTimeScale()
    {
        if (gameFlowController == null) return 1f;
        
        // 敌人阶段：敌人使用正常时间（无时停）
        if (gameFlowController.IsEnemyPhase)
        {
            return 1f;
        }
        
        bool shouldSlowDown = false;
        
        // 根据当前状态和设置决定是否时停
        if (gameFlowController.IsChargingState && enableEnemyTimeStopInCharging)
        {
            shouldSlowDown = true;
        }
        else if (gameFlowController.IsTransitionState && enableEnemyTimeStopInTransition)
        {
            shouldSlowDown = true;
        }
        else if (gameFlowController.IsNormalState && enableEnemyTimeStopInNormal)
        {
            shouldSlowDown = true;
        }
        
        float timeScale = shouldSlowDown ? enemyTimeScale : 1f;
        
        
        return timeScale;
    }
    
    /// <summary>
    /// 获取玩家时间缩放
    /// </summary>
    public float GetPlayerTimeScale()
    {
        return playerTimeScale;
    }
    
    /// <summary>
    /// 获取缩放后的DeltaTime（用于敌人移动和动画）
    /// </summary>
    public float GetEnemyDeltaTime()
    {
        return Time.deltaTime * GetEnemyTimeScale();
    }
    
    /// <summary>
    /// 获取缩放后的DeltaTime（用于玩家移动和动画）
    /// </summary>
    public float GetPlayerDeltaTime()
    {
        return Time.deltaTime * GetPlayerTimeScale();
    }
    
    /// <summary>
    /// 获取缩放后的累计时间
    /// </summary>
    public float GetScaledTime()
    {
        // 简化实现：直接返回Time.time * 时间缩放
        return Time.time * GetEnemyTimeScale();
    }
    
    /// <summary>
    /// 检查敌人是否应该被时停
    /// </summary>
    public bool IsEnemyTimeStopped()
    {
        if (gameFlowController == null) return false;
        
        bool chargingStop = gameFlowController.IsChargingState && enableEnemyTimeStopInCharging;
        bool transitionStop = gameFlowController.IsTransitionState && enableEnemyTimeStopInTransition;
        bool normalStop = gameFlowController.IsNormalState && enableEnemyTimeStopInNormal;
        
        bool result = chargingStop || transitionStop || normalStop;
        
        return result;
    }
    
    /// <summary>
    /// 检查敌人移动是否应该被时停影响
    /// </summary>
    public bool ShouldAffectEnemyMovement()
    {
        bool isTimeStopped = IsEnemyTimeStopped();
        bool result = affectEnemyMovement && isTimeStopped;
        
        return result;
    }
    
    /// <summary>
    /// 检查敌人动画是否应该被时停影响
    /// </summary>
    public bool ShouldAffectEnemyAnimation()
    {
        return affectEnemyAnimation && IsEnemyTimeStopped();
    }
    
    /// <summary>
    /// 检查敌人攻击间隔是否应该被时停影响
    /// </summary>
    public bool ShouldAffectEnemyAttackInterval()
    {
        return affectEnemyAttackInterval && IsEnemyTimeStopped();
    }
    
    #endregion
    
    #region 调试方法
    
    /// <summary>
    /// 强制设置敌人时间缩放（调试用）
    /// </summary>
    [ContextMenu("测试敌人时停")]
    public void TestEnemyTimeStop()
    {
        enemyTimeScale = 0.1f;
        Debug.Log($"TimeManager: 测试敌人时停，时间缩放 = {enemyTimeScale}");
    }
    
    /// <summary>
    /// 恢复敌人正常时间（调试用）
    /// </summary>
    [ContextMenu("恢复敌人正常时间")]
    public void RestoreEnemyNormalTime()
    {
        enemyTimeScale = 1f;
        Debug.Log($"TimeManager: 恢复敌人正常时间，时间缩放 = {enemyTimeScale}");
    }
    
    #endregion
}
