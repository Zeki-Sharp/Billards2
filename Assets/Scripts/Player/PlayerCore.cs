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
    
    // 蓄力相关（已移至ChargeSystem）
    
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
        EventTrigger.OnAttack += HandleAttack;
    }
    
    void OnDisable()
    {
        // 取消订阅攻击事件
        EventTrigger.OnAttack -= HandleAttack;
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
    /// 是否在微调移动（由MovementController管理）
    /// </summary>
    public bool IsMicroMoving()
    {
        // 这个方法由PlayerMovementController实现
        PlayerMovementController movementController = GetComponent<PlayerMovementController>();
        return movementController != null && movementController.IsMicroMoving;
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
        EventTrigger.Launch(transform.position, direction, gameObject);
        
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
        // 撞击敌人时的处理（只在Charging阶段）
        if (collision.gameObject.CompareTag("Enemy"))
        {
            // 检查游戏状态，只在Charging阶段处理碰撞
            GameFlowController gameFlowController = GameFlowController.Instance;
            if (gameFlowController != null && gameFlowController.IsChargingState)
            {
                // Charging状态：玩家攻击敌人
                AttackEnemy(collision);
            }
            else
            {
                // 在Normal/Transition阶段，不处理碰撞（由Enemy处理）
            }
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
    /// 玩家攻击敌人的逻辑（Charging阶段）
    /// </summary>
    void AttackEnemy(Collision2D collision)
    {
        // 防重复触发检查 - 只对同一个敌人进行冷却
        if (lastAttackedEnemy == collision.gameObject && 
            Time.time - lastAttackTime < ATTACK_COOLDOWN)
        {
            return;
        }
        
        // 更新最后攻击的敌人和时间
        lastAttackedEnemy = collision.gameObject;
        lastAttackTime = Time.time;
        
        // 获取敌人组件
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy == null) return;
        
        // 计算伤害和碰撞信息
        float damage = playerData != null ? playerData.damage : 50f;
        Vector3 hitPosition = (transform.position + collision.transform.position) * 0.5f;
        Vector3 hitDirection = (collision.transform.position - transform.position).normalized;
        
        // 触发攻击事件，伤害处理由事件监听器处理
        EventTrigger.Attack("Hit", hitPosition, hitDirection, gameObject, collision.gameObject, damage);
        
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
    /// 受到伤害
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (playerData == null)
        {
            Debug.LogError("PlayerCore: playerData 为空，无法处理伤害！");
            return;
        }
        
        // 检查当前玩家状态，只有在Idle状态才能受击
        PlayerStateMachine stateMachine = GetComponent<PlayerStateMachine>();
        if (stateMachine != null && !stateMachine.IsIdle)
        {
            return;
        }
        
        
        // 更新血量数据（使用实例变量）
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        float maxHealth = playerData.maxHealth;
        
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
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
            healthBar.SetTarget(transform);
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
            // 停止微调移动
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
