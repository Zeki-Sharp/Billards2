using UnityEngine;
using System.Collections;
using MoreMountains.Feedbacks;

/// <summary>
/// 敌人行为脚本 - 纯行为逻辑
/// 实现 IDamageable 接口（新伤害系统）
/// </summary>
public class EnemyBehavior : MonoBehaviour, IDamageable
{
    [Header("数据设置")]
    [Tooltip("敌人数据配置。手动放置的敌人需要在此配置，通过 EnemySpawner 生成的敌人会自动设置")]
    public EnemyData enemyData;
    private int currentLevel = 1;  // 当前等级
    
    /// <summary>
    /// 当前等级配置（快捷访问）
    /// </summary>
    private EnemyLevelConfig CurrentLevelConfig => enemyData?.GetLevelConfig(currentLevel);
    
    [Header("移动设置")]
    // 移动状态由 BaseMovementBehavior 统一管理
    
    [Header("行为系统")]
    private IMovementBehavior movementBehavior;  // 移动行为组件
    private IAttackBehavior attackBehavior;      // 攻击行为组件
    
    [Header("组件引用")]
    public AttackRange attackRange;
    public MMFeedbacks attackEffect;  // 攻击特效MMF组件（直接引用）
    private Transform player;
    private Vector2 currentMovementDirection = Vector2.zero;
    
    [Header("攻击范围管理")]
    private Transform attackArea;  // 攻击范围预制体引用
    
    [Header("属性管理")]
    private EnemyStats statsManager;  // ✅ 三层属性系统管理器
    
    [Header("血量管理")]
    private bool isDead = false;
    
    [Header("UI组件")]
    public HealthBar healthBar;  // 血条UI引用
    
    [Header("陷阱系统")]
    private bool isTrapMode = false;   // 是否处于陷阱模式（玩家碰撞时触发陷阱伤害而非攻击敌人）
    
    /// <summary>
    /// 是否处于陷阱模式（公开属性，供 PlayerCore 检查）
    /// </summary>
    public bool IsTrapMode => isTrapMode;
    
    /// <summary>
    /// Awake - 初始化组件引用（确保在 SetEnemyData 调用前完成）
    /// </summary>
    void Awake()
    {
        // ✅ 在 Awake 中初始化 EnemyStats，确保在 SetEnemyData 调用前完成
        InitializeStatsManager();
    }
    
    void Start()
    {
        // ✅ 所有敌人都通过 SetEnemyData 初始化，Start() 只负责组件查找和事件订阅
        
        // 如果手动配置了AttackRange，就不需要自动查找
        if (attackRange == null)
        {
            Debug.LogWarning($"EnemyBehavior {name}: 请手动配置AttackRange引用！");
        }
        
        // 查找玩家
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 未找到玩家！");
        }
        
        // 订阅伤害处理完成事件 - 应用最终伤害
        GameEventBus.OnDamageProcessed += OnDamageProcessed;
        
        // ✅ 新伤害系统：订阅伤害事件
        GameEventBus.OnDamage += OnDamageReceived;
        
        // ✅ 新伤害系统：注册到 DamageSystem
        if (enemyData != null && enemyData.damageProfile != null)
        {
            DamageSystem.Instance.RegisterEntity(gameObject, enemyData.damageProfile);
            Debug.Log($"[EnemyBehavior] {name} 注册到 DamageSystem，Profile: {enemyData.damageProfile.profileName}");
        }
        else
        {
            Debug.LogWarning($"[EnemyBehavior] {name} 未配置 DamageProfile，无法主动攻击");
        }
        
        Debug.Log($"EnemyBehavior {name}: Start 完成 (订阅伤害事件)");
    }
    
    void OnDestroy()
    {
        // 取消订阅伤害处理完成事件
        GameEventBus.OnDamageProcessed -= OnDamageProcessed;
        
        // ✅ 新伤害系统：取消订阅
        GameEventBus.OnDamage -= OnDamageReceived;
        
        // ✅ 新伤害系统：从 DamageSystem 注销
        if (DamageSystem.HasInstance)
        {
            DamageSystem.Instance.UnregisterEntity(gameObject);
        }
    }
    
    void Update()
    {
        // 临时空实现
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public void ExecuteAttackPhase()
    {
        // 使用攻击行为系统执行攻击
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            attackBehavior.ExecuteAttack(transform, player, enemyData, CurrentLevelConfig, attackRange, attackEffect);
            Debug.Log($"EnemyBehavior {name}: 执行攻击 - 攻击类型: {CurrentLevelConfig.attackType}");
            
            // 注意：清理将在移动阶段开始时执行，避免在攻击特效播放时立即恢复位置
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 攻击行为或攻击范围未设置，无法执行攻击！");
        }
    }
    
    /// <summary>
    /// 对玩家造成伤害
    /// </summary>
    private void DealDamageToPlayer(GameObject player)
    {
        Debug.Log($"EnemyBehavior {name}: DealDamageToPlayer 被调用，enemyData 状态: {(enemyData != null ? $"已设置({enemyData.enemyName})" : "未设置")}");
        if (CurrentLevelConfig == null)
        {
            Debug.LogError($"【攻击范围检测】EnemyBehavior {name}: Level {currentLevel} 配置未找到，无法造成伤害！");
            return;
        }
        
        // 从等级配置读取伤害值
        float damage = CurrentLevelConfig.damage;
        
        // 只发布攻击事件，让 DamageProcessor 统一处理伤害应用
        gameObject.PublishAttack("Hit", transform.position, player, damage);
        
        Debug.Log($"EnemyBehavior {name}: 发布攻击事件，伤害: {damage}");
    }
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public void ExecuteTelegraphPhase()
    {
        // 更新攻击范围
        if (attackArea != null)
        {
            // 显示攻击范围
            attackArea.gameObject.SetActive(true);
        }
        
        // 使用攻击行为系统执行预告
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            attackBehavior.ExecuteTelegraph(transform, player, enemyData, CurrentLevelConfig, attackRange);
            Debug.Log($"EnemyBehavior {name}: 执行攻击预告 - 攻击类型: {CurrentLevelConfig.attackType}");
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 攻击行为或攻击范围未设置");
        }
    }
    
    /// <summary>
    /// 执行移动阶段
    /// </summary>
    public void ExecuteMovePhase()
    {
        Debug.Log($"EnemyBehavior {name}: 执行移动阶段 - 行为类型: {CurrentLevelConfig?.movementType}");
        
        // 在移动阶段开始时清理上一个攻击阶段的状态
        if (attackBehavior != null && attackRange != null)
        {
            attackBehavior.CleanupAttack(transform, attackRange);
            Debug.Log($"EnemyBehavior {name}: 移动阶段开始，清理攻击状态");
        }
        
        Debug.Log($"EnemyBehavior {name}: 移动前位置: {transform.position}");
        
        if (player != null && movementBehavior != null && CurrentLevelConfig != null)
        {
            // 使用行为系统执行移动
            Vector2 targetPosition = movementBehavior.ExecuteMovement(transform, player, enemyData, CurrentLevelConfig);
            currentMovementDirection = movementBehavior.GetMovementDirection();
            
            // 设置移动状态（由 BaseMovementBehavior 管理）
            movementBehavior.SetMoving(true);
            
            // 开始平滑移动
            StartCoroutine(MoveToTarget(targetPosition));
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 无法移动，未找到玩家或行为组件！");
        }
    }
    
    /// <summary>
    /// 平滑移动到目标位置
    /// </summary>
    IEnumerator MoveToTarget(Vector2 targetPosition)
    {
        Vector2 startPosition = transform.position;
        float distance = Vector2.Distance(startPosition, targetPosition);
        
        // 根据行为类型获取对应的移动速度
        float currentMoveSpeed = GetCurrentMoveSpeed();
        float moveTime = distance / currentMoveSpeed;
        
        float elapsedTime = 0f;
        
        while (elapsedTime < moveTime)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / moveTime;
            
            // 使用线性插值平滑移动
            transform.position = Vector2.Lerp(startPosition, targetPosition, progress);
            
            yield return null;
        }
        
        // 确保最终位置准确
        transform.position = targetPosition;
        
        // 重置移动状态（由 BaseMovementBehavior 管理）
        movementBehavior?.SetMoving(false);
        
        Debug.Log($"EnemyBehavior {name}: 移动完成，最终位置: {transform.position}");
    }
    
    /// <summary>
    /// 获取当前移动方向
    /// </summary>
    public Vector2 GetCurrentMovementDirection()
    {
        return movementBehavior?.GetMovementDirection() ?? currentMovementDirection;
    }
    
    /// <summary>
    /// 检查是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return movementBehavior?.IsMoving() ?? false;
    }
    
    /// <summary>
    /// 获取当前移动速度
    /// </summary>
    private float GetCurrentMoveSpeed()
    {
        // 优先使用移动行为提供的速度（支持动态速度变化）
        if (movementBehavior != null)
        {
            return movementBehavior.GetCurrentMoveSpeed();
        }
        
        // 降级：如果移动行为未初始化，使用配置中的默认速度
        if (enemyData == null) return 3f;
        
        switch (CurrentLevelConfig.movementType)
        {
            case MovementType.FollowPlayer:
                return CurrentLevelConfig.followConfig.moveSpeed;
            case MovementType.Flee:
                return CurrentLevelConfig.fleeConfig.moveSpeed;
            default:
                return 3f;
        }
    }
    
    /// <summary>
    /// 设置攻击范围引用
    /// </summary>
    public void SetAttackArea(Transform attackAreaTransform)
    {
        attackArea = attackAreaTransform;
        Debug.Log($"EnemyBehavior {name}: 设置攻击范围引用");
    }
    
    /// <summary>
    /// 设置敌人数据（由 EnemySpawner 调用）
    /// </summary>
    public void SetEnemyData(EnemyData data, int level = 1)
    {
        Debug.Log($"EnemyBehavior {name}: SetEnemyData 被调用，传入数据: {(data != null ? data.enemyName : "null")}, 等级: {level}");
        enemyData = data;
        currentLevel = level;  // ✅ 保存等级
        
        if (enemyData != null)
        {
            if (CurrentLevelConfig == null)
            {
                Debug.LogError($"EnemyBehavior {name}: 未找到 Level {level} 配置！");
                return;
            }
            
            Debug.Log($"EnemyBehavior {name}: 设置敌人数据成功 - {enemyData.enemyName} Lv{level}，移动类型: {CurrentLevelConfig.movementType}");
            // 重新初始化（传递等级参数）
            InitializeHealth(level);
            InitializeBehavior();
            Debug.Log($"EnemyBehavior {name}: 初始化完成，enemyData 状态: {(enemyData != null ? "已设置" : "未设置")}");
        }
        else
        {
            Debug.LogError($"EnemyBehavior {name}: 设置的 EnemyData 为空！");
        }
    }
    
    /// <summary>
    /// 初始化行为系统
    /// </summary>
    private void InitializeBehavior()
    {
        if (CurrentLevelConfig == null)
        {
            Debug.LogError($"EnemyBehavior {name}: Level {currentLevel} 配置未找到，无法初始化行为系统！");
            return;
        }
        
        // 根据配置创建移动行为
        movementBehavior = BehaviorFactory.CreateMovementBehavior(CurrentLevelConfig.movementType);
        Debug.Log($"EnemyBehavior {name}: 初始化移动行为 - 移动类型: {CurrentLevelConfig.movementType}");
        
        // 根据配置创建攻击行为
        attackBehavior = BehaviorFactory.CreateAttackBehavior(CurrentLevelConfig.attackType);
        Debug.Log($"EnemyBehavior {name}: 初始化攻击行为 - 攻击类型: {CurrentLevelConfig.attackType}");
    }
    
    /// <summary>
    /// 初始化属性管理器
    /// </summary>
    private void InitializeStatsManager()
    {
        // 获取或添加 EnemyStats 组件
        statsManager = GetComponent<EnemyStats>();
        if (statsManager == null)
        {
            statsManager = gameObject.AddComponent<EnemyStats>();
            Debug.Log($"EnemyBehavior {name}: ✅ 自动添加 EnemyStats 组件");
        }
    }
    
    /// <summary>
    /// 初始化血量
    /// </summary>
    private void InitializeHealth(int level = 1)
    {
        if (enemyData == null)
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyData 未设置，无法初始化血量！");
            return;
        }
        
        if (statsManager == null)
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyStats 未初始化，无法初始化血量！");
            return;
        }
        
        // ✅ 设置 EnemyData 并初始化属性系统（传递等级参数）
        statsManager.SetEnemyData(enemyData);
        statsManager.Initialize(level);
        
        isDead = false;
        Debug.Log($"EnemyBehavior {name} Lv{level}: 初始化完成，血量 {statsManager.CurrentHealth}/{statsManager.MaxHealth}");
        
        // 初始化血条UI
        if (healthBar != null)
        {
            healthBar.UpdateHealth(statsManager.CurrentHealth, statsManager.MaxHealth);
            Debug.Log($"EnemyBehavior {name}: 血条UI已初始化");
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: HealthBar未设置！");
        }
    }
    
    /// <summary>
    /// 敌人受击处理
    /// </summary>
    private void OnDamageProcessed(ProcessedDamageData processedData)
    {
        Debug.Log($"EnemyBehavior {name}: 接收到伤害处理完成事件 - 目标: {processedData.OriginalData.Target?.name}, 最终伤害: {processedData.FinalDamage}, 攻击者: {processedData.OriginalData.Attacker?.name}");
        
        // 检查自己是否是攻击目标
        if (processedData.OriginalData.Target == gameObject && processedData.FinalDamage > 0f)
        {
            Debug.Log($"EnemyBehavior {name}: 受到 {processedData.FinalDamage} 点伤害！");
            
            // 处理敌人受击逻辑
            TakeDamage(processedData.FinalDamage);
        }
        else
        {
            Debug.Log($"EnemyBehavior {name}: 不是攻击目标，忽略伤害处理完成事件");
        }
    }
    
    /// <summary>
    /// 敌人受到伤害（私有方法，内部使用）
    /// </summary>
    private void TakeDamage(float damage)
    {
        if (statsManager == null)
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyStats 未初始化，无法处理伤害！");
            return;
        }
        
        if (isDead)
        {
            Debug.Log($"EnemyBehavior {name}: 敌人已死亡，无法受到伤害");
            return;
        }
        
        // ✅ 使用 EnemyStats 扣除血量
        statsManager.SubtractHealth(damage);
        
        Debug.Log($"EnemyBehavior {name}: 受到 {damage} 点伤害，当前血量: {statsManager.CurrentHealth}/{statsManager.MaxHealth}");
        
        // 更新血条UI
        if (healthBar != null)
        {
            healthBar.UpdateHealth(statsManager.CurrentHealth, statsManager.MaxHealth);
        }
        
        // 检查是否死亡
        if (statsManager.CurrentHealth <= 0)
        {
            Die();
        }
    }
    
    /// <summary>
    /// 接收伤害（新伤害系统，IDamageable 接口实现）
    /// </summary>
    public void OnDamageReceived(DamageEvent damageEvent)
    {
        // 检查是否是针对自己或自己的子对象的伤害
        bool isTargetSelf = damageEvent.Target == gameObject;
        bool isTargetChild = damageEvent.Target != null && damageEvent.Target.transform.IsChildOf(transform);
        
        if (!isTargetSelf && !isTargetChild)
        {
            return;
        }
        
        // 检查是否可以受伤
        if (!CanTakeDamage())
        {
            return;
        }
        
        // 应用伤害（复用现有逻辑）
        TakeDamage(damageEvent.FinalDamage);
    }
    
    /// <summary>
    /// 是否可以受伤（IDamageable 接口实现）
    /// </summary>
    public bool CanTakeDamage()
    {
        return !isDead && statsManager != null;
    }
    
    /// <summary>
    /// 获取当前血量（IDamageable 接口实现）
    /// </summary>
    public float GetCurrentHealth()
    {
        return statsManager != null ? statsManager.CurrentHealth : 0f;
    }
    
    /// <summary>
    /// 获取最大血量（IDamageable 接口实现）
    /// </summary>
    public float GetMaxHealth()
    {
        return statsManager != null ? statsManager.MaxHealth : 100f;
    }
    
    /// <summary>
    /// 设置陷阱模式（由攻击行为调用）
    /// </summary>
    public void SetTrapMode(bool trapMode)
    {
        isTrapMode = trapMode;
        Debug.Log($"EnemyBehavior {name}: 陷阱模式设置为 {(trapMode ? "开启" : "关闭")}");
    }
    
    /// <summary>
    /// 对玩家造成陷阱伤害（由 PlayerCore 触发碰撞时调用）
    /// 攻击者是敌人，受击者是玩家
    /// </summary>
    public void DealTrapDamageToPlayer(GameObject playerObject, Vector3 hitPosition)
    {
        if (CurrentLevelConfig == null)
        {
            Debug.LogWarning($"EnemyBehavior {name}: Level {currentLevel} 配置未找到，无法造成陷阱伤害");
            return;
        }
        
        float damage = CurrentLevelConfig.damage;
        
        // 只发布攻击事件，让 DamageProcessor 统一处理伤害应用
        gameObject.PublishAttack("Trap", hitPosition, playerObject, damage);
        
        Debug.Log($"EnemyBehavior {name}: 陷阱发布攻击事件（类型：Trap），伤害: {damage}");
    }
    
    /// <summary>
    /// 敌人死亡
    /// </summary>
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        Debug.Log($"EnemyBehavior {name}: 敌人死亡！");
        
        // 【关键修复】通过Enemy组件获取enemyItem位置，避免transform.Find()无法找到inactive对象的问题
        Enemy enemy = GetComponent<Enemy>();
        Transform enemyItem = enemy?.enemyItem;
        Vector3 cachedEnemyItemPosition = enemyItem != null ? enemyItem.position : transform.position;
        
        if (enemyItem != null)
        {
            Debug.Log($"EnemyBehavior {name}: 通过Enemy组件获取enemyItem位置作为死亡位置: {cachedEnemyItemPosition}");
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 未找到enemyItem引用，使用根物体位置: {cachedEnemyItemPosition}");
        }
        
        // 立即清理攻击状态
        if (attackBehavior != null && attackRange != null)
        {
            // 对于远程攻击，如果 AttackRange 已经解除父子关系，需要特殊处理
            if (attackRange.transform.parent == null)
            {
                // AttackRange 是独立的（远程攻击投射状态），直接销毁
                Debug.Log($"EnemyBehavior {name}: 死亡时检测到 AttackRange 独立存在，直接销毁");
                Destroy(attackRange.gameObject);
            }
            else
            {
                // AttackRange 是子物体（近战攻击或已清理的远程攻击），正常清理
                attackBehavior.CleanupAttack(transform, attackRange);
                Debug.Log($"EnemyBehavior {name}: 死亡时清理攻击状态");
            }
        }
        
        // 触发死亡特效
        Debug.Log($"EnemyBehavior {name}: 触发死亡特效事件");
        
        // 使用缓存的位置发布死亡事件
        gameObject.PublishDeath("EnemyDeath", cachedEnemyItemPosition);
        
        /// 禁用collider（解决碰撞问题）
        Collider2D[] colliders = GetComponentsInChildren<Collider2D>();
        foreach (var collider in colliders)
        {
            collider.enabled = false;
        }

        // 禁用敌人行为
        // gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 显示攻击范围并更新位置（使用攻击行为系统）
    /// </summary>
    public void ShowAttackRange()
    {
        // 使用攻击行为系统执行预告
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            attackBehavior.ExecuteTelegraph(transform, player, enemyData, CurrentLevelConfig, attackRange);
            Debug.Log($"EnemyBehavior {name}: ShowAttackRange 调用攻击行为系统");
        }
        else if (attackRange != null)
        {
            // 降级：如果攻击行为未初始化，使用旧逻辑
            attackRange.ShowTelegraph();
            Debug.LogWarning($"EnemyBehavior {name}: ShowAttackRange 使用旧逻辑（攻击行为未初始化）");
        }
    }
    
    /// <summary>
    /// 隐藏攻击范围
    /// </summary>
    public void HideAttackRange()
    {
        if (attackRange != null)
        {
            attackRange.HideTelegraph();
        }
    }
}
