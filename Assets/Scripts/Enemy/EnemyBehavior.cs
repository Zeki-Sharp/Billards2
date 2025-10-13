using UnityEngine;
using System.Collections;
using MoreMountains.Feedbacks;

/// <summary>
/// 敌人行为脚本 - 纯行为逻辑
/// </summary>
public class EnemyBehavior : MonoBehaviour
{
    [Header("数据设置")]
    [Tooltip("敌人数据配置。手动放置的敌人需要在此配置，通过 EnemySpawner 生成的敌人会自动设置")]
    public EnemyData enemyData;
    
    [Header("移动设置")]
    private bool isMoving = false;  // 是否正在移动
    
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
    
    [Header("血量管理")]
    private float currentHealth;
    private bool isDead = false;
    
    [Header("UI组件")]
    public HealthBar healthBar;  // 血条UI引用
    
    [Header("陷阱系统")]
    private bool isTrapMode = false;   // 是否处于陷阱模式（玩家碰撞时触发陷阱伤害而非攻击敌人）
    
    /// <summary>
    /// 是否处于陷阱模式（公开属性，供 PlayerCore 检查）
    /// </summary>
    public bool IsTrapMode => isTrapMode;
    
    void Start()
    {
        // 如果 enemyData 已经设置（手动放置的敌人），直接初始化
        if (enemyData != null)
        {
            Debug.Log($"EnemyBehavior {name}: 检测到手动配置的 EnemyData，直接初始化");
            InitializeHealth();
            InitializeBehavior();
        }
        else
        {
            Debug.Log($"EnemyBehavior {name}: 等待 SetEnemyData 调用（通过 EnemySpawner 生成）");
        }
        
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
        
        // 订阅攻击事件
        GameEventBus.OnAttack += OnEnemyAttacked;
        
        Debug.Log($"EnemyBehavior {name}: Start 完成");
    }
    
    void OnDestroy()
    {
        // 取消订阅攻击事件
        GameEventBus.OnAttack -= OnEnemyAttacked;
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
        if (attackBehavior != null && attackRange != null)
        {
            attackBehavior.ExecuteAttack(transform, player, enemyData, attackRange, attackEffect);
            Debug.Log($"EnemyBehavior {name}: 执行攻击 - 攻击类型: {enemyData.attackType}");
            
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
        if (enemyData == null)
        {
            Debug.LogError($"【攻击范围检测】EnemyBehavior {name}: EnemyData 未设置，无法造成伤害！");
            return;
        }
        
        // 在玩家及其子物体中查找 PlayerCore 组件
        PlayerCore playerCore = player.GetComponentInChildren<PlayerCore>();
        if (playerCore != null)
        {
            // 从 EnemyData 读取伤害值
            float damage = enemyData.damage;
            
            // 对玩家造成伤害
            playerCore.TakeDamage(damage);

            gameObject.PublishAttack("Hit", transform.position, player, damage);
        }
        else
        {
            Debug.LogWarning($"【攻击范围检测】EnemyBehavior {name}: 玩家及其子物体中没有找到 PlayerCore 组件，无法造成伤害！");
        }
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
        if (attackBehavior != null && attackRange != null)
        {
            attackBehavior.ExecuteTelegraph(transform, player, enemyData, attackRange);
            Debug.Log($"EnemyBehavior {name}: 执行攻击预告 - 攻击类型: {enemyData.attackType}");
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
        Debug.Log($"EnemyBehavior {name}: 执行移动阶段 - 行为类型: {enemyData?.movementType}");
        
        // 在移动阶段开始时清理上一个攻击阶段的状态
        if (attackBehavior != null && attackRange != null)
        {
            attackBehavior.CleanupAttack(transform, attackRange);
            Debug.Log($"EnemyBehavior {name}: 移动阶段开始，清理攻击状态");
        }
        
        Debug.Log($"EnemyBehavior {name}: 移动前位置: {transform.position}");
        
        if (player != null && movementBehavior != null)
        {
            // 使用行为系统执行移动
            Vector2 targetPosition = movementBehavior.ExecuteMovement(transform, player, enemyData);
            currentMovementDirection = movementBehavior.GetMovementDirection();
            
            // 设置移动状态
            isMoving = true;
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
        
        // 重置移动状态
        isMoving = false;
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
        return movementBehavior?.IsMoving() ?? isMoving;
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
        
        switch (enemyData.movementType)
        {
            case MovementType.FollowPlayer:
                return enemyData.followConfig.moveSpeed;
            case MovementType.Flee:
                return enemyData.fleeConfig.moveSpeed;
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
    public void SetEnemyData(EnemyData data)
    {
        Debug.Log($"EnemyBehavior {name}: SetEnemyData 被调用，传入数据: {(data != null ? data.enemyName : "null")}");
        enemyData = data;
        
        if (enemyData != null)
        {
            Debug.Log($"EnemyBehavior {name}: 设置敌人数据成功 - {enemyData.enemyName}，移动类型: {enemyData.movementType}");
            // 重新初始化
            InitializeHealth();
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
        if (enemyData != null)
        {
            // 根据配置创建移动行为
            movementBehavior = BehaviorFactory.CreateMovementBehavior(enemyData.movementType);
            Debug.Log($"EnemyBehavior {name}: 初始化移动行为 - 移动类型: {enemyData.movementType}");
            
            // 根据配置创建攻击行为
            attackBehavior = BehaviorFactory.CreateAttackBehavior(enemyData.attackType);
            Debug.Log($"EnemyBehavior {name}: 初始化攻击行为 - 攻击类型: {enemyData.attackType}");
        }
        else
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyData 未设置，无法初始化行为系统！");
        }
    }
    
    /// <summary>
    /// 初始化血量
    /// </summary>
    private void InitializeHealth()
    {
        if (enemyData != null)
        {
            currentHealth = enemyData.maxHealth;
            isDead = false;
            Debug.Log($"EnemyBehavior {name}: 初始化血量 {currentHealth}/{enemyData.maxHealth}");
            
            // 初始化血条UI
            if (healthBar != null)
            {
                //healthBar.SetTarget(transform);
                healthBar.UpdateHealth(currentHealth, enemyData.maxHealth);  // 初始化血量显示
                Debug.Log($"EnemyBehavior {name}: 血条UI已初始化");
            }
            else
            {
                Debug.LogWarning($"EnemyBehavior {name}: HealthBar未设置！");
            }
        }
        else
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyData 未设置，无法初始化血量！");
        }
    }
    
    /// <summary>
    /// 敌人受击处理
    /// </summary>
    private void OnEnemyAttacked(AttackData attackData)
    {
        Debug.Log($"EnemyBehavior {name}: 接收到攻击事件 - 目标: {attackData.Target?.name}, 伤害: {attackData.Damage}, 攻击者: {attackData.Attacker?.name}");
        
        // 检查自己是否是攻击目标
        if (attackData.Target == gameObject && attackData.Damage > 0f)
        {
            Debug.Log($"EnemyBehavior {name}: 受到 {attackData.Damage} 点伤害！");
            
            // 处理敌人受击逻辑
            TakeDamage(attackData.Damage);
        }
        else
        {
            Debug.Log($"EnemyBehavior {name}: 不是攻击目标，忽略攻击事件");
        }
    }
    
    /// <summary>
    /// 敌人受到伤害
    /// </summary>
    private void TakeDamage(float damage)
    {
        if (enemyData == null)
        {
            Debug.LogError($"EnemyBehavior {name}: EnemyData 未设置，无法处理伤害！");
            return;
        }
        
        if (isDead)
        {
            Debug.Log($"EnemyBehavior {name}: 敌人已死亡，无法受到伤害");
            return;
        }
        
        // 扣除血量
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);
        
        Debug.Log($"EnemyBehavior {name}: 受到 {damage} 点伤害，当前血量: {currentHealth}/{enemyData.maxHealth}");
        
        // 更新血条UI
        if (healthBar != null)
        {
            healthBar.UpdateHealth(currentHealth, enemyData.maxHealth);
        }
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
        }
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
        if (enemyData == null)
        {
            Debug.LogWarning($"EnemyBehavior {name}: enemyData 为空，无法造成陷阱伤害");
            return;
        }
        
        // 查找 PlayerCore 组件
        PlayerCore playerCore = playerObject.GetComponent<PlayerCore>();
        if (playerCore == null)
        {
            playerCore = playerObject.GetComponentInChildren<PlayerCore>();
        }
        
        if (playerCore != null)
        {
            float damage = enemyData.damage;
            
            // 对玩家造成伤害（忽略阶段）
            playerCore.TakeDamageIgnorePhase(damage);
            
            // 发布攻击事件（触发受击特效）
            gameObject.PublishAttack("Hit", hitPosition, playerObject, damage);
            
            Debug.Log($"EnemyBehavior {name}: 陷阱对玩家造成 {damage} 点伤害");
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 玩家对象中未找到 PlayerCore 组件");
        }
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
        if (attackBehavior != null && attackRange != null)
        {
            attackBehavior.ExecuteTelegraph(transform, player, enemyData, attackRange);
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
