using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家状态管理器 - 跨关卡数据保留的协调器
/// 整合血量、修饰器等关键数据，确保玩家游戏体验的连续性
/// 使用单例模式，确保跨场景存在
/// </summary>
public class PlayerStateManager : MonoBehaviour
{
    public static PlayerStateManager Instance { get; private set; }
    
    [Header("跨关卡保留的数据")]
    [SerializeField] private float savedCurrentHealth;
    [SerializeField] private List<StatModifierData> savedActiveModifiers = new List<StatModifierData>();
    
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    
    // 状态标志
    [SerializeField] private bool hasSavedData = false;
    
    #region Unity生命周期
    
    /// <summary>
    /// 单例模式初始化
    /// </summary>
    void Awake()
    {
        // 单例模式初始化
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 跨场景保留
            
            if (enableDebugLog)
            {
                Debug.Log("[PlayerStateManager] 单例初始化完成，将跨场景保留");
            }
        }
        else
        {
            // 如果已经存在实例，销毁当前对象
            if (enableDebugLog)
            {
                Debug.Log("[PlayerStateManager] 检测到重复实例，销毁当前对象");
            }
            Destroy(gameObject);
            return;
        }
    }
    
    /// <summary>
    /// 启动时尝试恢复之前保存的数据
    /// </summary>
    void Start()
    {
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 启动完成");
            if (hasSavedData)
            {
                Debug.Log($"[PlayerStateManager] 检测到保存的数据 - 血量: {savedCurrentHealth}, 修饰器: {savedActiveModifiers.Count}");
            }
        }
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 保存当前玩家状态（在关卡完成前调用）
    /// </summary>
    public void SavePlayerState()
    {
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 开始保存玩家状态");
        }
        
        // 保存血量数据
        SaveHealthData();
        
        // 保存修饰器数据
        SaveModifierData();
        
        hasSavedData = true;
        
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerStateManager] 玩家状态保存完成");
            Debug.Log($"  - 血量: {savedCurrentHealth}");
            Debug.Log($"  - 修饰器数量: {savedActiveModifiers.Count}");
        }
    }
    
    /// <summary>
    /// 恢复玩家状态（在新关卡开始时调用）
    /// </summary>
    public void RestorePlayerState()
    {
        if (enableDebugLog)
        {
            Debug.Log($"[PlayerStateManager] RestorePlayerState() 被调用");
            Debug.Log($"[PlayerStateManager] 当前状态: hasSavedData={hasSavedData}, enableDebugLog={enableDebugLog}");
            Debug.Log($"[PlayerStateManager] 保存的数据: 血量={savedCurrentHealth}, 修饰器数量={savedActiveModifiers?.Count ?? 0}");
        }
        
        if (!hasSavedData)
        {
            if (enableDebugLog)
            {
                Debug.Log("[PlayerStateManager] 没有保存的数据，跳过恢复");
            }
            return;
        }
        
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 开始恢复玩家状态");
        }
        
        // 恢复血量数据
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 开始恢复血量数据");
        }
        RestoreHealthData();
        
        // 恢复修饰器数据
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 开始恢复修饰器数据");
        }
        RestoreModifierData();
        
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 玩家状态恢复完成");
        }
    }
    
    /// <summary>
    /// 清除保存的数据（用于重新开始游戏）
    /// </summary>
    public void ClearSavedData()
    {
        savedCurrentHealth = 0f;
        savedActiveModifiers.Clear();
        hasSavedData = false;
        
        if (enableDebugLog)
        {
            Debug.Log("[PlayerStateManager] 已清除所有保存的数据");
        }
    }
    
    /// <summary>
    /// 检查是否有保存的数据
    /// </summary>
    /// <returns>是否有保存的数据</returns>
    public bool HasSavedData()
    {
        return hasSavedData;
    }
    
    /// <summary>
    /// 获取保存的血量
    /// </summary>
    /// <returns>保存的血量值</returns>
    public float GetSavedHealth()
    {
        return savedCurrentHealth;
    }
    
    /// <summary>
    /// 获取保存的修饰器数据
    /// </summary>
    /// <returns>保存的修饰器数据列表</returns>
    public List<StatModifierData> GetSavedModifiers()
    {
        return new List<StatModifierData>(savedActiveModifiers);
    }
    
    #endregion
    
    #region 私有方法 - 数据保存
    
    /// <summary>
    /// 保存血量数据
    /// </summary>
    private void SaveHealthData()
    {
        var playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore != null)
        {
            savedCurrentHealth = playerCore.GetCurrentHealth();
        }
        else
        {
            savedCurrentHealth = 0f;
            if (enableDebugLog)
            {
                Debug.LogWarning("[PlayerStateManager] 未找到PlayerCore，血量保存为0");
            }
        }
    }
    
    /// <summary>
    /// 保存修饰器数据
    /// </summary>
    private void SaveModifierData()
    {
        savedActiveModifiers.Clear();
        
        var statsManager = FindFirstObjectByType<PlayerStatsManager>();
        if (statsManager != null)
        {
            var modifiers = statsManager.SerializeActiveModifiers();
            if (modifiers != null)
            {
                savedActiveModifiers.AddRange(modifiers);
            }
        }
        else
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[PlayerStateManager] 未找到PlayerStatsManager，修饰器数据为空");
            }
        }
    }
    
    #endregion
    
    #region 私有方法 - 数据恢复
    
    /// <summary>
    /// 恢复血量数据
    /// </summary>
    private void RestoreHealthData()
    {
        var playerCore = FindFirstObjectByType<PlayerCore>();
        if (playerCore == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError("[PlayerStateManager] 未找到PlayerCore，无法恢复血量数据");
            }
            return;
        }
        
        if (savedCurrentHealth <= 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[PlayerStateManager] 保存的血量无效: {savedCurrentHealth}，跳过恢复");
            }
            return;
        }
        
        // 验证血量数据的合理性
        float maxHealth = playerCore.GetMaxHealth();
        if (savedCurrentHealth > maxHealth * 2f) // 允许一定的容错范围
        {
            if (enableDebugLog)
            {
                Debug.LogWarning($"[PlayerStateManager] 保存的血量异常: {savedCurrentHealth}，最大血量: {maxHealth}，使用最大血量");
            }
            savedCurrentHealth = maxHealth;
        }
        
        try
        {
            playerCore.RestoreHealth(savedCurrentHealth);
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerStateManager] 血量恢复成功: {savedCurrentHealth}");
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[PlayerStateManager] 血量恢复失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 恢复修饰器数据
    /// </summary>
    private void RestoreModifierData()
    {
        // 延迟恢复，确保PlayerStatsManager已经初始化完成
        StartCoroutine(DelayedModifierRestore());
    }
    
    /// <summary>
    /// 延迟恢复修饰器数据
    /// </summary>
    private System.Collections.IEnumerator DelayedModifierRestore()
    {
        // 等待几帧，确保PlayerStatsManager完全初始化
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();
        
        var statsManager = FindFirstObjectByType<PlayerStatsManager>();
        if (statsManager == null)
        {
            if (enableDebugLog)
            {
                Debug.LogError("[PlayerStateManager] 延迟恢复时仍未找到PlayerStatsManager，无法恢复修饰器数据");
            }
            yield break;
        }
        
        if (savedActiveModifiers == null || savedActiveModifiers.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.Log("[PlayerStateManager] 没有修饰器数据需要恢复");
            }
            yield break;
        }
        
        // 验证修饰器数据的有效性
        var validModifiers = new List<StatModifierData>();
        foreach (var modifierData in savedActiveModifiers)
        {
            if (IsValidModifierData(modifierData))
            {
                validModifiers.Add(modifierData);
            }
            else
            {
                if (enableDebugLog)
                {
                    Debug.LogWarning($"[PlayerStateManager] 跳过无效的修饰器数据: {modifierData?.GetDebugInfo() ?? "null"}");
                }
            }
        }
        
        if (validModifiers.Count == 0)
        {
            if (enableDebugLog)
            {
                Debug.LogWarning("[PlayerStateManager] 没有有效的修饰器数据可以恢复");
            }
            yield break;
        }
        
        try
        {
            statsManager.RestoreModifiers(validModifiers);
            if (enableDebugLog)
            {
                Debug.Log($"[PlayerStateManager] 修饰器恢复成功: {validModifiers.Count}/{savedActiveModifiers.Count}");
            }
        }
        catch (System.Exception e)
        {
            if (enableDebugLog)
            {
                Debug.LogError($"[PlayerStateManager] 修饰器恢复失败: {e.Message}");
            }
        }
    }
    
    /// <summary>
    /// 验证修饰器数据的有效性
    /// </summary>
    /// <param name="modifierData">要验证的修饰器数据</param>
    /// <returns>是否有效</returns>
    private bool IsValidModifierData(StatModifierData modifierData)
    {
        if (modifierData == null)
            return false;
        
        if (string.IsNullOrEmpty(modifierData.targetStat))
            return false;
        
        if (modifierData.value == 0f)
            return false;
        
        // 验证修饰器类型是否有效
        if (!System.Enum.IsDefined(typeof(StatModifierType), modifierData.type))
            return false;
        
        return true;
    }
    
    #endregion
    
    #region 调试方法
    
    [ContextMenu("测试保存玩家状态")]
    void TestSavePlayerState()
    {
        SavePlayerState();
    }
    
    [ContextMenu("测试恢复玩家状态")]
    void TestRestorePlayerState()
    {
        RestorePlayerState();
    }
    
    [ContextMenu("显示保存的数据")]
    void ShowSavedData()
    {
        Debug.Log($"[PlayerStateManager] 保存的数据:\n" +
                 $"血量: {savedCurrentHealth}\n" +
                 $"修饰器数量: {savedActiveModifiers.Count}\n" +
                 $"有保存数据: {hasSavedData}");
        
        if (savedActiveModifiers.Count > 0)
        {
            Debug.Log("修饰器详情:");
            foreach (var modifier in savedActiveModifiers)
            {
                Debug.Log($"  - {modifier.targetStat}: {modifier.value} ({modifier.type})");
            }
        }
    }
    
    
    #endregion
}

/// <summary>
/// 修饰器数据序列化结构
/// 用于跨关卡保存和恢复修饰器状态
/// </summary>
[System.Serializable]
public class StatModifierData
{
    public string targetStat;      // 目标属性名称
    public float value;           // 修饰值
    public StatModifierType type; // 修饰类型
    public string sourceType;     // 来源类型
    public float timeRemaining;   // 剩余时间
    
    /// <summary>
    /// 构造函数 - 从StatModifier创建
    /// </summary>
    /// <param name="modifier">要序列化的修饰器</param>
    public StatModifierData(StatModifier modifier)
    {
        if (modifier != null)
        {
            targetStat = modifier.targetStat;
            value = modifier.value;
            type = modifier.type;
            sourceType = modifier.source?.GetType().Name ?? "Unknown";
            timeRemaining = modifier.timeRemaining;
        }
    }
    
    /// <summary>
    /// 默认构造函数
    /// </summary>
    public StatModifierData()
    {
        targetStat = "";
        value = 0f;
        type = StatModifierType.Add;
        sourceType = "";
        timeRemaining = 0f;
    }
    
    /// <summary>
    /// 获取修饰器的调试信息
    /// </summary>
    /// <returns>调试信息字符串</returns>
    public string GetDebugInfo()
    {
        return $"{targetStat}: {value} ({type}) - 来源: {sourceType}, 剩余时间: {timeRemaining}";
    }
}
