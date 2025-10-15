using UnityEngine;

/// <summary>
/// 玩家攻击管理器 - 负责处理所有攻击相关的逻辑
/// 
/// 【核心职责】：
/// - 管理不同攻击模式的逻辑
/// - 处理攻击力计算和获取
/// - 处理碰撞攻击和范围攻击
/// - 发布攻击事件
/// 
/// 【主要功能】：
/// - 攻击模式判断：根据 PlayerData 判断当前攻击模式
/// - 攻击力计算：获取基础攻击力和最终攻击力
/// - 攻击处理：处理碰撞攻击和范围攻击逻辑
/// - 事件发布：通过 GameEventBus 发布攻击事件
/// 
/// 【设计原则】：
/// - 专注攻击逻辑，不处理物理和状态管理
/// - 通过 PlayerCore 获取必要的组件引用
/// - 保持与 PlayerStatsManager 的兼容性
/// </summary>
public class PlayerAttackManager : MonoBehaviour
{
    [Header("攻击配置")]
    // PlayerData 现在通过 Player 统一分发
    
    // 数据和组件引用（由 Player 统一设置）
    private PlayerData playerData;
    private PlayerCore playerCore;
    private PlayerStatsManager statsManager;
    
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
    public void SetPlayerCore(PlayerCore core)
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
    }
    
    /// <summary>
    /// 初始化攻击管理器
    /// </summary>
    void InitializeAttackManager()
    {
        // 获取 StatsManager 引用
        statsManager = GetComponent<PlayerStatsManager>();
        
        if (playerCore == null)
        {
            Debug.LogError("PlayerAttackManager: 未找到 PlayerCore 组件！");
        }
        
        if (statsManager == null)
        {
            Debug.LogError("PlayerAttackManager: 未找到 PlayerStatsManager 组件！");
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
        // 优先从 PlayerStatsManager 获取（包含技能修正）
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
    /// 处理碰撞攻击
    /// </summary>
    public void HandleCollisionAttack(Collision2D collision)
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerAttackManager: PlayerData 未配置，无法执行碰撞攻击！");
            return;
        }
        
        // 检查是否是碰撞攻击模式
        if (playerData.attackMode != PlayerData.AttackMode.Collision)
        {
            Debug.Log("PlayerAttackManager: 当前不是碰撞攻击模式，跳过碰撞攻击");
            return;
        }
        
        // 检查碰撞对象是否是敌人（包括父物体）
        EnemyBehavior enemy = collision.gameObject.GetComponent<EnemyBehavior>();
        if (enemy == null)
        {
            enemy = collision.gameObject.GetComponentInParent<EnemyBehavior>();
        }
        
        if (enemy != null)
        {
            float finalDamage = GetCurrentAttackDamage();
            gameObject.PublishAttack("Hit", collision.contacts[0].point, enemy.gameObject, finalDamage);
            Debug.Log($"[PlayerAttackManager] 碰撞攻击命中 {enemy.name}，造成伤害: {finalDamage}");
        }
    }
    
    /// <summary>
    /// 处理范围攻击
    /// </summary>
    public void HandleAreaAttack(Vector3 ballPosition)
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerAttackManager: PlayerData 未配置，无法执行范围攻击！");
            return;
        }
        
        // 检查是否是范围攻击模式
        if (playerData.attackMode != PlayerData.AttackMode.Area)
        {
            Debug.Log("PlayerAttackManager: 当前不是范围攻击模式，跳过范围攻击");
            return;
        }
        
        Collider2D[] enemies = Physics2D.OverlapCircleAll(
            ballPosition, 
            playerData.areaRadius, 
            playerData.enemyLayerMask
        );
        
        float finalDamage = GetCurrentAttackDamage();
        
        int hitCount = 0;
        foreach (Collider2D enemyCollider in enemies)
        {
            // 检查敌人组件（包括父物体）
            EnemyBehavior enemy = enemyCollider.GetComponent<EnemyBehavior>();
            if (enemy == null)
            {
                enemy = enemyCollider.GetComponentInParent<EnemyBehavior>();
            }
            
            if (enemy != null)
            {
                gameObject.PublishAttack("Hit", ballPosition, enemy.gameObject, finalDamage);
                hitCount++;
                Debug.Log($"[PlayerAttackManager] 范围攻击命中 {enemy.name}，造成伤害: {finalDamage}");
            }
        }
        
        if (hitCount > 0)
        {
            Debug.Log($"[PlayerAttackManager] 范围攻击完成，命中 {hitCount} 个敌人，范围: {playerData.areaRadius}");
        }
        else
        {
            Debug.Log("[PlayerAttackManager] 范围攻击未命中任何敌人");
        }
    }
    
    /// <summary>
    /// 处理碰撞事件（由 PlayerCore 调用）
    /// </summary>
    public void ProcessCollision(Collision2D collision)
    {
        // 检查玩家状态，只在Moving状态处理碰撞
        PlayerStateMachine playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
        if (playerStateMachine != null && playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.Moving)
        {
            HandleCollisionAttack(collision);
        }
        else
        {
            Debug.Log("PlayerAttackManager: 不在Moving状态，不处理碰撞攻击");
        }
    }
    
    /// <summary>
    /// 处理球停止事件（由 PlayerCore 调用）
    /// </summary>
    public void ProcessBallStopped(Vector3 ballPosition)
    {
        HandleAreaAttack(ballPosition);
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
}
