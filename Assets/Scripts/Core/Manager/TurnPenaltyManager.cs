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
/// 
/// 【执行顺序】：SYSTEM 层 (-50)
/// 【依赖】：GameManager (CORE 层)
/// 【初始化】：OnManagerCreated 中订阅事件，Start 中初始化关卡
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.SYSTEM)]
public class TurnPenaltyManager : SingletonManager<TurnPenaltyManager>
{
    
    [Header("组件引用")]
    // ✅ 多角色系统：不再需要单一 PlayerBehavior 引用
    
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
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;
    protected override bool EnableDebugLog => showDebugInfo;
    
    protected override void OnManagerCreated()
    {
        // ✅ Manager 自身初始化（事件订阅）
        GameEventBus.OnPlayerPlayingPhaseStarted += OnPlayerTurnStarted;
        GameEventBus.OnLevelStarted += OnLevelStarted;
        
        if (showDebugInfo)
        {
            Debug.Log("TurnPenaltyManager: 单例创建成功（SYSTEM 层），将跨场景保留");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅事件
        GameEventBus.OnPlayerPlayingPhaseStarted -= OnPlayerTurnStarted;
        GameEventBus.OnLevelStarted -= OnLevelStarted;
    }
    
    #endregion
    
    void Start()
    {
        // ✅ 场景相关初始化
        InitializeCurrentLevel();
        
        if (showDebugInfo)
        {
            Debug.Log("TurnPenaltyManager: 初始化完成");
        }
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
        // ✅ 多角色系统：不再查找单一 PlayerBehavior，改为从 GameSession 获取队伍数据
        
        // 如果没有通过事件设置配置，尝试从 LevelManager 获取
        if (currentLevelConfig == null)
        {
            LevelManager levelManager = LevelManager.Instance;
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
        
        // ✅ 多角色系统：查找血量最高的存活角色
        var targetCharacter = FindHighestHealthCharacter();
        if (targetCharacter == null)
        {
            if (showDebugInfo)
            {
                Debug.Log("TurnPenaltyManager: 没有存活的角色，跳过惩罚");
            }
            return;
        }
        
        // 计算惩罚伤害
        float penaltyDamage = CalculatePenaltyDamage();
        
        // 施加惩罚
        ApplyPenalty(penaltyDamage, targetCharacter);
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
    /// 查找血量最高的存活角色
    /// </summary>
    private CharacterInstance FindHighestHealthCharacter()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null || teamData.characters == null || teamData.characters.Count == 0)
        {
            return null;
        }
        
        CharacterInstance highestHealthCharacter = null;
        float maxHealth = 0f;
        
        foreach (var character in teamData.characters)
        {
            // 跳过已死亡的角色
            if (!character.isAlive)
                continue;
            
            // 找血量最高的
            if (character.currentHealth > maxHealth)
            {
                maxHealth = character.currentHealth;
                highestHealthCharacter = character;
            }
        }
        
        return highestHealthCharacter;
    }
    
    /// <summary>
    /// 施加惩罚伤害（多角色版本）
    /// </summary>
    private void ApplyPenalty(float damage, CharacterInstance targetCharacter)
    {
        if (targetCharacter == null)
        {
            Debug.LogError("TurnPenaltyManager: 目标角色为 null，无法施加惩罚！");
            return;
        }
        
        // 累计惩罚伤害（用于统计）
        accumulatedPenaltyDamage += damage;
        
        // 显示警告信息
        if (showDebugInfo)
        {
            int exceededTurns = currentTurnCount - currentLevelConfig.turnPenaltyThreshold;
            string characterName = targetCharacter.characterData?.info?.name ?? targetCharacter.characterID;
            Debug.LogWarning($"⚠️ 回合惩罚触发！目标: {characterName} (血量:{targetCharacter.currentHealth:F1}), 回合数: {currentTurnCount} (超出{exceededTurns}回合), 扣除血量: {damage}, 累计扣血: {accumulatedPenaltyDamage}");
        }
        
        // 直接扣除角色血量
        targetCharacter.currentHealth -= damage;
        
        // 确保不会扣成负数
        if (targetCharacter.currentHealth < 0)
        {
            targetCharacter.currentHealth = 0;
        }
        
        // 发布角色受伤事件（让 UI 和其他系统响应）
        GameEventBus.PublishCharacterDamaged(targetCharacter.characterID, damage, "TurnPenalty");
        
        // 检查是否死亡
        if (targetCharacter.currentHealth <= 0)
        {
            targetCharacter.isAlive = false;
            GameEventBus.PublishCharacterDied(targetCharacter.characterID);
            
            if (showDebugInfo)
            {
                string characterName = targetCharacter.characterData?.info?.name ?? targetCharacter.characterID;
                Debug.LogWarning($"⚠️ {characterName} 因回合惩罚死亡！");
            }
        }
        
        // 可以在这里添加 UI 提示或特效
        // 例如：显示"回合超时！{角色名}失去XX生命值"的提示
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

