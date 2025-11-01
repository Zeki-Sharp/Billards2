using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家攻击管理器 - 负责处理所有攻击相关的逻辑
/// 
/// 【核心职责】：
/// - 管理不同攻击模式的逻辑
/// - 处理攻击力计算和获取
/// - 处理碰撞攻击和范围攻击
/// - 发布攻击事件
/// - 管理攻击范围修改器
/// 
/// 【主要功能】：
/// - 攻击模式判断：根据 PlayerData 判断当前攻击模式
/// - 攻击力计算：获取基础攻击力和最终攻击力
/// - 攻击处理：处理碰撞攻击和范围攻击逻辑
/// - 事件发布：通过 GameEventBus 发布攻击事件
/// - 范围修改：支持动态修改攻击范围
/// 
/// 【设计原则】：
/// - 专注攻击逻辑，不处理物理和状态管理
/// - 通过 PlayerCore 获取必要的组件引用
/// - 保持与 PlayerStatsManagerV2 的兼容性
/// </summary>
public class PlayerAttackManager : MonoBehaviour
{
    [Header("攻击配置")]
    // PlayerData 现在通过 Player 统一分发
    
    [Header("范围攻击表现")]
    [SerializeField] private GameObject areaCirclePrefab;
    [SerializeField] private float circleDisplayDuration = 0.5f;
    
    // 数据和组件引用（由 Player 统一设置）
    private PlayerData playerData;
    private PlayerBehavior playerCore;
    private PlayerStats statsManager; // ✅ 使用轻量级 Modifier 系统
    
    // 范围攻击表现
    private GameObject currentAreaCircle;
    
    /// <summary>
    /// 初始化攻击管理器（由 Player 调用）
    /// </summary>
    public void Initialize()
    {
        InitializeAttackManager();
    }
    
    /// <summary>
    /// 设置 PlayerData（由 Player 调用）
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
        Debug.Log("PlayerAttackManager: PlayerData 已设置");
    }
    
    /// <summary>
    /// 设置 PlayerCore（由 Player 调用）
    /// </summary>
    public void SetPlayerCore(PlayerBehavior core)
    {
        playerCore = core;
        Debug.Log("PlayerAttackManager: PlayerCore 已设置");
    }
    
    void Start()
    {
        // 如果 Player 还没有调用 Initialize，则自动初始化
        if (playerData == null)
        {
            Debug.LogWarning("PlayerAttackManager: Player 尚未调用 Initialize，自动初始化");
            InitializeAttackManager();
        }
        
        // 测试攻击范围
        Invoke(nameof(TestAreaRadius), 2f);
    }
    
    /// <summary>
    /// 测试攻击范围（临时调试方法）
    /// </summary>
    private void TestAreaRadius()
    {
        float finalRadius = GetFinalAreaRadius();
        Debug.Log($"[测试] 当前攻击范围: {finalRadius}");
    }
    
    /// <summary>
    /// 初始化攻击管理器
    /// </summary>
    void InitializeAttackManager()
    {
        // 获取 StatsManager 引用
        statsManager = GetComponent<PlayerStats>();
        
        if (playerCore == null)
        {
            Debug.LogError("PlayerAttackManager: 未找到 PlayerCore 组件！");
        }
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerAttackManager: 未找到 PlayerStatsManagerV2 组件！");
        }
        
        if (playerData == null)
        {
            Debug.LogError("PlayerAttackManager: 未配置 PlayerData！");
        }
        
        Debug.Log($"PlayerAttackManager: 初始化完成 - 攻击模式: {playerData?.attackMode}");
    }
    
    #region 攻击力计算
    
    /// <summary>
    /// 获取当前攻击力（包含技能修正）
    /// </summary>
    public float GetCurrentAttackDamage()
    {
        // 优先从 PlayerStatsManagerV2 获取（包含技能修正）
        if (statsManager != null)
        {
            return statsManager.FinalDamage;
        }
        
        // 回退到基础攻击力
        return GetBaseAttackDamage();
    }
    
    /// <summary>
    /// 获取基础攻击力（不包含技能修正）
    /// </summary>
    public float GetBaseAttackDamage()
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerAttackManager: PlayerData 未配置，无法获取攻击力！");
            return 0f;
        }
        
        switch (playerData.attackMode)
        {
            case PlayerData.AttackMode.Collision:
                return playerData.collisionDamage;
            case PlayerData.AttackMode.Area:
                return playerData.areaDamage;
            default:
                Debug.LogError("PlayerAttackManager: 未知的攻击模式！");
                return 0f;
        }
    }
    
    #endregion
    
    #region 攻击处理
    
    
    /// <summary>
    /// 处理球停止事件（由 PlayerCore 调用）
    /// </summary>
    public void ProcessBallStopped(Vector3 ballPosition)
    {
        // ✅ 新伤害系统：发布停止事件
        StoppedEvent stoppedEvent = StoppedEvent.Create(gameObject, ballPosition);
        GameEventBus.PublishStopped(stoppedEvent);
        
        // ✅ 根据 DamageProfile 配置决定是否显示范围圈
        if (ShouldShowAreaCircle())
        {
            ShowAreaCircle(ballPosition);
            StartCoroutine(HideAreaCircleAfterDelay());
        }
    }
    
    /// <summary>
    /// 检查是否应该显示范围圈（根据 DamageProfile 配置）
    /// </summary>
    private bool ShouldShowAreaCircle()
    {
        if (playerData == null || playerData.damageProfile == null)
        {
            return false;
        }
        
        // 检查 DamageProfile 中是否有 Stopped 类型的规则
        foreach (var rule in playerData.damageProfile.rules)
        {
            if (rule != null && rule.triggerType == DamageTriggerType.Stopped)
            {
                return true;
            }
        }
        
        return false;
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 获取当前攻击模式
    /// </summary>
    public PlayerData.AttackMode CurrentAttackMode
    {
        get { return playerData != null ? playerData.attackMode : PlayerData.AttackMode.Collision; }
    }
    
    /// <summary>
    /// 检查是否可以进行攻击
    /// </summary>
    public bool CanPerformAttack()
    {
        if (playerCore == null) return false;
        
        PlayerStateMachine playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        if (playerStateMachine != null)
        {
            return playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.Moving ||
                   playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.MovingEnd;
        }
        return false;
    }
    
    #endregion
    
    #region 攻击范围获取
    
    /// <summary>
    /// 获取最终的攻击范围（应用所有修改器后）
    /// 从 PlayerStatsManagerV2 获取
    /// </summary>
    /// <returns>最终攻击范围</returns>
    public float GetFinalAreaRadius()
    {
        // 优先从 PlayerStatsManagerV2 获取（包含技能修正）
        if (statsManager != null)
        {
            float finalRadius = statsManager.FinalAreaRadius;
            Debug.Log($"[PlayerAttackManager] 获取最终攻击范围: {finalRadius} (基础: {playerData?.areaRadius ?? 0f})");
            return finalRadius;
        }
        
        // 回退到基础范围
        float baseRadius = playerData != null ? playerData.areaRadius : 0f;
        Debug.LogWarning($"[PlayerAttackManager] StatsManager为空，使用基础攻击范围: {baseRadius}");
        return baseRadius;
    }
    
    #endregion
    
    #region 范围攻击表现
    
    /// <summary>
    /// 显示范围攻击圈
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    private void ShowAreaCircle(Vector3 worldPosition)
    {
        if (areaCirclePrefab == null)
        {
            Debug.LogWarning("[PlayerAttackManager] 范围圈预制体未设置");
            return;
        }
        
        // 销毁旧的范围圈
        if (currentAreaCircle != null)
        {
            Destroy(currentAreaCircle);
        }
        
        // 创建新的范围圈
        currentAreaCircle = Instantiate(areaCirclePrefab);
        
        // 设置位置
        currentAreaCircle.transform.position = worldPosition;
        
        // 设置大小
        float radius = GetFinalAreaRadius();
        currentAreaCircle.transform.localScale = Vector3.one * radius * 2; // 直径
        
        // 确保显示在Player下层
        SpriteRenderer circleRenderer = currentAreaCircle.GetComponent<SpriteRenderer>();
        if (circleRenderer != null)
        {
            circleRenderer.sortingOrder = -10; // 比Player的sortingOrder小
        }
        
        Debug.Log($"[PlayerAttackManager] 显示范围圈: 位置 {worldPosition}, 半径 {radius}");
    }
    
    /// <summary>
    /// 延迟隐藏范围圈
    /// </summary>
    private System.Collections.IEnumerator HideAreaCircleAfterDelay()
    {
        yield return new WaitForSeconds(circleDisplayDuration);
        
        if (currentAreaCircle != null)
        {
            Destroy(currentAreaCircle);
            currentAreaCircle = null;
            Debug.Log("[PlayerAttackManager] 隐藏范围圈");
        }
    }
    
    #endregion
}
