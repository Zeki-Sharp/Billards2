using UnityEngine;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    [Header("数据设置")]
    public EnemyData enemyData; // 敌人配置数据
    
    private float currentHealth;
    private Player targetPlayer;
    
    // 防重复触发机制
    private float lastAttackTime = 0f;
    private const float ATTACK_COOLDOWN = 0.1f; // 0.1秒冷却时间
    private BallPhysics ballPhysics;
    private HealthBar healthBar; // 血条组件
    private Animator enemyAnimator; // 敌人动画组件
    
    // 攻击间隔控制
    private float lastEnemyAttackTime = 0f;
    
    // 巡逻移动相关
    private Vector2 patrolDirection;
    private Vector2 lastPosition;
    private float lastMoveTime;
    private Vector2 stuckCheckPosition; // 开始检查卡住时的位置
    private float stuckCheckStartTime; // 开始检查卡住时的时间
    
    // 事件
    public System.Action<float> OnHealthChanged;
    
    // 调试信息
    [SerializeField] private bool showDebugInfo = false;
    
    void Start()
    {
        // 检查配置数据
        if (enemyData == null)
        {
            Debug.LogError($"Enemy {name}: enemyData 未设置！");
            return;
        }
        
        // 初始化血量
        currentHealth = enemyData.maxHealth;
        targetPlayer = FindAnyObjectByType<Player>();
        
        // 获取或添加 BallPhysics 组件
        ballPhysics = GetComponent<BallPhysics>();
        if (ballPhysics == null)
        {
            ballPhysics = gameObject.AddComponent<BallPhysics>();
        }
        
        // 设置 BallData
        if (enemyData.ballData != null)
        {
            ballPhysics.ballData = enemyData.ballData;
        }
        else
        {
            Debug.LogError($"Enemy {name}: enemyData.ballData 未设置！");
        }
        
        // 初始化攻击范围朝向
        InitializeAttackRange();
        
        // 初始化血条
        InitializeHealthBar();
        
        // 初始化动画组件
        InitializeAnimator();
        
        // 初始化巡逻移动
        InitializePatrolMovement();
        
        // 播放生成特效
        PlaySpawnEffect();
    }
    
    void Update()
    {
        // 检查是否在敌人阶段，只有在敌人阶段才执行AI逻辑
        if (!IsInEnemyPhase())
        {
            return; // 玩家阶段完全静止，不执行任何AI逻辑
        }
        
        // 获取缩放后的时间
        float scaledDeltaTime = TimeManager.Instance != null ? 
            TimeManager.Instance.GetEnemyDeltaTime() : Time.deltaTime;
        
        // 更新动画（根据设置决定是否应用时间缩放）
        UpdateAnimations(scaledDeltaTime);
        
        // 敌人AI逻辑（根据配置化的移动方式）
        if (ShouldEnableAI() && targetPlayer != null && IsAlive())
        {
            ExecuteMovementAI(scaledDeltaTime);
            ExecuteAttackAI();
        }
    }
    
    /// <summary>
    /// 检查是否在敌人阶段
    /// </summary>
    bool IsInEnemyPhase()
    {
        GameFlowController gameFlowController = GameFlowController.Instance;
        return gameFlowController != null && gameFlowController.IsEnemyPhase;
    }
    
    /// <summary>
    /// 检查是否应该启用AI
    /// </summary>
    bool ShouldEnableAI()
    {
        return enemyData.enableAI;
    }
    
    /// <summary>
    /// 执行移动AI
    /// </summary>
    void ExecuteMovementAI(float deltaTime)
    {
        // 根据配置的移动方式执行不同的移动逻辑
        switch (enemyData.movementType)
        {
            case MovementType.FollowPlayer:
                MoveTowardsPlayer(deltaTime);
                break;
            case MovementType.Patrol:
                ExecutePatrolMovement(deltaTime);
                break;
        }
    }
    
    /// <summary>
    /// 执行攻击AI
    /// </summary>
    void ExecuteAttackAI()
    {
        // 根据配置的攻击方式执行不同的攻击逻辑
        switch (enemyData.attackType)
        {
            case AttackType.Contact:
                // 接触攻击在碰撞时触发，这里不需要额外逻辑
                break;
            case AttackType.Ranged:
                ExecuteRangedAttack();
                break;
        }
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
    
    public void InitializeAttackRange()
    {
        AttackRange attackRange = GetComponentInChildren<AttackRange>();
        if (attackRange != null && targetPlayer != null)
        {
            // 计算从敌人到白球的方向
            Vector2 direction = (targetPlayer.transform.position - transform.position).normalized;
            
            // 简化初始化：直接调用AttackRange的SetAttackDirection方法
            attackRange.SetAttackDirection(direction);
        }
    }
    
    /// <summary>
    /// 敌人朝向玩家移动
    /// </summary>
    void MoveTowardsPlayer(float deltaTime)
    {
        if (targetPlayer == null) return;
        
        // 获取移动速度（优先使用配置化设置）
        float currentMoveSpeed = GetMoveSpeed();
        
        // 计算朝向玩家的方向
        Vector2 direction = (targetPlayer.transform.position - transform.position).normalized;
        
        // 检查是否需要保持距离
        if (enemyData.maintainDistance)
        {
            float distance = Vector3.Distance(transform.position, targetPlayer.transform.position);
            if (distance < enemyData.followMinDistance)
            {
                // 距离太近，远离目标
                direction = -direction;
            }
            else if (distance > enemyData.followMaxDistance)
            {
                // 距离太远，接近目标
                // direction 已经是朝向目标的方向
            }
            else
            {
                // 在合适距离内，停止移动
                return;
            }
        }
        
        // 根据TimeManager设置决定是否使用缩放时间
        float actualDeltaTime = deltaTime;
        bool shouldAffect = TimeManager.Instance != null && TimeManager.Instance.ShouldAffectEnemyMovement();
        if (!shouldAffect)
        {
            actualDeltaTime = Time.deltaTime; // 使用正常时间
        }
        
        
        // 移动敌人
        transform.Translate(direction * currentMoveSpeed * actualDeltaTime);
    }
    
    /// <summary>
    /// 获取移动速度
    /// </summary>
    float GetMoveSpeed()
    {
        return enemyData.moveSpeed;
    }
    
    /// <summary>
    /// 初始化巡逻移动
    /// </summary>
    void InitializePatrolMovement()
    {
        // 随机选择初始方向
        float randomAngle = Random.Range(0f, 360f);
        patrolDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        
        // 初始化位置记录
        lastPosition = transform.position;
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        lastMoveTime = currentTime;
        stuckCheckPosition = transform.position;
        stuckCheckStartTime = currentTime;
        
    }
    
    /// <summary>
    /// 执行巡逻移动（反弹巡逻）
    /// </summary>
    void ExecutePatrolMovement(float deltaTime)
    {
        
        // 检查碰撞并处理反弹
        CheckCollisionAndBounce();
        
        // 执行移动
        float patrolSpeed = GetMoveSpeed() * enemyData.patrolSpeedMultiplier;
        
        // 根据TimeManager设置决定是否使用缩放时间
        float actualDeltaTime = deltaTime;
        if (TimeManager.Instance != null && !TimeManager.Instance.ShouldAffectEnemyMovement())
        {
            actualDeltaTime = Time.deltaTime;
        }
        
        Vector2 movement = patrolDirection * patrolSpeed * actualDeltaTime;
        transform.Translate(movement);
        
        // 更新位置记录
        UpdatePositionTracking();
        
        // 检查是否卡住（在移动后检查）
        if (CheckIfStuck())
        {
            // 强制随机方向
            SetRandomDirection();
        }
    }
    
    /// <summary>
    /// 检查是否卡住
    /// </summary>
    bool CheckIfStuck()
    {
        Vector2 currentPos = transform.position;
        float totalDistanceMoved = Vector2.Distance(currentPos, stuckCheckPosition);
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        float timeSinceCheckStart = currentTime - stuckCheckStartTime;
        
        // 如果移动距离足够，重置检查
        if (totalDistanceMoved >= enemyData.minMoveDistance)
        {
            stuckCheckPosition = currentPos;
            stuckCheckStartTime = currentTime;
            return false;
        }
        
        // 如果时间超过了检测时间，且移动距离不够，则认为卡住
        if (timeSinceCheckStart >= enemyData.stuckDetectionTime)
        {
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 检查碰撞并处理反弹
    /// </summary>
    bool CheckCollisionAndBounce()
    {
        //使用射线检测前方是否有碰撞
        float deltaTime = TimeManager.Instance != null ? TimeManager.Instance.GetEnemyDeltaTime() : Time.deltaTime;
        float checkDistance = GetMoveSpeed() * enemyData.patrolSpeedMultiplier * deltaTime * 2f;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, patrolDirection, checkDistance);
        
        if (hit.collider != null && hit.collider.gameObject != gameObject)
        {
            // 计算反弹方向
            Vector2 reflectDirection = Vector2.Reflect(patrolDirection, hit.normal);
            
            // 添加随机偏移
            float randomOffset = Random.Range(-enemyData.bounceRandomOffset, enemyData.bounceRandomOffset);
            float newAngle = Mathf.Atan2(reflectDirection.y, reflectDirection.x) * Mathf.Rad2Deg + randomOffset;
            patrolDirection = new Vector2(Mathf.Cos(newAngle * Mathf.Deg2Rad), Mathf.Sin(newAngle * Mathf.Deg2Rad));
            
            return true;
        }
        
        return false;
    }
    
    /// <summary>
    /// 设置随机方向
    /// </summary>
    void SetRandomDirection()
    {
        float randomAngle = Random.Range(0f, 360f);
        patrolDirection = new Vector2(Mathf.Cos(randomAngle * Mathf.Deg2Rad), Mathf.Sin(randomAngle * Mathf.Deg2Rad));
        lastPosition = transform.position;
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        lastMoveTime = currentTime;
        stuckCheckPosition = transform.position;
        stuckCheckStartTime = currentTime;
    }
    
    /// <summary>
    /// 更新位置跟踪
    /// </summary>
    void UpdatePositionTracking()
    {
        lastPosition = transform.position;
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        lastMoveTime = currentTime;
    }
    
    /// <summary>
    /// 执行远程攻击（占位符）
    /// </summary>
    void ExecuteRangedAttack()
    {
        if (targetPlayer == null) return;
        
        // 检查攻击冷却
        float attackInterval = GetAttackInterval();
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        if (currentTime - lastAttackTime < attackInterval)
        {
            return;
        }
        
        lastAttackTime = currentTime;
        
        // 占位符实现：远程攻击功能待实现
        
        // 显示攻击方向
        Vector3 attackDirection = (targetPlayer.transform.position - transform.position).normalized;
        Debug.DrawRay(transform.position, attackDirection * enemyData.attackRange, Color.red, 0.5f);
        
        // TODO: 实现实际的远程攻击逻辑
        // 1. 创建子弹预制体
        // 2. 设置子弹位置、方向、速度
        // 3. 播放发射音效
    }
    
    /// <summary>
    /// 获取攻击间隔
    /// </summary>
    float GetAttackInterval()
    {
        return enemyData.attackCooldown;
    }
    
    /// <summary>
    /// 敌人攻击玩家（接触攻击）
    /// </summary>
    void AttackPlayer()
    {
        if (targetPlayer == null) 
        {
            Debug.LogWarning($"Enemy {name}: targetPlayer 为空，无法攻击");
            return;
        }
        
        // 检查攻击间隔（根据设置决定是否受时停影响）
        float actualAttackInterval = GetAttackInterval();
        if (TimeManager.Instance != null && TimeManager.Instance.ShouldAffectEnemyAttackInterval())
        {
            // 如果攻击间隔受时停影响，需要调整间隔时间
            float timeScale = TimeManager.Instance.GetEnemyTimeScale();
            actualAttackInterval = actualAttackInterval / timeScale; // 时停时攻击间隔变长
        }
        
        float currentTime = TimeManager.Instance != null ? TimeManager.Instance.GetScaledTime() : Time.time;
        if (currentTime - lastEnemyAttackTime < actualAttackInterval)
        {
            return;
        }
        
        // 更新攻击时间
        lastEnemyAttackTime = currentTime;
        
        // 计算攻击方向和位置
        Vector3 attackPosition = (transform.position + targetPlayer.transform.position) * 0.5f;
        Vector3 attackDirection = (targetPlayer.transform.position - transform.position).normalized;
        
        // 获取敌人伤害值
        float damage = GetDamage();
        
        // 触发攻击事件
        EventTrigger.Attack("EnemyAttack", attackPosition, attackDirection, gameObject, targetPlayer.gameObject, damage);
    }
    
    /// <summary>
    /// 获取伤害值
    /// </summary>
    float GetDamage()
    {
        return enemyData.damage;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // 与玩家碰撞时的处理（只在Normal和Transition阶段）
        if (collision.gameObject.CompareTag("Player"))
        {
            // 检查游戏状态，只在Normal和Transition阶段处理碰撞
            GameFlowController gameFlowController = GameFlowController.Instance;
            if (gameFlowController != null && 
                (gameFlowController.IsNormalState || gameFlowController.IsTransitionState))
            {
                // Normal和Transition状态：敌人攻击玩家
                AttackPlayer();
            }
            else
            {
                // 在Charging阶段，不处理碰撞（由PlayerCore处理）
            }
        }
        
        // 撞墙时的处理
        if (collision.gameObject.CompareTag("Wall"))
        {
        // 检查是否还能获得充能力（基于速度）
        if (CanGetBoost())
        {
            // 计算撞墙方向（从墙壁指向敌人）
            Vector2 wallDirection = ((Vector2)transform.position - collision.contacts[0].point).normalized;
            
            // 给敌人添加撞墙充能力
            Vector2 wallBoostForce = wallDirection * enemyData.ballData.hitBoostForce * enemyData.ballData.hitBoostMultiplier;
            ballPhysics.ApplyForce(wallBoostForce);
        }
        }
    }
    
    
    public void TakeDamage(float damage)
    {
        float oldHealth = currentHealth;
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        float maxHealth = enemyData.maxHealth;
        // 更新血条
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, maxHealth);
        }
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
        
        // 检查死亡
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    void InitializeHealthBar()
    {
        // 查找血条组件
        healthBar = GetComponentInChildren<HealthBar>();
        if (healthBar != null)
        {
            healthBar.SetTarget(transform);
            healthBar.UpdateHealth(currentHealth, enemyData.maxHealth);
        }
        else
        {
            Debug.LogWarning($"敌人 {name} 未找到HealthBar组件，请确保血条预制体包含HealthBar脚本");
        }
    }
    
    /// <summary>
    /// 初始化动画组件
    /// </summary>
    void InitializeAnimator()
    {
        enemyAnimator = GetComponent<Animator>();
        if (enemyAnimator == null)
        {
            Debug.LogWarning($"敌人 {name} 未找到Animator组件");
        }
    }
    
    /// <summary>
    /// 更新敌人动画（根据设置决定是否应用时间缩放）
    /// </summary>
    void UpdateAnimations(float deltaTime)
    {
        if (enemyAnimator != null)
        {
            // 根据TimeManager设置决定是否应用时间缩放
            float timeScale = 1f;
            if (TimeManager.Instance != null && TimeManager.Instance.ShouldAffectEnemyAnimation())
            {
                timeScale = TimeManager.Instance.GetEnemyTimeScale();
            }
            
            enemyAnimator.speed = timeScale;
        }
    }
    
    void Die()
    { 
        // 禁用碰撞器，停止与白球的物理交互
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 触发死亡特效
        EventTrigger.Dead(transform.position, Vector3.zero, gameObject);
    }
    
    /// <summary>
    /// 处理攻击事件
    /// 当自己是攻击目标时处理伤害
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
    
    public float GetHealthPercentage()
    {
        return currentHealth / enemyData.maxHealth;
    }
    
    public bool IsAlive()
    {
        return currentHealth > 0;
    }
    
    public float GetCurrentSpeed()
    {
        return ballPhysics != null ? ballPhysics.GetSpeed() : 0f;
    }
    
    public bool IsMoving()
    {
        return ballPhysics != null && ballPhysics.IsMoving();
    }
    
    // 检查是否还能获得充能力（基于速度）
    private bool CanGetBoost()
    {
        return ballPhysics != null && ballPhysics.GetSpeed() > enemyData.ballData.boostSpeedThreshold;
    }
    
    /// <summary>
    /// 播放敌人生成特效
    /// </summary>
    void PlaySpawnEffect()
    {
        var effectPlayer = GetComponentInChildren<EffectPlayer>();
        if (effectPlayer != null)
        {
            effectPlayer.PlayEffect("Enemy Spawn Effect", transform.position);
        }
        else
        {
            Debug.LogWarning($"敌人 {name} 未找到 EffectPlayer 组件，无法播放生成特效");
        }
    }
}
