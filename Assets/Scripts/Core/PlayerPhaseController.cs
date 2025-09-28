using UnityEngine;
using MoreMountains.Tools;

/// <summary>
/// 玩家阶段控制器 - 管理玩家阶段的所有子阶段
/// 
/// 【核心职责】：
/// - 管理玩家子阶段：Normal, Charging, Moving, Transition
/// - 协调PlayerStateMachine和相关系统
/// - 处理玩家阶段内的所有逻辑
/// - 与EnemyPhaseController对称设计
/// </summary>
public class PlayerPhaseController : MonoBehaviour
{
    public static PlayerPhaseController Instance { get; private set; }
    
    /// <summary>
    /// 玩家子阶段枚举
    /// </summary>
    public enum PlayerSubPhase
    {
        Normal,         // 正常状态：玩家移动躲避
        Charging,       // 蓄力状态：完全时停，玩家瞄准蓄力
        Moving,         // 移动状态：玩家球在物理移动中，敌人和子弹时停
        Transition      // 过渡状态：玩家可移动，敌人和子弹仍时停，白球运动
    }
    
    
    // 当前子阶段
    private PlayerSubPhase currentSubPhase = PlayerSubPhase.Normal;
    
    // 子阶段顺序（类似EnemyPhaseController的设计）
    private readonly PlayerSubPhase[] subPhaseSequence = {
        PlayerSubPhase.Normal,     // 正常状态
        PlayerSubPhase.Charging,   // 蓄力状态
        PlayerSubPhase.Moving,     // 移动状态
        PlayerSubPhase.Transition  // 过渡状态
    };
    
    private int currentSubPhaseIndex = 0;
    
    // 组件引用
    private PlayerStateMachine playerStateMachine;
    private TransitionManager transitionManager;
    private TimeStopEffect timeStopEffect;
    
    // 事件
    public System.Action<PlayerSubPhase> OnSubPhaseStart;
    public System.Action<PlayerSubPhase> OnSubPhaseComplete;
    public System.Action OnPlayerPhaseComplete; // 整个玩家阶段完成事件
    
    // 公共属性
    public PlayerSubPhase CurrentSubPhase => currentSubPhase;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        // 延迟初始化，确保PlayerStateMachine已经被创建
        StartCoroutine(DelayedInitialization());
    }
    
    System.Collections.IEnumerator DelayedInitialization()
    {
        // 等待一帧，确保所有组件都已创建
        yield return null;
        
        InitializeController();
    }
    
    void InitializeController()
    {
        // 获取组件引用
        playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        transitionManager = FindFirstObjectByType<TransitionManager>();
        timeStopEffect = FindFirstObjectByType<TimeStopEffect>();
        
        // 订阅PlayerStateMachine的状态变化事件
        if (playerStateMachine != null)
        {
            playerStateMachine.OnStateChanged += OnPlayerStateChanged;
        }
        else
        {
            Debug.LogError("PlayerPhaseController: 未找到PlayerStateMachine！");
        }
    }
    
    void OnDestroy()
    {
        if (playerStateMachine != null)
        {
            playerStateMachine.OnStateChanged -= OnPlayerStateChanged;
        }
    }
    
    /// <summary>
    /// 开始玩家阶段（由GameFlowController调用）
    /// </summary>
    public void StartPlayerPhase()
    {
        // 玩家阶段开始
        
        // 确保PlayerStateMachine引用正确
        if (playerStateMachine == null)
        {
            playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
            if (playerStateMachine != null)
            {
                playerStateMachine.OnStateChanged += OnPlayerStateChanged;
            }
            else
            {
                Debug.LogError("PlayerPhaseController: 在StartPlayerPhase时仍未找到PlayerStateMachine！");
            }
        }
        
        // 重置子阶段索引
        currentSubPhaseIndex = 0;
        
        // 开始执行第一个子阶段
        ExecuteNextSubPhase();
    }
    
    /// <summary>
    /// 执行下一个子阶段
    /// </summary>
    void ExecuteNextSubPhase()
    {
        if (currentSubPhaseIndex >= subPhaseSequence.Length)
        {
            // 所有子阶段完成，通知GameFlowController
            OnPlayerPhaseComplete?.Invoke();
            return;
        }
        
        // 获取当前子阶段
        PlayerSubPhase subPhase = subPhaseSequence[currentSubPhaseIndex];
        currentSubPhase = subPhase;
        
        // 开始执行子阶段
        
        // 通知子阶段开始
        OnSubPhaseStart?.Invoke(subPhase);
        
        // 执行具体子阶段逻辑
        ExecuteSubPhase(subPhase);
    }
    
    /// <summary>
    /// 执行具体子阶段逻辑
    /// </summary>
    void ExecuteSubPhase(PlayerSubPhase subPhase)
    {
        switch (subPhase)
        {
            case PlayerSubPhase.Normal:
                ExecuteNormalPhase();
                break;
            case PlayerSubPhase.Charging:
                ExecuteChargingPhase();
                break;
            case PlayerSubPhase.Moving:
                ExecuteMovingPhase();
                break;
            case PlayerSubPhase.Transition:
                ExecuteTransitionPhase();
                break;
        }
    }
    
    /// <summary>
    /// 处理PlayerStateMachine的状态变化
    /// </summary>
    void OnPlayerStateChanged(PlayerStateMachine.PlayerState newState, PlayerStateMachine.PlayerState oldState)
    {
        // 根据PlayerStateMachine的状态变化，检查是否可以进入下一个子阶段
        if (CanAdvanceToNextSubPhase(newState, oldState))
        {
            currentSubPhaseIndex++;
            ExecuteNextSubPhase();
        }
    }
    
    /// <summary>
    /// 检查是否可以进入下一个子阶段
    /// </summary>
    bool CanAdvanceToNextSubPhase(PlayerStateMachine.PlayerState newState, PlayerStateMachine.PlayerState oldState)
    {
        // 根据当前子阶段和PlayerStateMachine状态变化判断
        switch (currentSubPhase)
        {
            case PlayerSubPhase.Normal:
                // Normal -> Charging: PlayerState Idle -> Charging
                return newState == PlayerStateMachine.PlayerState.Charging && 
                       oldState == PlayerStateMachine.PlayerState.Idle;
                       
            case PlayerSubPhase.Charging:
                // Charging -> Moving: PlayerState Charging -> Moving
                return newState == PlayerStateMachine.PlayerState.Moving && 
                       oldState == PlayerStateMachine.PlayerState.Charging;
                       
            case PlayerSubPhase.Moving:
                // Moving -> Transition: PlayerState Moving -> Idle
                return newState == PlayerStateMachine.PlayerState.Idle && 
                       oldState == PlayerStateMachine.PlayerState.Moving;
                       
            case PlayerSubPhase.Transition:
                // Transition完成由TransitionManager通知
                return false;
        }
        
        return false;
    }
    
    /// <summary>
    /// 执行Normal子阶段
    /// </summary>
    void ExecuteNormalPhase()
    {
        // 关闭时停效果
        if (timeStopEffect != null)
        {
            timeStopEffect.SetIntensityImmediate(0f);
        }
        
        // 触发Normal特效
        
        // Normal子阶段开始，等待PlayerStateMachine状态变化
        

    }
    
    /// <summary>
    /// 执行Charging子阶段
    /// </summary>
    void ExecuteChargingPhase()
    {
        // 时停效果由ChargeSystem控制
        // 触发蓄力特效
        //GameEventBus.PublishEffectEvent("Charge", Vector3.zero);
        
        // Charging子阶段开始，等待PlayerStateMachine状态变化
        
        // Charging阶段不立即完成，等待PlayerStateMachine触发Moving
        // 子阶段切换由OnPlayerStateChanged处理
    }
    
    /// <summary>
    /// 执行Moving子阶段
    /// </summary>
    void ExecuteMovingPhase()
    {
        // 保持时停效果
        if (timeStopEffect != null)
        {
            timeStopEffect.SetIntensityImmediate(1f);
        }
        
        
        // Moving子阶段开始，等待PlayerStateMachine状态变化
        
    }
    
    /// <summary>
    /// 执行Transition子阶段
    /// </summary>
    void ExecuteTransitionPhase()
    {
        // 开始过渡
        if (transitionManager != null)
        {
            transitionManager.StartTransition();
            
            // 监听过渡完成
            transitionManager.OnTransitionEnd += OnTransitionComplete;
        }
        
        // 触发时停出场特效
    }
    
    /// <summary>
    /// 过渡完成回调
    /// </summary>
    void OnTransitionComplete()
    {
        if (transitionManager != null)
        {
            transitionManager.OnTransitionEnd -= OnTransitionComplete;
        }
        
        // Transition子阶段完成
        OnSubPhaseComplete?.Invoke(PlayerSubPhase.Transition);
        
        // 进入下一个子阶段（实际上是完成整个玩家阶段）
        currentSubPhaseIndex++;
        ExecuteNextSubPhase();
    }
    
    /// <summary>
    /// 获取子阶段描述
    /// </summary>
    string GetSubPhaseDescription(PlayerSubPhase subPhase)
    {
        switch (subPhase)
        {
            case PlayerSubPhase.Normal:
                return "正常状态：玩家移动躲避";
            case PlayerSubPhase.Charging:
                return "蓄力状态：完全时停，玩家瞄准蓄力";
            case PlayerSubPhase.Moving:
                return "移动状态：玩家球在物理移动中，敌人和子弹时停";
            case PlayerSubPhase.Transition:
                return "过渡状态：玩家可移动，敌人和子弹仍时停，白球运动";
            default:
                return "未知子阶段";
        }
    }
}
