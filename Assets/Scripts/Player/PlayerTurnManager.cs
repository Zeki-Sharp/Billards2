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
    private int launchedCount;               // 已发射次数（球发射瞬间计数）
    private List<string> launchedCharacterIDs = new List<string>(); // 已发射的角色ID列表
    
    // ✅ 回合结束监控
    private bool isWaitingForAllBallsToStop = false;  // 是否在等待所有玩家球停止
    
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
        // ✅ 订阅事件：发射时计数，球停止时检查回合结束
        GameEventBus.OnCharacterLaunched += OnCharacterLaunched;   // 球发射时
        GameEventBus.OnBallStopped += OnAnyBallStopped;             // 任意球停止时
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnCharacterLaunched -= OnCharacterLaunched;
        GameEventBus.OnBallStopped -= OnAnyBallStopped;
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
    /// 开始新回合（重置发射次数和监控状态）
    /// </summary>
    public void StartTurn()
    {
        // 重置发射次数
        remainingLaunches = launchesPerTurn;
        launchedCount = 0;
        launchedCharacterIDs.Clear();
        isWaitingForAllBallsToStop = false;
        
        // 发布回合重置事件
        OnTurnReset?.Invoke();
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerTurnManager] ✅ 回合开始，需要发射 {launchesPerTurn} 个球");
        }
    }
    
    /// <summary>
    /// ✅ 处理角色发射事件（球发射瞬间计数）
    /// </summary>
    void OnCharacterLaunched(string characterID, Vector2 direction, float force)
    {
        // 检查是否已经记录过（防止重复计数）
        if (launchedCharacterIDs.Contains(characterID))
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"[PlayerTurnManager] 角色 {characterID} 已经发射过，忽略重复");
            }
            return;
        }
        
        // 记录发射
        launchedCharacterIDs.Add(characterID);
        launchedCount++;
        remainingLaunches--;
        
        if (showDebugInfo)
        {
            Debug.Log($"[PlayerTurnManager] ✅ 角色 {characterID} 发射！已发射 {launchedCount}/{launchesPerTurn}，剩余 {remainingLaunches} 次");
        }
        
        // ✅ 发射次数用尽，开始监控所有玩家球是否停止
        if (remainingLaunches <= 0)
        {
            isWaitingForAllBallsToStop = true;
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerTurnManager] 发射次数用尽，等待所有玩家球停止");
            }
        }
    }
    
    /// <summary>
    /// ✅ 处理任意球停止事件（检查所有玩家球是否都停止）
    /// </summary>
    void OnAnyBallStopped(BallPhysics ball)
    {
        // 只在监控期间处理
        if (!isWaitingForAllBallsToStop)
            return;
        
        // 检查是否是玩家球
        if (!IsPlayerBall(ball))
            return;
        
        // 检查所有玩家球是否都停止
        if (AreAllPlayerBallsStopped())
        {
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerTurnManager] ✅✅ 所有玩家球停止运动，回合结束！");
            }
            
            // 停止监控
            isWaitingForAllBallsToStop = false;
            
            // 触发回合完成事件
            OnTurnComplete?.Invoke();
        }
    }
    
    /// <summary>
    /// 检查是否是玩家球
    /// </summary>
    bool IsPlayerBall(BallPhysics ball)
    {
        if (ball == null || ball.gameObject == null)
            return false;
        
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
            return false;
        
        // 检查是否是队伍中任意一个角色的球
        foreach (var character in teamData.characters)
        {
            if (character != null && character.ballInstance == ball.gameObject)
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查所有玩家球是否都停止
    /// </summary>
    bool AreAllPlayerBallsStopped()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.LogError("[PlayerTurnManager] TeamData 为 null，无法检查球停止状态");
            return false;
        }
        
        // 检查所有角色的球
        foreach (var character in teamData.characters)
        {
            if (character == null || character.ballInstance == null)
                continue;
            
            // 获取球的物理组件
            BallPhysics physics = character.ballInstance.GetComponent<BallPhysics>();
            if (physics == null)
            {
                Debug.LogWarning($"[PlayerTurnManager] 角色 {character.characterID} 的球缺少 BallPhysics 组件");
                continue;
            }
            
            // 检查是否在运动
            if (physics.IsMoving())
            {
                return false;  // 还有球在动
            }
        }
        
        // 所有球都停止了
        return true;
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
        info += $"剩余发射次数: {remainingLaunches}\n";
        info += $"已发射角色: {string.Join(", ", launchedCharacterIDs)}\n";
        info += $"等待所有球停止: {(isWaitingForAllBallsToStop ? "是" : "否")}\n";
        info += $"回合完成: {(IsTurnComplete ? "是" : "否")}";
        return info;
    }
    
    #endregion
}

