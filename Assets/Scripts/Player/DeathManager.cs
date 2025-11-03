using UnityEngine;

/// <summary>
/// 死亡管理器 - 管理角色死亡和队伍全灭检测
/// 
/// 【核心职责】：
/// - 处理单个角色死亡
/// - 更新 TeamData 的存活状态
/// - 检查队伍全灭条件
/// - 触发游戏失败事件
/// 
/// 【执行顺序】：CONTROLLER 层 (0)
/// 【依赖】：GameSession, TeamData
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class DeathManager : SingletonManager<DeathManager>
{
    [Header("调试设置")]
    [SerializeField] private bool showDebugLog = true;
    
    #region SingletonManager 重写
    
    protected override bool PersistAcrossScenes => true;  // ✅ 跨场景持久化，全局管理角色死亡
    protected override bool EnableDebugLog => showDebugLog;
    
    protected override void OnManagerCreated()
    {
        // 订阅角色死亡事件
        GameEventBus.OnCharacterDied += HandleCharacterDeath;
        
        if (showDebugLog)
        {
            Debug.Log("[DeathManager] 初始化完成，订阅角色死亡事件");
        }
    }
    
    protected override void OnManagerDestroyed()
    {
        // 取消订阅
        GameEventBus.OnCharacterDied -= HandleCharacterDeath;
    }
    
    #endregion
    
    #region 死亡处理
    
    /// <summary>
    /// 处理角色死亡事件
    /// </summary>
    /// <param name="characterID">死亡的角色ID</param>
    void HandleCharacterDeath(string characterID)
    {
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError("[DeathManager] 角色ID为空！");
            return;
        }
        
        if (showDebugLog)
        {
            Debug.Log($"[DeathManager] 收到角色死亡事件：{characterID}");
        }
        
        // 获取 TeamData
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.LogError("[DeathManager] TeamData 为空，无法处理角色死亡！");
            return;
        }
        
        // 查找死亡的角色
        var character = teamData.characters.Find(c => c.characterID == characterID);
        if (character == null)
        {
            Debug.LogError($"[DeathManager] 找不到角色：{characterID}");
            return;
        }
        
        // 标记角色为死亡状态
        character.isAlive = false;
        character.currentHealth = 0f;
        
        if (showDebugLog)
        {
            Debug.Log($"[DeathManager] ✅ 角色 '{characterID}' 已标记为死亡");
        }
        
        // 禁用/隐藏球体
        if (character.ballInstance != null)
        {
            DisableBall(character.ballInstance);
        }
        
        // 检查队伍全灭
        int aliveCount = teamData.characters.FindAll(c => c.isAlive).Count;
        
        if (showDebugLog)
        {
            Debug.Log($"[DeathManager] 剩余存活角色：{aliveCount}/{teamData.characters.Count}");
        }
        
        if (aliveCount == 0)
        {
            // 队伍全灭，触发游戏失败
            OnTeamWiped();
        }
    }
    
    /// <summary>
    /// 禁用球体（死亡角色的处理）
    /// </summary>
    /// <param name="ballObject">球体GameObject</param>
    void DisableBall(GameObject ballObject)
    {
        if (ballObject == null) return;
        
        // 禁用渲染
        var spriteRenderer = ballObject.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = false;
        }
        
        // 禁用碰撞器
        var collider = ballObject.GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }
        
        // 禁用物理（停止移动）
        var rigidbody = ballObject.GetComponent<Rigidbody2D>();
        if (rigidbody != null)
        {
            rigidbody.linearVelocity = Vector2.zero;
            rigidbody.angularVelocity = 0f;
            rigidbody.simulated = false;  // 完全禁用物理模拟
        }
        
        // 可选：播放死亡特效
        // PlayDeathEffect(ballObject.transform.position);
        
        if (showDebugLog)
        {
            Debug.Log($"[DeathManager] 球体 '{ballObject.name}' 已禁用（渲染、碰撞、物理）");
        }
    }
    
    /// <summary>
    /// 队伍全灭处理
    /// </summary>
    void OnTeamWiped()
    {
        if (showDebugLog)
        {
            Debug.LogWarning("[DeathManager] ⚠️ 队伍全灭！触发游戏失败");
        }
        
        // 发布游戏失败事件
        GameEventBus.PublishGameOver();
        
        // 可选：延迟显示失败界面
        // Invoke(nameof(ShowGameOverUI), 1f);
    }
    
    #endregion
    
    #region 公共方法
    
    /// <summary>
    /// 检查队伍是否全灭
    /// </summary>
    /// <returns>是否全灭</returns>
    public bool IsTeamWiped()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return true;  // 没有队伍数据，视为全灭
        
        int aliveCount = teamData.characters.FindAll(c => c.isAlive).Count;
        return aliveCount == 0;
    }
    
    /// <summary>
    /// 获取存活角色数量
    /// </summary>
    /// <returns>存活角色数量</returns>
    public int GetAliveCharacterCount()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return 0;
        
        return teamData.characters.FindAll(c => c.isAlive).Count;
    }
    
    #endregion
    
    #region 调试
    
    [ContextMenu("测试队伍全灭")]
    void TestTeamWiped()
    {
        if (showDebugLog)
        {
            Debug.Log("[DeathManager] 测试：模拟队伍全灭");
        }
        OnTeamWiped();
    }
    
    [ContextMenu("显示队伍状态")]
    void ShowTeamStatus()
    {
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.Log("[DeathManager] TeamData 为空");
            return;
        }
        
        Debug.Log("=== 队伍状态 ===");
        foreach (var character in teamData.characters)
        {
            string status = character.isAlive ? "存活" : "死亡";
            Debug.Log($"[{character.positionIndex}号位] {character.characterData?.info.name ?? "未知"}: {status} ({character.currentHealth}/{character.maxHealth})");
        }
        Debug.Log($"存活角色：{GetAliveCharacterCount()}/{teamData.characters.Count}");
        Debug.Log("================");
    }
    
    #endregion
}

