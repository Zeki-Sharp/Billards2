using UnityEngine;

/// <summary>
/// 回合惩罚管理器 - 管理超过回合限制后的扣血惩罚
/// 
/// 【核心职责】：
/// - 监听玩家回合开始事件，累计回合数
/// - 根据关卡配置判断是否超过回合限制
/// - 对玩家施加惩罚性伤害
/// - 提供回合数查询接口
/// 
/// 【设计原则】：
/// - 事件驱动：通过 GameEventBus 监听回合事件
/// - 配置驱动：从 LevelConfig 读取惩罚参数
/// - 单一职责：只负责回合计数和惩罚逻辑
/// - 松耦合：通过 PlayerCore 的公共接口施加伤害
/// </summary>
public class TurnPenaltyManager : MonoBehaviour
{
    public static TurnPenaltyManager Instance { get; private set; }
    
    [Header("组件引用")]
    [SerializeField] [Tooltip("玩家核心组件引用")]
    private PlayerCore playerCore;
    
    [Header("关卡配置")]
    [SerializeField] [Tooltip("当前关卡配置")]
    private LevelConfig currentLevelConfig;
    
    [Header("调试设置")]
    [SerializeField] [Tooltip("是否显示调试信息")]
    private bool showDebugInfo = true;
    
    // 当前回合计数
    private int currentTurnCount = 0;
    
    // 累计惩罚伤害（用于递增伤害计算）
    private float accumulatedPenaltyDamage = 0f;
    
    void Awake()
    {
        // 单例模式 + 跨场景持久化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (showDebugInfo)
            {
                Debug.Log("TurnPenaltyManager: 初始化为跨场景单例");
            }
        }
        else
        {
            if (showDebugInfo)
            {
                Debug.Log("TurnPenaltyManager: 发现重复实例，销毁");
            }
            Destroy(gameObject);
            return;
        }
    }
    
    void Start()
    {
        // 订阅事件
        GameEventBus.OnPlayerPlayingPhaseStarted += OnPlayerTurnStarted;
        GameEventBus.OnLevelStarted += OnLevelStarted;
        
        // 初始化当前关卡
        InitializeCurrentLevel();
        
        if (showDebugInfo)
        {
            Debug.Log("TurnPenaltyManager: 初始化完成，已订阅事件");
        }
    }
    
    void OnDestroy()
    {
        // 取消订阅事件
        GameEventBus.OnPlayerPlayingPhaseStarted -= OnPlayerTurnStarted;
        GameEventBus.OnLevelStarted -= OnLevelStarted;
    }
    
    /// <summary>
    /// 关卡开始事件处理（自动重置回合数并获取新关卡配置）
    /// </summary>
    private void OnLevelStarted(int levelIndex, LevelConfig levelConfig)
    {
        if (showDebugInfo)
        {
            Debug.Log($"TurnPenaltyManager: 检测到关卡开始 - {levelConfig?.levelName ?? "未知关卡"}");
        }
        
        // 重置回合计数
        ResetTurnCount();
        
        // 设置新的关卡配置
        SetLevelConfig(levelConfig);
        
        // 重新查找 PlayerCore（新场景中的实例）
        InitializeCurrentLevel();
    }
    
    /// <summary>
    /// 初始化当前关卡（自动查找组件和配置）
    /// </summary>
    private void InitializeCurrentLevel()
    {
        // 自动查找 PlayerCore（每个场景都需要重新查找）
        playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore == null && showDebugInfo)
        {
            Debug.LogWarning("TurnPenaltyManager: 未找到 PlayerCore 组件（可能还未加载场景）");
        }
        
        // 如果没有通过事件设置配置，尝试从 LevelManager 获取
        if (currentLevelConfig == null)
        {
            LevelManager levelManager = FindFirstObjectByType<LevelManager>();
            if (levelManager != null)
            {
                currentLevelConfig = levelManager.GetCurrentLevelConfig();
                if (currentLevelConfig != null && showDebugInfo)
                {
                    Debug.Log($"TurnPenaltyManager: 从 LevelManager 获取到关卡配置: {currentLevelConfig.levelName}");
                }
            }
        }
    }
    
    /// <summary>
    /// 玩家回合开始事件处理
    /// </summary>
    private void OnPlayerTurnStarted()
    {
        // 回合计数增加
        currentTurnCount++;
        
        if (showDebugInfo)
        {
            Debug.Log($"TurnPenaltyManager: 玩家回合开始 - 当前回合数: {currentTurnCount}");
        }
        
        // 检查是否需要施加惩罚
        CheckAndApplyPenalty();
    }
    
    /// <summary>
    /// 检查并施加回合惩罚
    /// </summary>
    private void CheckAndApplyPenalty()
    {
        // 检查配置有效性
        if (currentLevelConfig == null)
        {
            if (showDebugInfo && currentTurnCount == 1)
            {
                Debug.LogWarning("TurnPenaltyManager: 未设置关卡配置，回合惩罚系统未启用");
            }
            return;
        }
        
        if (!currentLevelConfig.enableTurnPenalty)
        {
            return;
        }
        
        // 检查是否超过回合限制
        if (currentTurnCount <= currentLevelConfig.turnPenaltyThreshold)
        {
            return;
        }
        
        // 检查玩家是否存活
        if (playerCore == null || !playerCore.IsAlive())
        {
            return;
        }
        
        // 计算惩罚伤害
        float penaltyDamage = CalculatePenaltyDamage();
        
        // 施加惩罚
        ApplyPenalty(penaltyDamage);
    }
    
    /// <summary>
    /// 计算惩罚伤害
    /// </summary>
    private float CalculatePenaltyDamage()
    {
        float damage = currentLevelConfig.damagePerTurn;
        
        // 如果启用递增伤害
        if (currentLevelConfig.increasingDamage)
        {
            // 计算超出回合数
            int exceededTurns = currentTurnCount - currentLevelConfig.turnPenaltyThreshold;
            
            // 累计递增伤害：基础伤害 + (超出回合数 - 1) * 递增量
            // 例如：第21回合 = 5，第22回合 = 5+1=6，第23回合 = 5+2=7
            damage += (exceededTurns - 1) * currentLevelConfig.damageIncrement;
        }
        
        return damage;
    }
    
    /// <summary>
    /// 施加惩罚伤害
    /// </summary>
    private void ApplyPenalty(float damage)
    {
        if (playerCore == null)
        {
            Debug.LogError("TurnPenaltyManager: PlayerCore 为 null，无法施加惩罚！");
            return;
        }
        
        // 累计惩罚伤害（用于统计）
        accumulatedPenaltyDamage += damage;
        
        // 显示警告信息
        if (showDebugInfo)
        {
            int exceededTurns = currentTurnCount - currentLevelConfig.turnPenaltyThreshold;
            Debug.LogWarning($"⚠️ 回合惩罚触发！回合数: {currentTurnCount} (超出{exceededTurns}回合), 扣除血量: {damage}, 累计扣血: {accumulatedPenaltyDamage}");
        }
        
        // 通过 PlayerCore 施加伤害（使用 IgnorePhase 版本，确保一定会生效）
        playerCore.TakeDamageIgnorePhase(damage);
        
        // 可以在这里添加 UI 提示或特效
        // 例如：显示"回合超时！失去XX生命值"的提示
    }
    
    /// <summary>
    /// 重置回合计数（用于切换关卡或重新开始）
    /// </summary>
    public void ResetTurnCount()
    {
        int oldCount = currentTurnCount;
        float oldDamage = accumulatedPenaltyDamage;
        
        currentTurnCount = 0;
        accumulatedPenaltyDamage = 0f;
        
        if (showDebugInfo && (oldCount > 0 || oldDamage > 0))
        {
            Debug.Log($"TurnPenaltyManager: 重置回合计数 - 回合数: {oldCount} → 0, 累计伤害: {oldDamage:F1} → 0");
        }
    }
    
    /// <summary>
    /// 设置关卡配置
    /// </summary>
    public void SetLevelConfig(LevelConfig config)
    {
        currentLevelConfig = config;
        
        if (showDebugInfo && config != null)
        {
            Debug.Log($"TurnPenaltyManager: 设置关卡配置: {config.levelName}");
            
            if (config.enableTurnPenalty)
            {
                Debug.Log($"  - 回合限制: {config.turnPenaltyThreshold} 回合");
                Debug.Log($"  - 每回合扣血: {config.damagePerTurn}");
                Debug.Log($"  - 递增伤害: {config.increasingDamage}");
                
                if (config.increasingDamage)
                {
                    Debug.Log($"  - 伤害递增量: {config.damageIncrement}");
                }
            }
            else
            {
                Debug.Log("  - 回合惩罚未启用");
            }
        }
    }
    
    #region 公共接口
    
    /// <summary>
    /// 获取当前回合数
    /// </summary>
    public int GetCurrentTurnCount()
    {
        return currentTurnCount;
    }
    
    /// <summary>
    /// 获取剩余安全回合数
    /// </summary>
    public int GetRemainingTurns()
    {
        if (currentLevelConfig == null || !currentLevelConfig.enableTurnPenalty)
        {
            return -1; // -1 表示无限制
        }
        
        int remaining = currentLevelConfig.turnPenaltyThreshold - currentTurnCount;
        return Mathf.Max(0, remaining);
    }
    
    /// <summary>
    /// 是否已超过回合限制
    /// </summary>
    public bool IsOverTurnLimit()
    {
        if (currentLevelConfig == null || !currentLevelConfig.enableTurnPenalty)
        {
            return false;
        }
        
        return currentTurnCount > currentLevelConfig.turnPenaltyThreshold;
    }
    
    /// <summary>
    /// 获取累计惩罚伤害
    /// </summary>
    public float GetAccumulatedPenaltyDamage()
    {
        return accumulatedPenaltyDamage;
    }
    
    /// <summary>
    /// 获取下一回合将扣除的血量（预测）
    /// </summary>
    public float GetNextTurnPenaltyDamage()
    {
        if (currentLevelConfig == null || !currentLevelConfig.enableTurnPenalty)
        {
            return 0f;
        }
        
        // 预测下一回合（currentTurnCount + 1）的伤害
        int nextTurn = currentTurnCount + 1;
        
        if (nextTurn <= currentLevelConfig.turnPenaltyThreshold)
        {
            return 0f;
        }
        
        float damage = currentLevelConfig.damagePerTurn;
        
        if (currentLevelConfig.increasingDamage)
        {
            int exceededTurns = nextTurn - currentLevelConfig.turnPenaltyThreshold;
            damage += (exceededTurns - 1) * currentLevelConfig.damageIncrement;
        }
        
        return damage;
    }
    
    /// <summary>
    /// 获取调试信息
    /// </summary>
    public string GetDebugInfo()
    {
        string info = $"回合惩罚管理器状态:\n";
        info += $"- 当前回合: {currentTurnCount}\n";
        
        if (currentLevelConfig != null && currentLevelConfig.enableTurnPenalty)
        {
            info += $"- 回合限制: {currentLevelConfig.turnPenaltyThreshold}\n";
            info += $"- 剩余安全回合: {GetRemainingTurns()}\n";
            info += $"- 是否超限: {IsOverTurnLimit()}\n";
            info += $"- 累计惩罚伤害: {accumulatedPenaltyDamage}\n";
            info += $"- 下回合惩罚: {GetNextTurnPenaltyDamage()}";
        }
        else
        {
            info += "- 回合惩罚未启用";
        }
        
        return info;
    }
    
    #endregion
}

