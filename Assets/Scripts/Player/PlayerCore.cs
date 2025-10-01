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
    public PlayerData playerData; // 玩家配置数据（由Player设置）
    
    [Header("蓄力系统")]
    public ChargeSystem chargeSystem; // 蓄力系统引用
    
    // 核心组件
    private BallPhysics ballPhysics;
    private HealthBar healthBar;
    
    // 血量管理（实例变量，不从ScriptableObject读取）
    private float currentHealth;
    
    
    // 防重复触发机制
    private GameObject lastAttackedEnemy = null;
    private float lastAttackTime = 0f;
    private const float ATTACK_COOLDOWN = 0.1f; // 0.1秒冷却时间
    
    // 事件
    public System.Action OnBallStopped;
    public System.Action<float> OnHealthChanged;
    
    void Start()
    {
        InitializeCore();
    }
    
    void OnEnable()
    {
        // 订阅攻击事件
        GameEventBus.OnAttack += HandleAttack;
    }
    
    void OnDisable()
    {
        // 取消订阅攻击事件
        GameEventBus.OnAttack -= HandleAttack;
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
        
        // 订阅 BallPhysics 事件
        ballPhysics.OnBallStopped += OnBallStoppedHandler;
        
        // 初始化血量（currentHealth = maxHealth）
        float maxHealth = playerData != null ? playerData.maxHealth : 100f;
        currentHealth = maxHealth; // 初始化为满血
        InitializeHealthBar(currentHealth);
        
        // 确保球体在初始化后完全停止
        if (ballPhysics != null)
        {
            ballPhysics.ResetBall();
        }
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
        
        
        // 计算发射方向（朝向鼠标位置）
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector2 direction = (mouseWorldPos - transform.position).normalized;
        
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
                Debug.Log($"PlayerCore: 碰到陷阱模式的敌人 {collision.gameObject.name}");
                HandleTrapCollision(collision);
                return; // 陷阱伤害玩家，玩家不攻击敌人
            }
            else
            {
                // 正常敌人，玩家攻击
                Debug.Log($"PlayerCore: 碰到敌人 {collision.gameObject.name}");
                
                // 检查玩家阶段状态，只在Moving子阶段处理碰撞
                PlayerPhaseController playerPhaseController = PlayerPhaseController.Instance;
                if (playerPhaseController != null)
                {
                    Debug.Log($"PlayerCore: 当前玩家子阶段: {playerPhaseController.CurrentSubPhase}");
                    
                    if (playerPhaseController.CurrentSubPhase == PlayerPhaseController.PlayerSubPhase.Moving)
                    {
                        // Moving子阶段：玩家攻击敌人
                        Debug.Log("PlayerCore: 在Moving子阶段，执行攻击");
                        AttackEnemy(collision);
                    }
                    else
                    {
                        // 在其他子阶段，不处理碰撞
                        Debug.Log("PlayerCore: 不在Moving子阶段，不处理碰撞");
                    }
                }
                else
                {
                    Debug.LogWarning("PlayerCore: PlayerPhaseController 未找到！");
                }
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
    /// 处理陷阱碰撞（不受阶段限制）
    /// </summary>
    void HandleTrapCollision(Collision2D collision)
    {
        // 从陷阱找到父物体的 EnemyBehavior
        EnemyBehavior enemy = collision.gameObject.GetComponentInParent<EnemyBehavior>();
        if (enemy == null)
        {
            Debug.LogWarning($"PlayerCore: 陷阱 {collision.gameObject.name} 未找到 EnemyBehavior");
            return;
        }
        
        // 获取玩家对象（向上查找带 Player Tag 的）
        GameObject playerObject = gameObject;
        Transform current = transform;
        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                playerObject = current.gameObject;
                break;
            }
            current = current.parent;
        }
        
        // 让敌人对玩家造成陷阱伤害（攻击者发布事件）
        Vector3 hitPosition = collision.contacts[0].point;
        enemy.DealTrapDamageToPlayer(playerObject, hitPosition);
    }
    
    /// <summary>
    /// 玩家攻击敌人的逻辑（Moving阶段）
    /// </summary>
    void AttackEnemy(Collision2D collision)
    {
        Debug.Log($"PlayerCore: AttackEnemy 被调用，目标: {collision.gameObject.name}");
        
        // 如果碰到的是陷阱，不攻击敌人
        if (collision.gameObject.CompareTag("Trap"))
        {
            Debug.Log("PlayerCore: 碰到陷阱，不攻击敌人");
            return;
        }
        
        // 防重复触发检查 - 只对同一个敌人进行冷却
        if (lastAttackedEnemy == collision.gameObject && 
            Time.time - lastAttackTime < ATTACK_COOLDOWN)
        {
            Debug.Log("PlayerCore: 攻击冷却中，跳过");
            return;
        }
        
        // 更新最后攻击的敌人和时间
        lastAttackedEnemy = collision.gameObject;
        lastAttackTime = Time.time;
        
        // 获取敌人组件（从父物体中查找）
        Enemy enemy = collision.gameObject.GetComponentInParent<Enemy>();
        if (enemy == null) 
        {
            Debug.LogWarning($"PlayerCore: 目标 {collision.gameObject.name} 及其父物体中没有 Enemy 组件");
            return;
        }
        
        // 计算伤害和碰撞信息
        float damage = playerData != null ? playerData.damage : 50f;
        Vector3 hitPosition = collision.contacts[0].point; // 使用实际碰撞点
        Vector3 hitDirection = (collision.transform.position - transform.position).normalized;
        
        // 添加位置验证日志
        Vector3 oldHitPosition = (transform.position + collision.transform.position) * 0.5f;
        float positionDifference = Vector3.Distance(hitPosition, oldHitPosition);
        Debug.Log($"PlayerCore碰撞位置验证 - 实际碰撞点: {hitPosition}, " +
                  $"旧方法位置: {oldHitPosition}, 位置差异: {positionDifference:F3}");
        
        Debug.Log($"PlayerCore: 触发攻击事件 - 伤害: {damage}, 目标: {enemy.gameObject.name}");
        
        // 触发攻击事件，伤害处理由事件监听器处理
        gameObject.PublishAttack("Hit", hitPosition, enemy.gameObject, damage);
        
        // 处理充能力逻辑
        BallPhysics enemyBallPhysics = collision.gameObject.GetComponent<BallPhysics>();
        if (enemyBallPhysics != null)
        {
            // 检查是否还能获得充能力（基于速度）
            if (CanGetBoost())
            {
                // 计算碰撞方向
                Vector2 collisionDirection = (transform.position - collision.transform.position).normalized;
                
                // 给双方都添加充能力
                Vector2 boostForce = collisionDirection * playerData.ballData.hitBoostForce * playerData.ballData.hitBoostMultiplier;
                
                // 给白球添加充能力
                ballPhysics.ApplyForce(boostForce);
                
                // 给敌人添加充能力
                enemyBallPhysics.ApplyForce(-boostForce);
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
        
        Debug.Log($"PlayerCore: 开始处理伤害，当前血量: {currentHealth}, 受到伤害: {damage}");
        
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
        
        Debug.Log($"PlayerCore: 受到持续性伤害（忽略阶段），当前血量: {currentHealth}, 受到伤害: {damage}");
        
        // 执行扣血
        ApplyDamage(damage);
    }
    
    /// <summary>
    /// 应用伤害（共用的扣血逻辑）
    /// </summary>
    private void ApplyDamage(float damage)
    {
        // 更新血量数据（使用实例变量）
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        float maxHealth = playerData.maxHealth;
        
        Debug.Log($"PlayerCore: 血量更新完成，当前血量: {currentHealth}/{maxHealth}");
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
            Debug.Log("PlayerCore: 血条已更新");
        }
        else
        {
            Debug.LogWarning("PlayerCore: healthBar 为空，无法更新血条UI！");
        }
        
        // 触发血量变化事件
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
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
        if (playerData == null) return 1f;
        return currentHealth / playerData.maxHealth;
    }
    
    /// <summary>
    /// 是否存活
    /// </summary>
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    /// <summary>
    /// 获取当前血量
    /// </summary>
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    /// <summary>
    /// 获取最大血量
    /// </summary>
    public float GetMaxHealth()
    {
        return playerData != null ? playerData.maxHealth : 100f;
    }
    
    #endregion
    
    #region 事件处理
    
    /// <summary>
    /// 球停止运动事件处理
    /// </summary>
    void OnBallStoppedHandler(BallPhysics ball)
    {
        OnBallStopped?.Invoke();
        
        // 通知状态机
        PlayerStateMachine stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine != null)
        {
            stateMachine.OnBallStopped();
        }
    }
    
    /// <summary>
    /// 处理攻击事件（C# Action 实现）
    /// </summary>
    private void HandleAttack(AttackData attackData)
    {
        // 检查自己是否是攻击目标
        if (attackData.Target == gameObject && attackData.Damage > 0f)
        {
            // 处理伤害
            TakeDamage(attackData.Damage);
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
    
    /// <summary>
    /// 设置蓄力系统引用
    /// </summary>
    public void SetChargeSystem(ChargeSystem system)
    {
        chargeSystem = system;
    }
    
    #endregion
}
