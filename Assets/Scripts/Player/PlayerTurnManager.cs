using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家回合管理器 - 管理玩家回合内的发射次数和角色可用性
/// 
/// 【核心职责】：
/// - 管理本回合发射次数（remainingLaunches, launchedCount）
/// - 管理已发射角色列表（launchedCharacterIDs）
/// - 判断角色是否已完成发射（IsCharacterCompleted）
/// - 判断角色是否可被选中（IsCharacterAvailable）
/// - 判断回合是否结束（IsTurnComplete）
/// - 发布回合事件（OnTurnComplete, OnTurnReset）
/// 
/// 【设计原则】：
/// - 对称 EnemyManager 的架构
/// - 不管理选择状态（CharacterSelectionController）
/// - 不管理球体物理状态（PlayerStateMachine）
/// - 不管理阶段流程（PlayerPhaseController）
/// - 通过事件驱动，完全解耦
/// 
/// 【执行顺序】：CONTROLLER (0)
/// 【依赖】：GameEventBus, TeamData
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class PlayerTurnManager : MonoBehaviour
{
    [Header("回合设置")]
    [SerializeField] 
    [Tooltip("每回合需要发射的球数")]
    private int launchesPerTurn = 2;
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 回合状态
    private int remainingLaunches;           // 剩余发射次数
    private int launchedCount;               // 已发射次数
    private List<string> launchedCharacterIDs = new List<string>(); // 已发射的角色ID列表
    
    // 场景单例
    private static PlayerTurnManager instance;
    public static PlayerTurnManager Instance => instance;
    
    // 事件
    public static System.Action OnTurnComplete;  // 回合完成事件
    public static System.Action OnTurnReset;     // 回合重置事件
    
    // 公共属性
    public int RemainingLaunches => remainingLaunches;
    public int LaunchedCount => launchedCount;
    public bool IsTurnComplete => remainingLaunches <= 0;
    
    void Awake()
    {
        // 单例检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning("PlayerTurnManager: 场景中存在多个实例，销毁多余实例");
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerTurnManager: 初始化完成");
        }
    }
    
    void OnEnable()
    {
        // ✅ 订阅角色完成事件（而不是发射事件）
        GameEventBus.OnCharacterCompleted += OnCharacterCompleted;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnCharacterCompleted -= OnCharacterCompleted;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    #region 回合管理
    
    /// <summary>
    /// 开始新回合（重置发射次数）
    /// </summary>
    public void StartTurn()
    {
        // 重置发射次数
        remainingLaunches = launchesPerTurn;
        launchedCount = 0;
        launchedCharacterIDs.Clear();
        
        // 发布回合重置事件
        OnTurnReset?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerTurnManager: ✅ 回合开始，需要发射 {launchesPerTurn} 个球");
        }
    }
    
    /// <summary>
    /// 处理角色完成事件（MovingEnd → Completed）
    /// </summary>
    void OnCharacterCompleted(string characterID)
    {
        // 检查是否已经记录过（防止重复计数）
        if (launchedCharacterIDs.Contains(characterID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"PlayerTurnManager: 角色 {characterID} 已经完成过，忽略重复");
            }
            return;
        }
        
        // 记录完成
        launchedCharacterIDs.Add(characterID);
        launchedCount++;
        remainingLaunches--;
        
        if (showDebugInfo)
        {
            Debug.Log($"PlayerTurnManager: ✅ 角色 {characterID} 完成发射！进度 ({launchedCount}/{launchesPerTurn})，剩余 {remainingLaunches} 次");
        }
        
        // 检查回合是否完成
        if (remainingLaunches <= 0)
        {
            if (showDebugInfo)
            {
                Debug.Log($"PlayerTurnManager: ✅✅ 所有发射完成，回合结束！");
            }
            
            // 触发回合完成事件
            OnTurnComplete?.Invoke();
        }
    }
    
    #endregion
    
    #region 查询接口
    
    /// <summary>
    /// 检查角色是否已完成发射
    /// </summary>
    public bool IsCharacterCompleted(string characterID)
    {
        return launchedCharacterIDs.Contains(characterID);
    }
    
    /// <summary>
    /// 检查角色是否可被选中（未完成发射）
    /// </summary>
    public bool IsCharacterAvailable(string characterID)
    {
        return !IsCharacterCompleted(characterID);
    }
    
    /// <summary>
    /// 获取已发射的角色ID列表
    /// </summary>
    public List<string> GetLaunchedCharacterIDs()
    {
        return new List<string>(launchedCharacterIDs);
    }
    
    #endregion
    
    #region 调试信息
    
    /// <summary>
    /// 获取回合状态信息（用于调试）
    /// </summary>
    public string GetTurnInfo()
    {
        string info = $"=== PlayerTurnManager 回合状态 ===\n";
        info += $"已发射: {launchedCount}/{launchesPerTurn}\n";
        info += $"剩余: {remainingLaunches}\n";
        info += $"已发射角色: {string.Join(", ", launchedCharacterIDs)}\n";
        info += $"回合完成: {(IsTurnComplete ? "是" : "否")}";
        return info;
    }
    
    #endregion
}

