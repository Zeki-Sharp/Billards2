using UnityEngine;

/// <summary>
/// 玩家核心组件 - 负责物理逻辑、碰撞处理和蓄力计算
/// 
/// 【核心职责】：
/// - 管理球体的物理运动和碰撞检测
/// - 处理蓄力系统和发射逻辑
/// - 管理血量系统和伤害处理
/// - 协调BallPhysics组件和UI显示
/// 
/// 【主要功能】：
/// - 物理控制：速度设置、移动检测、充能力计算
/// - 蓄力系统：蓄力进度、发射力度计算
/// - 战斗系统：血量管理、伤害处理、死亡逻辑
/// - 事件处理：球停止事件、攻击事件响应
/// 
/// 【设计原则】：
/// - 专注核心业务逻辑，不处理输入和状态管理
/// - 通过事件与其他组件通信
/// - 作为Player系统的业务逻辑中心
/// </summary>
public class PlayerCore : MonoBehaviour
{
    [Header("数据设置")]
    // PlayerData 现在通过 Player 统一分发
    
    [Header("组件引用")]
    // 组件引用现在通过 Player 统一设置
    
    // 数据和组件引用（由 Player 统一设置）
    private PlayerData playerData;
    private PlayerAttackManager attackManager;
    private ChargeSystem chargeSystem;
    private PlayerStatsManagerV2 statsManager; // ✅ 使用轻量级 Modifier 系统
    
    // 核心组件
    private BallPhysics ballPhysics;
    private HealthBar healthBar;
    
    // 血量管理 - ✅ 迁移到 Attributes 层
    // 通过 PlayerStatsManagerV2 的 Attributes 层管理
    // private float currentHealth; // ❌ 旧的变量，已废弃
    
    
    
    // 事件 - 血量变化事件已统一到 GameEventBus
    
    /// <summary>
    /// 检查指定的球是否是当前玩家的球
    /// </summary>
    public bool IsMyBall(BallPhysics ball)
    {
        return ball == this.ballPhysics;
    }
    
    #region 组件设置方法（由 Player 调用）
    
    /// <summary>
    /// 设置 PlayerData（由 Player 调用）
    /// </summary>
    public void SetPlayerData(PlayerData data)
    {
        playerData = data;
        Debug.Log("PlayerCore: PlayerData 已设置");
    }
    
    /// <summary>
    /// 设置 AttackManager（由 Player 调用）
    /// </summary>
    public void SetAttackManager(PlayerAttackManager manager)
    {
        attackManager = manager;
        Debug.Log("PlayerCore: AttackManager 已设置");
    }
    
    /// <summary>
    /// 设置 ChargeSystem（由 Player 调用）
    /// </summary>
    public void SetChargeSystem(ChargeSystem system)
    {
        chargeSystem = system;
        Debug.Log("PlayerCore: ChargeSystem 已设置");
    }
    
    /// <summary>
    /// 设置 StatsManager（由 Player 调用）
    /// </summary>
    public void SetStatsManager(PlayerStatsManagerV2 manager)
    {
        statsManager = manager;
        Debug.Log("PlayerCore: StatsManager 已设置");
    }
    
    #endregion
    
    /// <summary>
    /// 获取当前攻击力（委托给 AttackManager）
    /// </summary>
    public float GetCurrentAttackDamage()
    {
        if (attackManager != null)
        {
            return attackManager.GetCurrentAttackDamage();
        }
        
        Debug.LogError("PlayerCore: AttackManager 未配置，无法获取攻击力！");
        return 0f;
    }
    
    /// <summary>
    /// 获取基础攻击力（委托给 AttackManager）
    /// </summary>
    public float GetBaseAttackDamage()
    {
        if (attackManager != null)
        {
            return attackManager.GetBaseAttackDamage();
        }
        
        Debug.LogError("PlayerCore: AttackManager 未配置，无法获取基础攻击力！");
        return 0f;
    }
    
    /// <summary>
    /// 初始化 PlayerCore（由 Player 调用）
    /// </summary>
    public void Initialize()
    {
        InitializeCore();
    }
    
    void Start()
    {
        // 如果 Player 还没有调用 Initialize，则自动初始化
        if (playerData == null)
        {
            Debug.LogWarning("PlayerCore: Player 尚未调用 Initialize，自动初始化");
            InitializeCore();
        }
    }
    
    void OnEnable()
    {
        // 订阅伤害处理完成事件 - 应用最终伤害
        GameEventBus.OnDamageProcessed += HandleDamageProcessed;
    }
    
    void OnDisable()
    {
        // 取消订阅伤害处理完成事件
        GameEventBus.OnDamageProcessed -= HandleDamageProcessed;
    }
    
    #region 初始化
    
    /// <summary>
    /// 初始化核心组件
    /// </summary>
    void InitializeCore()
    {
        // 获取或添加 BallPhysics 组件
        ballPhysics = GetComponent<BallPhysics>();
        if (ballPhysics == null)
        {
            ballPhysics = gameObject.AddComponent<BallPhysics>();
        }
        
        // 设置 BallData
        if (playerData != null && playerData.ballData != null)
        {
            ballPhysics.ballData = playerData.ballData;
        }
        else
        {
            Debug.LogError("PlayerCore: 请设置 PlayerData 资源！");
        }
        
        // 攻击系统现在直接使用 PlayerData 配置，无需单独初始化
        
        // 订阅 GameEventBus 物理事件
        GameEventBus.OnBallStopped += OnBallStoppedHandler;
        
        // ✅ 初始化血量（使用 Attributes 层）
        if (statsManager != null)
        {
            // ✅ 血量已由 Attributes 层管理（在 RegisterBaseAttributes 中初始化为满血）
            // 已废弃 GameRuntimeData 血量存储
            Debug.Log($"[PlayerCore] ✅ 血量使用 Attributes 层管理，已初始化为满血");
            
            // 获取当前血量用于UI
            float currentHealth = statsManager.CurrentHealth;
            float maxHealth = statsManager.MaxHealth;
            
            InitializeHealthBar(currentHealth);
            
            Debug.Log($"[PlayerCore] ✅ 血量系统初始化完成（Attributes层）: {currentHealth:F1}/{maxHealth:F1}");
        }
        else
        {
            Debug.LogError("[PlayerCore] statsManager 为空，无法初始化血量！");
        }
        
        // 发布初始血量事件（SkillManager 在 SYSTEM 层已初始化，无需延迟）
        PublishInitialHealth();
        
        // 确保球体在初始化后完全停止
        if (ballPhysics != null)
        {
            ballPhysics.ResetBall();
        }
    }
    
    // 攻击行为初始化已移除，现在直接使用 PlayerData 配置
    
    void OnDestroy()
    {
        // 取消事件订阅
        GameEventBus.OnBallStopped -= OnBallStoppedHandler;
    }
    
    #endregion
    
    #region 物理控制
    
    /// <summary>
    /// 设置球体速度
    /// </summary>
    public void SetVelocity(Vector2 velocity)
    {
        if (ballPhysics != null)
        {
            ballPhysics.SetVelocity(velocity);
        }
    }
    
    /// <summary>
    /// 获取球体速度
    /// </summary>
    public Vector2 GetVelocity()
    {
        return ballPhysics != null ? ballPhysics.GetVelocity() : Vector2.zero;
    }
    
    /// <summary>
    /// 获取球体速度大小
    /// </summary>
    public float GetSpeed()
    {
        return ballPhysics != null ? ballPhysics.GetSpeed() : 0f;
    }
    
    /// <summary>
    /// 是否在物理移动
    /// </summary>
    public bool IsPhysicsMoving()
    {
        return ballPhysics != null && ballPhysics.IsMoving();
    }
    
    /// <summary>
    /// 是否在WASD移动（由MovementController管理）
    /// </summary>
    public bool IsMoving()
    {
        // 这个方法由PlayerMovementController实现
        PlayerMovementController movementController = GetComponent<PlayerMovementController>();
        return movementController != null && movementController.IsMoving;
    }
    
    #endregion
    
    #region 发射系统
    
    /// <summary>
    /// 发射蓄力攻击
    /// </summary>
    public void LaunchCharged()
    {
        if (chargeSystem == null)
        {
            Debug.LogError("PlayerCore: ChargeSystem未设置，无法发射");
            return;
        }
        
        float chargingPower = chargeSystem.GetChargingPower();
        float currentForce = chargeSystem.GetCurrentForce();
        
        // 获取发射方向（根据当前蓄力模式）
        Vector2 direction = chargeSystem.GetLaunchDirection(transform.position);
        
        // 使用蓄力系统的力度（直接使用蓄力系统计算的力度）
        float force = currentForce;
        
        // 发射
        Launch(direction, force);
    }
    
    /// <summary>
    /// 发射球体
    /// </summary>
    public void Launch(Vector2 direction, float force)
    {
        if (ballPhysics == null) return;
        
        if (ballPhysics.IsMoving()) 
        {
            Debug.LogWarning($"PlayerCore: 球正在移动，无法发射");
            return;
        }
        
        // 检查方向向量是否有效
        if (direction.magnitude < 0.1f)
        {
            Debug.LogWarning("PlayerCore: 发射方向无效，球不会移动");
            return;
        }
        
        // 触发发射特效事件
        gameObject.PublishEffect("Launch", transform.position, direction);
        
        // 使用 BallPhysics 的发射方法
        float launchSpeed = force;
        Vector2 velocity = direction.normalized * launchSpeed;
        
        // 直接设置刚体速度
        if (ballPhysics.GetComponent<Rigidbody2D>() != null)
        {
            ballPhysics.GetComponent<Rigidbody2D>().linearVelocity = velocity;
        }
        else
        {
            ballPhysics.SetVelocity(velocity);
        }
    }
    
    #endregion
    
    #region 碰撞处理
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 检查是否碰到敌人或其子物体
        EnemyBehavior enemy = collision.gameObject.GetComponent<EnemyBehavior>();
        if (enemy == null)
        {
            enemy = collision.gameObject.GetComponentInParent<EnemyBehavior>();
        }
        
        if (enemy != null)
        {
            // 检查是否处于陷阱模式
            if (enemy.IsTrapMode)
            {
                // 陷阱模式：让敌人对玩家造成陷阱伤害
                Debug.Log($"PlayerCore: 碰到陷阱模式的敌人 {collision.gameObject.name}，触发陷阱伤害");
                Vector3 hitPosition = collision.contacts[0].point;
                enemy.DealTrapDamageToPlayer(gameObject, hitPosition);
                return; // 陷阱伤害玩家，玩家不攻击敌人
            }
            
            // 正常敌人，玩家可能攻击
            Debug.Log($"PlayerCore: 碰到敌人 {collision.gameObject.name}");
            
            // 检查玩家状态，只在Moving状态处理碰撞
            PlayerStateMachine playerStateMachine = FindFirstObjectByType<PlayerStateMachine>();
            if (playerStateMachine != null)
            {
                Debug.Log($"PlayerCore: 当前玩家状态: {playerStateMachine.CurrentState}");
                
                if (playerStateMachine.CurrentState == PlayerStateMachine.PlayerState.Moving)
                {
                    // Moving状态：委托给 AttackManager 处理
                    if (attackManager != null)
                    {
                        attackManager.ProcessCollision(collision);
                    }
                    else
                    {
                        Debug.LogError("PlayerCore: AttackManager 未配置，无法处理碰撞攻击！");
                    }
                }
                else
                {
                    // 在其他状态，不处理碰撞
                    Debug.Log("PlayerCore: 不在Moving状态，不处理碰撞");
                }
            }
            else
            {
                Debug.LogWarning("PlayerCore: PlayerStateMachine 未找到！");
            }
            return;
        }
        
        // 撞击边界时的处理
        if (collision.gameObject.CompareTag("Wall"))
        {
            // 检查是否还能获得充能力（基于速度）
            if (CanGetBoost())
            {
                // 计算撞墙方向（从墙壁指向白球）
                Vector2 wallDirection = ((Vector2)transform.position - collision.contacts[0].point).normalized;
                
                // 给白球添加撞墙充能力
                Vector2 wallBoostForce = wallDirection * playerData.ballData.hitBoostForce * playerData.ballData.hitBoostMultiplier;
                ballPhysics.ApplyForce(wallBoostForce);
            }
        }
    }
    
    
    /// <summary>
    /// 检查是否还能获得充能力（基于速度）
    /// </summary>
    bool CanGetBoost()
    {
        return ballPhysics != null && playerData != null && playerData.ballData != null && ballPhysics.GetSpeed() > playerData.ballData.boostSpeedThreshold;
    }
    
    /// <summary>
    /// 处理球停止时的攻击（由 PlayerStateMachine 的 MovingEnd 状态调用）
    /// </summary>
    public void HandleBallStoppedAttack()
    {
        // 委托给 AttackManager 处理球停止攻击
        if (attackManager != null)
        {
            // 获取球的实际停止位置，而不是 PlayerCore 的位置
            Vector3 ballPosition = ballPhysics != null ? ballPhysics.transform.position : transform.position;
            attackManager.ProcessBallStopped(ballPosition);
            Debug.Log($"PlayerCore: 球停止，委托 AttackManager 处理攻击 - 位置: {ballPosition}");
        }
        else
        {
            Debug.LogError("PlayerCore: AttackManager 未配置，无法处理球停止攻击！");
        }
    }
    
    // 攻击处理方法已移动到 PlayerAttackManager
    
    #endregion
    
    #region 血量系统
    
    /// <summary>
    /// 受到伤害（阶段性，只在EnemyPhase生效）
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerCore: playerData 为空，无法处理伤害！");
            return;
        }
        
        // 检查游戏状态，只有在EnemyPhase阶段才能受击
        GameFlowController gameFlowController = GameFlowController.Instance;
        if (gameFlowController == null || !gameFlowController.IsEnemyPhase)
        {
            return;
        }
        
        Debug.Log($"PlayerCore: 开始处理伤害，当前血量: {GetCurrentHealth():F1}, 受到伤害: {damage}");
        
        // 执行扣血
        ApplyDamage(damage);
    }
    
    /// <summary>
    /// 受到伤害（忽略阶段限制，用于陷阱等持续性伤害）
    /// </summary>
    public void TakeDamageIgnorePhase(float damage)
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerCore: playerData 为空，无法处理伤害！");
            return;
        }
        
        Debug.Log($"PlayerCore: 受到持续性伤害（忽略阶段），当前血量: {GetCurrentHealth():F1}, 受到伤害: {damage}");
        
        // 执行扣血
        ApplyDamage(damage);
    }
    
    /// <summary>
    /// 恢复生命值 - ✅ 使用 Attributes 层
    /// </summary>
    public void Heal(float healAmount)
    {
        if (statsManager == null)
        {
            Debug.LogError("PlayerCore: statsManager 为空，无法恢复生命！");
            return;
        }
        
        Debug.Log($"PlayerCore: 恢复生命值，当前血量: {statsManager.CurrentHealth:F1}, 恢复量: {healAmount}");
        
        // 执行恢复
        ApplyHeal(healAmount);
    }
    
    /// <summary>
    /// 应用恢复（共用的恢复逻辑）- ✅ 使用 Attributes 层
    /// </summary>
    private void ApplyHeal(float healAmount)
    {
        if (statsManager == null) return;
        
        // ✅ 使用 Attributes 层加血
        statsManager.AddHealth(healAmount);
        
        // 获取最新血量
        float currentHealth = statsManager.CurrentHealth;
        float maxHealth = statsManager.MaxHealth;
        
        Debug.Log($"PlayerCore: ✅ 回血完成（Attributes层），当前血量: {currentHealth:F1}/{maxHealth:F1}");
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
            Debug.Log("PlayerCore: 血条已更新（恢复）");
        }
        else
        {
            Debug.LogWarning("PlayerCore: healthBar 为空，无法更新血条UI！");
        }
        
        // 触发血量变化事件 - 统一使用 GameEventBus
        GameEventBus.PublishHealthChanged(new HealthStateData
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        });
    }
    
    /// <summary>
    /// 应用伤害（共用的扣血逻辑）
    /// </summary>
    private void ApplyDamage(float damage)
    {
        if (statsManager == null)
        {
            Debug.LogError("PlayerCore: statsManager 为空，无法处理伤害！");
            return;
        }
        
        // ✅ 使用 Attributes 层扣血
        statsManager.SubtractHealth(damage);
        
        // 获取最新血量
        float currentHealth = statsManager.CurrentHealth;
        float maxHealth = statsManager.MaxHealth;
        
        Debug.Log($"PlayerCore: ✅ 扣血完成（Attributes层），当前血量: {currentHealth:F1}/{maxHealth:F1}");
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerCore: healthBar 为空，无法更新血条UI！");
        }
        
        // 触发血量变化事件
        GameEventBus.PublishHealthChanged(new HealthStateData
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        });
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 死亡处理
    /// </summary>
    void Die()
    {
        // 可以在这里添加死亡逻辑
    }
    
    /// <summary>
    /// 获取血量百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        return statsManager != null ? statsManager.HealthRatio : 1f;
    }
    
    
    /// <summary>
    /// 发布初始血量事件
    /// </summary>
    private void PublishInitialHealth()
    {
        if (statsManager == null) return;
        
        float currentHealth = statsManager.CurrentHealth;
        float maxHealth = statsManager.MaxHealth;
        
        Debug.Log($"[PlayerCore] ✅ 发布初始血量事件（Attributes层）: {currentHealth:F1}/{maxHealth:F1}");
        
        GameEventBus.PublishHealthChanged(new HealthStateData
        {
            CurrentHealth = currentHealth,
            MaxHealth = maxHealth
        });
    }
    
    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive()
    {
        return statsManager != null && statsManager.CurrentHealth > 0;
    }
    
    /// <summary>
    /// 获取当前血量 - ✅ 从 Attributes 层读取
    /// </summary>
    public float GetCurrentHealth()
    {
        return statsManager != null ? statsManager.CurrentHealth : 0f;
    }
    
    /// <summary>
    /// 获取最大血量 - ✅ 从 Attributes 层读取
    /// </summary>
    public float GetMaxHealth()
    {
        return statsManager != null ? statsManager.MaxHealth : 100f;
    }
    
    /// <summary>
    /// 恢复血量（用于跨关卡数据保留）
    /// </summary>
    /// <param name="health">要恢复的血量值</param>
    public void RestoreHealth(float health)
    {
        if (health < 0)
        {
            Debug.LogWarning("PlayerCore: 恢复血量不能为负数");
            return;
        }
        
        // ✅ 使用 Attributes 层设置血量
        if (statsManager != null)
        {
            statsManager.SetHealth(health);
            
            float currentHealth = statsManager.CurrentHealth;
            float maxHealth = statsManager.MaxHealth;
            
            Debug.Log($"PlayerCore: ✅ 恢复满血（Attributes层）: {currentHealth:F1}/{maxHealth:F1}");
            
            // 更新血条显示
            InitializeHealthBar(currentHealth);
            
            // 发布血量变化事件
            GameEventBus.PublishHealthChanged(new HealthStateData
            {
                CurrentHealth = currentHealth,
                MaxHealth = maxHealth
            });
        }
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 球停止运动事件处理（通过GameEventBus）
    /// </summary>
    void OnBallStoppedHandler(BallPhysics ball)
    {
        // 检查是否是自己的球
        if (ball != this.ballPhysics)
        {
            return;
        }
        
        // 状态机已直接订阅GameEventBus，无需触发额外事件
    }
    
    /// <summary>
    /// 处理攻击事件（C# Action 实现）
    /// </summary>
    private void HandleDamageProcessed(ProcessedDamageData processedData)
    {
        // 检查自己是否是攻击目标
        if (processedData.OriginalData.Target == gameObject && processedData.FinalDamage > 0f)
        {
            // 根据攻击类型决定处理方式
            string attackType = processedData.OriginalData.AttackType;
            
            if (attackType == "Trap")
            {
                // 陷阱伤害无视阶段限制（任何时候撞到都会扣血）
                TakeDamageIgnorePhase(processedData.FinalDamage);
                Debug.Log($"PlayerCore: 受到陷阱伤害 {processedData.FinalDamage}（类型：Trap，无视阶段）");
            }
            else if (attackType == "EnemyAttack")
            {
                // 敌人主动攻击，保持阶段检查（防止双向扣血）
                TakeDamage(processedData.FinalDamage);
                Debug.Log($"PlayerCore: 受到敌人攻击 {processedData.FinalDamage}（类型：EnemyAttack，有阶段检查）");
            }
            else
            {
                // 其他类型（如 "Hit"），保持阶段检查
                TakeDamage(processedData.FinalDamage);
                Debug.Log($"PlayerCore: 受到攻击 {processedData.FinalDamage}（类型：{attackType}，有阶段检查）");
            }
        }
    }
    
    #endregion
    
    #region 血条系统
    
    /// <summary>
    /// 初始化血条
    /// </summary>
    void InitializeHealthBar(float currentHealth)
    {
        // 查找血条组件
        healthBar = GetComponentInChildren<HealthBar>();
        if (healthBar != null)
        {
            float maxHealth = playerData != null ? playerData.maxHealth : 100f;
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        else
        {
            Debug.LogWarning("PlayerCore: 未找到HealthBar组件，请确保血条预制体包含HealthBar脚本");
        }
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 蓄力强度 (0-1) - 从ChargeSystem获取
    /// </summary>
    public float ChargingPower => chargeSystem != null ? chargeSystem.GetChargingPower() : 0f;
    
    /// <summary>
    /// 蓄力进度百分比 - 从ChargeSystem获取
    /// </summary>
    public float ChargingProgress => chargeSystem != null ? chargeSystem.GetChargingPower() * 100f : 0f;
    
    #endregion
    
    #region 重置和清理
    
    /// <summary>
    /// 重置为新回合
    /// </summary>
    public void ResetForNewTurn()
    {
        if (ballPhysics != null)
        {
            ballPhysics.ResetBallState();
        }
        
        PlayerMovementController movementController = GetComponent<PlayerMovementController>();
        if (movementController != null)
        {
            // 停止WASD移动
            movementController.StopWASDMovement();
        }
        
    }
    
    #endregion
    
    
    #region 组件设置
    // 组件设置方法已移动到上方统一管理
    #endregion
}
