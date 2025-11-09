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
    [Header("角色标识")]
    [SerializeField] private string characterID = "";
    
    /// <summary>
    /// 获取角色ID
    /// 
    /// 【设计说明】：
    /// - Player 组件是场景对象层的唯一ID持有者
    /// - 所有从 GameObject 查 characterID 的操作应优先通过此属性
    /// - 由 PlayerSpawner 在生成时设置
    /// </summary>
    public string CharacterID => characterID;
    
    [Header("数据设置")]
    public PlayerData playerData; // 玩家配置数据
    
    [Header("核心组件")]
    [Tooltip("如果为空，将自动从同一GameObject获取")]
    [SerializeField] private PlayerBehavior playerBehavior;
    [SerializeField] private PlayerStateMachine stateMachine;
    // ⚠️ 多角色系统改造：已移除 PlayerInputHandler 和 PlayerMovementController
    // 输入由全局 GlobalInputManager 处理，不再需要WASD移动
    
    [Header("子系统组件")]
    [Tooltip("如果为空，将自动从同一GameObject获取")]
    [SerializeField] private PlayerAttackManager attackManager;
    [SerializeField] private ChargeSystem chargeSystem;
    [SerializeField] private PlayerStats statsManager; // ✅ 使用轻量级 Modifier 系统
    [SerializeField] private PlayerVisualController visualController; // 视觉表现控制器
    
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

        SkillManager.Instance?.NotifyCharacterSpawned(characterID);
    }
    
    /// <summary>
    /// 获取所有组件（如果Inspector中未配置，则自动从同一GameObject获取）
    /// </summary>
    void InitializeComponents()
    {
        // 核心组件 - 优先使用Inspector配置，否则从GameObject获取
        if (playerBehavior == null)
            playerBehavior = GetComponent<PlayerBehavior>();
        if (stateMachine == null)
            stateMachine = GetComponent<PlayerStateMachine>();
        // ⚠️ 多角色系统改造：不再获取 PlayerInputHandler 和 PlayerMovementController
        
        // 子系统组件 - 优先使用Inspector配置，否则从GameObject获取
        if (attackManager == null)
            attackManager = GetComponent<PlayerAttackManager>();
        if (chargeSystem == null)
            chargeSystem = GetComponent<ChargeSystem>();
        if (statsManager == null)
            statsManager = GetComponent<PlayerStats>();
        if (visualController == null)
            visualController = GetComponent<PlayerVisualController>();
        
        // 检查必需组件是否存在
        if (playerBehavior == null)
            Debug.LogError("Player: 缺少 PlayerBehavior 组件！请在Inspector中添加或在GameObject上添加。");
        if (stateMachine == null)
            Debug.LogError("Player: 缺少 PlayerStateMachine 组件！请在Inspector中添加或在GameObject上添加。");
        // ⚠️ 多角色系统改造：不再检查 PlayerInputHandler 和 PlayerMovementController
        if (attackManager == null)
            Debug.LogError("Player: 缺少 PlayerAttackManager 组件！请在Inspector中添加或在GameObject上添加。");
        if (chargeSystem == null)
            Debug.LogError("Player: 缺少 ChargeSystem 组件！请在Inspector中添加或在GameObject上添加。");
        if (statsManager == null)
            Debug.LogError("Player: 缺少 PlayerStats 组件！请在Inspector中添加或在GameObject上添加。");
    }
    
    /// <summary>
    /// 分发 PlayerData 给各个组件
    /// </summary>
    void DistributePlayerData()
    {
        // 分发数据给需要的组件
        if (playerBehavior != null)
            playerBehavior.SetPlayerData(playerData);
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
        if (playerBehavior != null)
        {
            playerBehavior.SetAttackManager(attackManager);
            playerBehavior.SetChargeSystem(chargeSystem);
            playerBehavior.SetStatsManager(statsManager);
        }
        
        // 建立 AttackManager 的组件引用
        if (attackManager != null)
        {
            attackManager.SetPlayerCore(playerBehavior);
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
        if (playerBehavior != null)
            playerBehavior.Initialize();
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
    public PlayerBehavior GetPlayerCore()
    {
        return playerBehavior;
    }
    
    /// <summary>
    /// 获取状态机组件
    /// </summary>
    public PlayerStateMachine GetStateMachine()
    {
        return stateMachine;
    }
    
    // ⚠️ 多角色系统改造：已移除 GetInputHandler() 和 GetMovementController()
    // 输入由全局 GlobalInputManager 处理
    // 暂时不需要移动功能
    
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
    public PlayerStats GetStatsManager()
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
    /// 设置角色ID
    /// 
    /// 【调用时机】：由 PlayerSpawner 在生成角色时调用
    /// 【验证】：确保ID不为空
    /// </summary>
    public void SetCharacterID(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogError("Player: 尝试设置空的角色ID！");
            return;
        }
        
        characterID = id;
        
        if (showDebugInfo)
        {
            Debug.Log($"Player: 设置角色ID = {id}");
        }
    }
    
    /// <summary>
    /// 设置玩家数据（用于角色选择后的数据注入）
    /// </summary>
    /// <param name="newPlayerData">新的玩家数据</param>
    public void SetPlayerData(PlayerData newPlayerData)
    {
        if (newPlayerData == null)
        {
            Debug.LogError("Player: 尝试设置空的PlayerData！");
            return;
        }
        
        playerData = newPlayerData;
        
        if (showDebugInfo)
        {
            Debug.Log($"Player: 设置新的PlayerData - {newPlayerData.info.name} (攻击力: {newPlayerData.attackPower})");
        }
        
        // 重新分发数据给各个组件
        DistributePlayerData();
        
        // 重新初始化所有组件以应用新数据
        InitializeAllComponents();
        
        // 应用视觉表现
        if (visualController != null)
        {
            visualController.ApplyVisuals(newPlayerData);
        }
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
        
        if (playerBehavior != null)
        {
            playerBehavior.ResetForNewTurn();
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
            
            if (playerBehavior != null)
            {
                GUILayout.Label($"Charging: {playerBehavior.ChargingProgress:F1}%");
                GUILayout.Label($"Speed: {playerBehavior.GetSpeed():F2}");
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
