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
        if (attackRange != null)
        {
            // 使用预告阶段保存的朝向
            attackRange.ApplyTelegraphedDirection();
            
            Debug.Log($"EnemyBehavior {name}: 执行一次攻击");
            // 播放攻击特效
            if (attackEffect != null)
            {
                Debug.Log($"【攻击特效】EnemyBehavior {name}: 播放MMF攻击特效");
                attackEffect.PlayFeedbacks();
            }
            else
            {
                Debug.LogWarning($"【攻击特效】EnemyBehavior {name}: attackEffect 为空，无法播放攻击特效");
            }
            
            // 对攻击范围内的目标执行攻击
            var targets = attackRange.GetTargetsInRange();
            
            foreach (var target in targets)
            {
                // 对玩家造成伤害
                if (target.CompareTag("Player"))
                {
                    DealDamageToPlayer(target);
                }
            }
        }
        else
        {
            Debug.LogWarning($"【攻击范围检测】EnemyBehavior {name}: AttackRange 未设置，无法执行攻击！");
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
        
        if (attackRange != null)
        {
            // 更新攻击范围的方向和位置
            attackRange.ShowTelegraph();
        }
    }
    
    /// <summary>
    /// 执行移动阶段
    /// </summary>
    public void ExecuteMovePhase()
    {
        Debug.Log($"EnemyBehavior {name}: 执行移动阶段 - 行为类型: {enemyData?.movementType}");
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
        if (enemyData == null) return 3f; // 默认速度
        
        switch (enemyData.movementType)
        {
            case MovementType.FollowPlayer:
                return enemyData.followConfig.moveSpeed;
            case MovementType.Flee:
                return enemyData.fleeConfig.moveSpeed;
            default:
                return 3f; // 默认速度
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
            Debug.Log($"EnemyBehavior {name}: 初始化行为系统 - 移动类型: {enemyData.movementType}");
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
        
        // 检查是否死亡
        if (currentHealth <= 0)
        {
            Die();
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
        
        // 触发死亡特效
        Debug.Log($"EnemyBehavior {name}: 触发死亡特效事件");
        gameObject.PublishDeath("EnemyDeath", transform.position);
        
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
    /// 显示攻击范围并更新位置
    /// </summary>
    public void ShowAttackRange()
    {
        if (attackRange != null)
        {
            attackRange.ShowTelegraph();
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
