using UnityEngine;
using MoreMountains.Feedbacks;
using System.Collections.Generic;
using DeepSpaceLabs.SAM;

/// <summary>
/// 玩家总控制器 - 事件驱动的组件协调器
/// 
/// 【核心职责】：
/// - 作为Player系统的总协调器和管理器
/// - 自动初始化和配置所有Player子组件
/// - 提供统一的对外接口和组件访问
/// - 响应状态变化事件进行协调
/// 
/// 【管理组件】：
/// - PlayerCore: 核心业务逻辑（物理、蓄力、血量）
/// - PlayerStateMachine: 玩家状态管理（事件驱动）
/// - PlayerInputHandler: 输入处理（事件驱动）
/// - PlayerMovementController: 移动控制（事件驱动）
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 使用协调器模式，不直接处理业务逻辑
/// - 自动组件管理，减少手动配置
/// - 通过事件系统响应状态变化
/// </summary>
public class Player : MonoBehaviour
{
    [Header("数据设置")]
    public PlayerData playerData; // 玩家配置数据
    
    [Header("核心组件")]
    // 以下组件由 Player 自动管理，无需手动配置
    private PlayerCore playerCore;
    private PlayerStateMachine stateMachine;
    private PlayerInputHandler inputHandler;
    private PlayerMovementController movementController;
    
    [Header("子系统组件")]
    // 以下组件由 Player 自动管理，无需手动配置
    private PlayerAttackManager attackManager;
    private ChargeSystem chargeSystem;
    private PlayerStatsManager statsManager;
    
    [Header("特效配置")]
    [Tooltip("玩家特效配置列表，在 Inspector 中直接拖拽 MMF_Player 组件")]
    public List<EffectConfig> effects = new List<EffectConfig>();
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    void Start()
    {
        InitializePlayer();
    }
    
    void OnEnable()
    {
        // 注册所有特效到新的特效管理器
        RegisterEffects();
    }
    
    void OnDisable()
    {
        // 注销所有特效
        UnregisterEffects();
    }
    
    /// <summary>
    /// 初始化玩家
    /// </summary>
    void InitializePlayer()
    {
        // 1. 获取或添加所有组件
        InitializeComponents();
        
        // 2. 分发数据给各个组件
        DistributePlayerData();
        
        // 3. 建立组件间的引用关系
        SetupComponentReferences();
        
        // 4. 初始化各个组件
        InitializeAllComponents();
        
        // 5. 订阅事件
        SubscribeToEvents();
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 初始化完成，所有组件已准备就绪");
        }
    }
    
    /// <summary>
    /// 获取或添加所有组件
    /// </summary>
    void InitializeComponents()
    {
        // 核心组件
        playerCore = GetComponent<PlayerCore>();
        stateMachine = GetComponent<PlayerStateMachine>();
        inputHandler = GetComponent<PlayerInputHandler>();
        movementController = GetComponent<PlayerMovementController>();
        
        // 子系统组件
        attackManager = GetComponent<PlayerAttackManager>();
        chargeSystem = GetComponent<ChargeSystem>();
        statsManager = GetComponent<PlayerStatsManager>();
        
        // 确保所有组件都存在
        if (playerCore == null)
        {
            playerCore = gameObject.AddComponent<PlayerCore>();
            Debug.LogWarning("Player: 自动添加PlayerCore组件");
        }
        
        if (stateMachine == null)
        {
            stateMachine = gameObject.AddComponent<PlayerStateMachine>();
            Debug.LogWarning("Player: 自动添加PlayerStateMachine组件");
        }
        
        if (inputHandler == null)
        {
            inputHandler = gameObject.AddComponent<PlayerInputHandler>();
            Debug.LogWarning("Player: 自动添加PlayerInputHandler组件");
        }
        
        if (movementController == null)
        {
            movementController = gameObject.AddComponent<PlayerMovementController>();
            Debug.LogWarning("Player: 自动添加PlayerMovementController组件");
        }
        
        if (attackManager == null)
        {
            attackManager = gameObject.AddComponent<PlayerAttackManager>();
            Debug.LogWarning("Player: 自动添加PlayerAttackManager组件");
        }
        
        if (chargeSystem == null)
        {
            chargeSystem = gameObject.AddComponent<ChargeSystem>();
            Debug.LogWarning("Player: 自动添加ChargeSystem组件");
        }
        
        if (statsManager == null)
        {
            statsManager = gameObject.AddComponent<PlayerStatsManager>();
            Debug.LogWarning("Player: 自动添加PlayerStatsManager组件");
        }
    }
    
    /// <summary>
    /// 分发 PlayerData 给各个组件
    /// </summary>
    void DistributePlayerData()
    {
        // 分发数据给需要的组件
        if (playerCore != null)
            playerCore.SetPlayerData(playerData);
        if (statsManager != null)
            statsManager.SetPlayerData(playerData);
        if (attackManager != null)
            attackManager.SetPlayerData(playerData);
        
        if (showDebugInfo)
        {
            Debug.Log("Player: PlayerData 已分发给所有组件");
        }
    }
    
    /// <summary>
    /// 建立组件间的引用关系
    /// </summary>
    void SetupComponentReferences()
    {
        // 建立 PlayerCore 的组件引用
        if (playerCore != null)
        {
            playerCore.SetAttackManager(attackManager);
            playerCore.SetChargeSystem(chargeSystem);
            playerCore.SetStatsManager(statsManager);
        }
        
        // 建立 AttackManager 的组件引用
        if (attackManager != null)
        {
            attackManager.SetPlayerCore(playerCore);
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 组件引用关系已建立");
        }
    }
    
    /// <summary>
    /// 初始化所有组件
    /// </summary>
    void InitializeAllComponents()
    {
        // 按正确顺序初始化组件
        if (playerCore != null)
            playerCore.Initialize();
        if (statsManager != null)
            statsManager.Initialize();
        if (attackManager != null)
            attackManager.Initialize();
        // ChargeSystem 不需要特殊的 Initialize 方法
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 所有组件初始化完成");
        }
    }
    
    /// <summary>
    /// 订阅事件
    /// </summary>
    void SubscribeToEvents()
    {
        // 订阅状态变化事件
        if (stateMachine != null)
        {
            stateMachine.OnStateChanged += OnPlayerStateChanged;
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 事件订阅完成");
        }
    }
    
    /// <summary>
    /// 玩家状态变化事件处理
    /// </summary>
    void OnPlayerStateChanged(PlayerStateMachine.PlayerState newState, PlayerStateMachine.PlayerState oldState)
    {
        if (showDebugInfo)
        {
            Debug.Log($"Player: 状态变化 {oldState} -> {newState}");
        }
        
        // 根据状态变化执行相应逻辑
        switch (newState)
        {
            case PlayerStateMachine.PlayerState.Idle:
                OnEnterIdleState();
                break;
            case PlayerStateMachine.PlayerState.Charging:
                OnEnterChargingState();
                break;
            case PlayerStateMachine.PlayerState.Moving:
                OnEnterMovingState();
                break;
        }
    }
    
    /// <summary>
    /// 进入空闲状态
    /// </summary>
    void OnEnterIdleState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入空闲状态 - 可以移动和蓄力");
        }
    }
    
    /// <summary>
    /// 进入蓄力状态
    /// </summary>
    void OnEnterChargingState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入蓄力状态 - 显示瞄准线，停止移动");
        }
        
    }
    
    /// <summary>
    /// 进入运动状态
    /// </summary>
    void OnEnterMovingState()
    {
        if (showDebugInfo)
        {
            Debug.Log("Player: 进入运动状态 - 球在物理移动中");
        }
    }
    
    #region 公共接口
    
    /// <summary>
    /// 获取玩家核心组件
    /// </summary>
    public PlayerCore GetPlayerCore()
    {
        return playerCore;
    }
    
    /// <summary>
    /// 获取状态机组件
    /// </summary>
    public PlayerStateMachine GetStateMachine()
    {
        return stateMachine;
    }
    
    /// <summary>
    /// 获取输入处理器组件
    /// </summary>
    public PlayerInputHandler GetInputHandler()
    {
        return inputHandler;
    }
    
    /// <summary>
    /// 获取移动控制器组件
    /// </summary>
    public PlayerMovementController GetMovementController()
    {
        return movementController;
    }
    
    /// <summary>
    /// 获取攻击管理器组件
    /// </summary>
    public PlayerAttackManager GetAttackManager()
    {
        return attackManager;
    }
    
    /// <summary>
    /// 获取蓄力系统组件
    /// </summary>
    public ChargeSystem GetChargeSystem()
    {
        return chargeSystem;
    }
    
    /// <summary>
    /// 获取数值管理器组件
    /// </summary>
    public PlayerStatsManager GetStatsManager()
    {
        return statsManager;
    }
    
    /// <summary>
    /// 获取玩家数据
    /// </summary>
    public PlayerData GetPlayerData()
    {
        return playerData;
    }
    
    /// <summary>
    /// 重置玩家状态
    /// </summary>
    public void ResetPlayer()
    {
        if (stateMachine != null)
        {
            // 重置状态机到空闲状态
            stateMachine.SwitchToState(PlayerStateMachine.PlayerState.Idle);
        }
        
        if (playerCore != null)
        {
            playerCore.ResetForNewTurn();
        }
        
        if (showDebugInfo)
        {
            Debug.Log("Player: 玩家状态已重置");
        }
    }
    
    #endregion
    
    #region 调试信息
    
    void OnGUI()
    {
        if (showDebugInfo && stateMachine != null)
        {
            // 显示当前状态
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Player State: {stateMachine.CurrentState}");
            
            if (playerCore != null)
            {
                GUILayout.Label($"Charging: {playerCore.ChargingProgress:F1}%");
                GUILayout.Label($"Speed: {playerCore.GetSpeed():F2}");
            }
            
            GUILayout.EndArea();
        }
    }
    
    #endregion
    
    #region 特效管理
    
    /// <summary>
    /// 注册所有特效到特效管理器
    /// </summary>
    void RegisterEffects()
    {
        // 检查 EffectManager 是否已初始化
        if (EffectManager.Instance == null)
        {
            Debug.LogWarning($"Player {name}: EffectManager 尚未初始化，延迟注册特效");
            // 使用协程延迟注册
            StartCoroutine(DelayedRegisterEffects());
            return;
        }
        
        foreach (var effect in effects)
        {
            if (effect.IsValid())
            {
                EffectManager.Instance.RegisterEffect(gameObject, effect.effectType, effect.mmfPlayer);
            }
            else
            {
                Debug.LogWarning($"Player: 无效的特效配置: {effect.GetDebugInfo()}");
            }
        }
    }
    
    /// <summary>
    /// 延迟注册特效的协程
    /// </summary>
    System.Collections.IEnumerator DelayedRegisterEffects()
    {
        // 等待 EffectManager 初始化完成，如果不存在则创建
        while (EffectManager.Instance == null)
        {
            // 如果场景中没有 EffectManager，创建一个
            if (FindAnyObjectByType<EffectManager>() == null)
            {
                GameObject effectManagerGO = new GameObject("EffectManager");
                effectManagerGO.AddComponent<EffectManager>();
                if (showDebugInfo)
                {
                    Debug.Log($"Player {name}: 自动创建 EffectManager");
                }
            }
            yield return null;
        }
        
        // 重新注册特效
        foreach (var effect in effects)
        {
            if (effect.IsValid())
            {
                EffectManager.Instance.RegisterEffect(gameObject, effect.effectType, effect.mmfPlayer);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"Player {name}: 延迟注册特效完成");
        }
    }
    
    /// <summary>
    /// 注销所有特效
    /// </summary>
    void UnregisterEffects()
    {
        if (EffectManager.Instance != null)
        {
            EffectManager.Instance.UnregisterEffect(gameObject);
        }
    }
    
    #endregion
    
    
}
