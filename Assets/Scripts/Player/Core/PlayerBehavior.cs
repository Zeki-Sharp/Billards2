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
/// - 实现 IDamageable 接口（新伤害系统）
/// </summary>
public class PlayerBehavior : MonoBehaviour, IDamageable
{
    [Header("数据设置")]
    // PlayerData 现在通过 Player 统一分发
    
    [Header("组件引用")]
    // 组件引用现在通过 Player 统一设置
    
    // 数据和组件引用（由 Player 统一设置）
    private PlayerData playerData;
    private PlayerAttackManager attackManager;
    private ChargeSystem chargeSystem;
    private PlayerStats statsManager; // ✅ 使用轻量级 Modifier 系统
    
    // 核心组件
    private BallPhysics ballPhysics;
    
    // 【三角形攻击】轨迹记录
    private Vector2? launchPosition;          // 发射起点
    private Vector2? firstCollisionPoint;     // 第一碰撞点
    private bool hasRecordedFirstCollision;   // 是否已记录第一次碰撞
    
    // 公共访问器（用于新伤害系统）
    public PlayerData PlayerData => playerData;
    
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
    public void SetStatsManager(PlayerStats manager)
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
        
        // ✅ 新伤害系统：在 Start() 注册到 DamageSystem（确保 DamageSystem 已初始化）
        RegisterToDamageSystem();
    }
    
    /// <summary>
    /// 注册到新伤害系统（支持多 Profile 组合）
    /// </summary>
    void RegisterToDamageSystem()
    {
        if (playerData == null || DamageSystem.Instance == null)
        {
            Debug.LogWarning($"[PlayerBehavior] 无法注册到 DamageSystem - playerData 或 DamageSystem 为空");
            return;
        }
        
        // 优先使用多 Profile 列表
        if (playerData.damageProfiles != null && playerData.damageProfiles.Count > 0)
        {
            DamageSystem.Instance.RegisterEntity(gameObject, playerData.damageProfiles);
            Debug.Log($"[PlayerBehavior] 注册到 DamageSystem，Profile 数量: {playerData.damageProfiles.Count}");
        }
        // 回退到单 Profile（向后兼容）
        else if (playerData.damageProfile != null)
        {
            DamageSystem.Instance.RegisterEntity(gameObject, playerData.damageProfile);
            Debug.Log($"[PlayerBehavior] 注册到 DamageSystem（单 Profile）");
        }
        else
        {
            Debug.LogWarning($"[PlayerBehavior] 无法注册到 DamageSystem - 未配置任何 DamageProfile");
        }
    }
    
    void OnEnable()
    {
        // 订阅新伤害系统事件
        GameEventBus.OnDamage += OnDamageReceived;
    }
    
    void OnDisable()
    {
        // 取消订阅伤害事件
        GameEventBus.OnDamage -= OnDamageReceived;
        
        // 注销实体
        var damageSystem = DamageSystem.Instance;
        if (damageSystem != null)
        {
            damageSystem.UnregisterEntity(gameObject);
        }
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
        
        // 初始化血量系统（由 Attributes 层和 GameSession 自动管理）
        if (statsManager != null)
        {
            Debug.Log($"[PlayerCore] ✅ 血量系统初始化完成: {statsManager.CurrentHealth:F1}/{statsManager.MaxHealth:F1}");
        }
        else
        {
            Debug.LogError("[PlayerCore] statsManager 为空，无法初始化血量！");
        }
        
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
    
    // ⚠️ 多角色系统改造：IsMoving() 方法已废弃
    // WASD移动功能已移除，不再需要此方法
    
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
        
        // 【三角形攻击】记录发射起点并重置碰撞记录
        launchPosition = transform.position;
        firstCollisionPoint = null;
        hasRecordedFirstCollision = false;
        
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
        // 【三角形攻击】记录第一次碰撞点（仅记录一次）
        if (!hasRecordedFirstCollision && collision.contacts.Length > 0)
        {
            firstCollisionPoint = collision.contacts[0].point;
            hasRecordedFirstCollision = true;
        }
        
        // 发布碰撞事件
        GameEventBus.PublishCollision(CollisionEvent.Create(gameObject, collision));
        
        // 撞击边界时的处理（非伤害逻辑）
        if (collision.gameObject.CompareTag("Wall"))
        {
            if (CanGetBoost())
            {
                Vector2 wallDirection = ((Vector2)transform.position - collision.contacts[0].point).normalized;
                Vector2 wallBoostForce = wallDirection * playerData.ballData.hitBoostForce * playerData.ballData.hitBoostMultiplier;
                ballPhysics.ApplyForce(wallBoostForce);
            }
        }
    }
    
    /// <summary>
    /// 处理 Trigger 碰撞（如敌人攻击范围、陷阱等）
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        // 发布 Trigger 碰撞事件
        // 注意：source 是玩家，target 是触碰到的 Trigger（如 AttackRange）
        GameEventBus.PublishCollision(CollisionEvent.CreateFromTrigger(gameObject, other));
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
            
            // 【三角形攻击】传递轨迹数据
            attackManager.ProcessBallStopped(ballPosition, launchPosition, firstCollisionPoint);
            
            Debug.Log($"PlayerCore: 球停止，委托 AttackManager 处理攻击 - 位置: {ballPosition}, 有碰撞: {firstCollisionPoint.HasValue}");
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
    /// 应用恢复（共用的恢复逻辑）
    /// </summary>
    private void ApplyHeal(float healAmount)
    {
        if (statsManager == null) return;
        
        // 1. 更新 PlayerStats（组件内部）
        statsManager.AddHealth(healAmount);
        Debug.Log($"PlayerCore: ✅ 回血 {healAmount:F1}，当前血量: {statsManager.CurrentHealth:F1}/{statsManager.MaxHealth:F1}");
        
        // 2. ✅ 同步到 TeamData（UI从这里读取）
        SyncHealthToTeamData();
        
        // 3. ✅ 发布角色治疗事件（UI会监听并更新血条）
        string characterID = GetMyCharacterID();
        if (!string.IsNullOrEmpty(characterID))
        {
            GameEventBus.PublishCharacterHealed(characterID, healAmount);
        }
    }
    
    /// <summary>
    /// ✅ 同步血量到 TeamData（UI从这里读取血量）
    /// </summary>
    private void SyncHealthToTeamData()
    {
        if (statsManager == null) return;
        
        // 获取角色ID
        string characterID = GetMyCharacterID();
        if (string.IsNullOrEmpty(characterID)) return;
        
        // 从 TeamData 中找到对应角色
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null) return;
        
        var character = teamData.characters.Find(c => c.characterID == characterID);
        if (character == null) return;
        
        // 同步血量
        character.currentHealth = statsManager.CurrentHealth;
        character.maxHealth = statsManager.MaxHealth;
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
        
        // 1. 更新 PlayerStats（组件内部）
        statsManager.SubtractHealth(damage);
        Debug.Log($"PlayerCore: 受到伤害 {damage:F1}，当前血量: {statsManager.CurrentHealth:F1}/{statsManager.MaxHealth:F1}");
        
        // 2. ✅ 同步到 TeamData（UI从这里读取）
        SyncHealthToTeamData();
        
        // 3. ✅ 发布角色受伤事件（UI会监听并更新血条）
        string characterID = GetMyCharacterID();
        if (!string.IsNullOrEmpty(characterID))
        {
            GameEventBus.PublishCharacterDamaged(characterID, damage, "Unknown");
        }
        
        // 4. 检查是否死亡
        if (statsManager.CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// ✅ 多角色系统改造：死亡处理
    /// </summary>
    void Die()
    {
        Debug.LogWarning($"[PlayerBehavior] {gameObject.name} 血量归零，开始死亡处理");
        
        // 获取角色ID
        string characterID = GetMyCharacterID();
        if (string.IsNullOrEmpty(characterID))
        {
            Debug.LogError($"[PlayerBehavior] {gameObject.name} 无法获取角色ID，死亡处理失败！");
            return;
        }
        
        // 发布角色死亡事件（DeathManager 会监听并处理）
        GameEventBus.PublishCharacterDied(characterID);
        
        Debug.LogWarning($"[PlayerBehavior] ✅ 已发布角色死亡事件：{characterID}");
        
        // 死亡的具体处理（禁用球体、更新TeamData等）由 DeathManager 统一处理
    }
    
    /// <summary>
    /// ✅ 多角色系统：获取当前角色ID
    /// </summary>
    string GetMyCharacterID()
    {
        var session = GameSession.GetOrCreateInstance();
        if (session != null && session.HasTeamData())
        {
            var teamData = session.GetTeamData();
            foreach (var character in teamData.characters)
            {
                if (character.ballInstance == gameObject)
                {
                    return character.characterID;
                }
            }
        }
        return null;
    }
    
    /// <summary>
    /// 获取血量百分比
    /// </summary>
    public float GetHealthPercentage()
    {
        return statsManager != null ? statsManager.HealthRatio : 1f;
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
    /// 恢复血量
    /// </summary>
    public void RestoreHealth(float health)
    {
        if (health < 0)
        {
            Debug.LogWarning("PlayerCore: 恢复血量不能为负数");
            return;
        }
        
        if (statsManager != null)
        {
            statsManager.SetHealth(health);
            Debug.Log($"PlayerCore: 恢复血量: {statsManager.CurrentHealth:F1}/{statsManager.MaxHealth:F1}");
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
    /// 接收伤害（新伤害系统，IDamageable 接口实现）
    /// </summary>
    public void OnDamageReceived(DamageEvent damageEvent)
    {
        // 检查是否是针对自己的伤害
        if (damageEvent.Target != gameObject)
        {
            return;
        }
        
        // 检查是否可以受伤
        if (!CanTakeDamage())
        {
            return;
        }
        
        // 应用伤害（复用现有逻辑）
        ApplyDamage(damageEvent.FinalDamage);
        
        // TODO: 附加效果（击退、眩晕等）
        if (damageEvent.KnockbackForce > 0)
        {
            // 击退逻辑（待实现）
        }
    }
    
    /// <summary>
    /// 是否可以受伤（IDamageable 接口实现）
    /// </summary>
    public bool CanTakeDamage()
    {
        // 检查是否已死亡
        if (statsManager != null && statsManager.CurrentHealth <= 0)
        {
            return false;
        }
        
        // TODO: 检查无敌帧、护盾等
        return true;
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
        
        // ⚠️ 多角色系统改造：已移除WASD移动功能，不需要停止移动
        
    }
    
    #endregion
    
    
    #region 组件设置
    // 组件设置方法已移动到上方统一管理
    #endregion
}
