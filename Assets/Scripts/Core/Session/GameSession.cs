using UnityEngine;

/// <summary>
/// 游戏会话管理器
/// 
/// 【核心职责】：
/// 1. 管理游戏会话生命周期（DontDestroyOnLoad）
/// 2. 保存和恢复玩家运行时数据（跨场景持久化）
/// 3. 管理游戏统计数据
/// 4. 管理会话状态
/// 
/// 【生命周期】：
/// - 游戏启动 → OnManagerCreated()
/// - 一局游戏内 → 数据自动持久化
/// - 游戏结束 → Clear()
/// - 游戏重置 → Reset()
/// 
/// 【替代方案】：
/// - 替代混乱的 GameRuntimeData 静态类
/// - 提供清晰的职责分离和生命周期管理
/// </summary>
public class GameSession : SingletonManager<GameSession>
{
    
    #region 核心数据
    
    /// <summary>
    /// 玩家运行时数据（跨场景持久化）
    /// </summary>
    public PlayerRuntimeData PlayerData { get; private set; }
    
    /// <summary>
    /// 游戏统计数据
    /// </summary>
    public GameStatistics Statistics { get; private set; }
    
    /// <summary>
    /// 会话状态数据
    /// </summary>
    public SessionState State { get; private set; }
    
    #endregion
    
    #region 配置
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = true;
    
    /// <summary>
    /// 重写：启用调试日志
    /// </summary>
    protected override bool EnableDebugLog => showDebugLog;
    
    #endregion
    
    #region SingletonManager 生命周期
    
    /// <summary>
    /// Manager 创建时调用（替代 Awake 初始化）
    /// </summary>
    protected override void OnManagerCreated()
    {
        Initialize();
    }
    
    /// <summary>
    /// Manager 销毁时调用（替代 OnDestroy 清理）
    /// </summary>
    protected override void OnManagerDestroyed()
    {
        // 会话销毁时的清理逻辑
        if (EnableDebugLog)
        {
            Debug.Log("[GameSession] 🧹 会话管理器销毁");
        }
    }
    
    #endregion
    
    #region Unity 生命周期
    
    void Update()
    {
        // 更新游戏时长
        if (Statistics != null)
        {
            Statistics.UpdateGameTime(Time.deltaTime);
        }
    }
    
    #endregion
    
    #region 生命周期管理
    
    /// <summary>
    /// 初始化会话
    /// </summary>
    private void Initialize()
    {
        // 创建数据容器
        PlayerData = new PlayerRuntimeData();
        Statistics = new GameStatistics();
        State = new SessionState();
        
        if (EnableDebugLog)
        {
            Debug.Log("[GameSession] ✅ 会话初始化完成");
        }
    }
    
    /// <summary>
    /// 重置会话（游戏重新开始）
    /// </summary>
    public void Reset()
    {
        PlayerData.Clear();
        Statistics.Clear();
        State.Clear();
        
        if (EnableDebugLog)
        {
            Debug.Log("[GameSession] 🔄 会话已重置");
        }
    }
    
    /// <summary>
    /// 清除会话数据（游戏结束）
    /// </summary>
    public void Clear()
    {
        Reset();
        
        if (EnableDebugLog)
        {
            Debug.Log("[GameSession] 🧹 会话已清除");
        }
    }
    
    #endregion
    
    #region 便捷访问方法
    
    /// <summary>
    /// 检查是否有保存的玩家数据
    /// </summary>
    public bool HasPlayerData()
    {
        return PlayerData != null && PlayerData.HasData();
    }
    
    /// <summary>
    /// 检查是否来自地图系统
    /// </summary>
    public bool IsFromMapSystem()
    {
        return State != null && State.fromMapSystem;
    }
    
    /// <summary>
    /// 获取当前地图层级
    /// </summary>
    public int GetCurrentMapLayer()
    {
        return State != null ? State.currentMapLayer : -1;
    }
    
    /// <summary>
    /// 获取总击杀数
    /// </summary>
    public int GetTotalKills()
    {
        return Statistics != null ? Statistics.totalEnemyKills : 0;
    }
    
    #endregion
    
    #region 调试信息
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        return $"[GameSession 调试信息]\n" +
               $"=== 玩家数据 ===\n" +
               $"  - 有数据: {HasPlayerData()}\n" +
               $"  - 属性数: {PlayerData?.attributeCurrentValues.Count ?? 0}\n" +
               $"  - 修改器: {PlayerData?.activeModifiers.Count ?? 0}\n" +
               $"  - 状态效果: {PlayerData?.activeStatusEffects.Count ?? 0}\n" +
               $"=== 游戏统计 ===\n" +
               $"  - 总击杀: {Statistics?.totalEnemyKills ?? 0}\n" +
               $"  - 通关数: {Statistics?.levelsCompleted ?? 0}\n" +
               $"  - 游戏时长: {Statistics?.gameTime ?? 0f:F1}秒\n" +
               $"=== 会话状态 ===\n" +
               $"  - 来自地图: {State?.fromMapSystem ?? false}\n" +
               $"  - 地图层级: {State?.currentMapLayer ?? -1}\n" +
               $"  - 当前关卡: {State?.currentLevelID ?? "无"}";
    }
    
    /// <summary>
    /// 打印调试信息
    /// </summary>
    public void PrintDebugInfo()
    {
        Debug.Log(GetDebugInfo());
    }
    
    #endregion
}

