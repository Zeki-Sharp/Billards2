using UnityEngine;
using System.Collections;
using System.Linq;
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
    [Tooltip("敌人的几何物理组件（如果未手动指定，将在 Awake 中自动查找子节点中的 BallPhysics）")]
    [SerializeField] private BallPhysics ballPhysics;
    private Transform player;
    private Vector2 currentMovementDirection = Vector2.zero;
    
    [Header("攻击范围管理")]
    private Transform attackArea;  // 攻击范围预制体引用
    
    [Header("属性管理")]
    private EnemyStats statsManager;  // ✅ 三层属性系统管理器
    
    [Header("血量管理")]
    private bool isDead = false;
    
    // ✅ 多角色系统：缓存最后的攻击者（用于死亡事件）
    private GameObject lastAttacker;
    
    [Header("UI组件")]
    public HealthBar healthBar;  // 血条UI引用
    
    [Header("陷阱系统")]
    private bool isTrapMode = false;   // 是否处于陷阱模式（玩家碰撞时触发陷阱伤害而非攻击敌人）
    
    [SerializeField] private bool showDebugInfo = false;
    
    /// <summary>
    /// 是否处于陷阱模式（公开属性，供 PlayerCore 检查）
    /// </summary>
    public bool IsTrapMode => isTrapMode;
    
    [Header("运行时状态")]
    private EnemyRuntimeState runtimeState = new EnemyRuntimeState(); // 运行时状态数据
    
    /// <summary>
    /// Awake - 初始化组件引用（确保在 SetEnemyData 调用前完成）
    /// </summary>
    void Awake()
    {
        // ✅ 在 Awake 中初始化 EnemyStats，确保在 SetEnemyData 调用前完成
        InitializeStatsManager();
        
        // ✅ 初始化 BallPhysics 引用（用于主动移动和击退几何模拟）
        if (ballPhysics == null)
        {
            ballPhysics = GetComponentInChildren<BallPhysics>();
            if (ballPhysics == null)
            {
                Debug.LogWarning($"EnemyBehavior {name}: 未找到 BallPhysics 组件，敌人将无法使用几何物理移动与碰撞");
            }
        }
    }
    
    void Start()
    {
        // ✅ 所有敌人都通过 SetEnemyData 初始化，Start() 只负责组件查找和事件订阅
        
        // 如果手动配置了AttackRange，就不需要自动查找
        if (attackRange == null)
        {
            Debug.LogWarning($"EnemyBehavior {name}: 请手动配置AttackRange引用！");
        }
        
        // ✅ 多角色系统改造：玩家目标在每个阶段执行前动态查找（FindNearestPlayer）
        // 不再在 Start() 中静态查找玩家，因为：
        // 1. 多角色系统中有多个玩家，需要选择最近的
        // 2. 玩家可能移动或死亡，需要实时更新目标
        // 3. 每个行动阶段（预告/攻击/移动）都会调用 FindNearestPlayer()
        
        // ✅ 新伤害系统：订阅伤害事件
        GameEventBus.OnDamage += OnDamageReceived;
        
        // ✅ 新伤害系统：注册到 DamageSystem
        if (enemyData != null)
        {
            var profiles = enemyData.GetDamageProfiles();
            if (profiles != null && profiles.Count > 0)
            {
                DamageSystem.Instance.RegisterEntity(gameObject, profiles);
                if (showDebugInfo)
                {
                    string profileNames = string.Join(", ", profiles.Select(p => p != null ? p.profileName : "NULL"));
                    Debug.Log($"[EnemyBehavior] {name} 注册到 DamageSystem，Profile 数量: {profiles.Count}, 列表: [{profileNames}]");
                }
            }
            else
            {
                Debug.LogWarning($"[EnemyBehavior] {name} 未配置任何 DamageProfile，无法主动攻击");
            }
        }
        
        
        Debug.Log($"EnemyBehavior {name}: Start 完成 (订阅伤害事件)");
    }
    
    void OnDestroy()
    {
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
    /// ✅ 多角色系统改造：查找最近的存活玩家作为目标
    /// </summary>
    /// <returns>最近的存活玩家 Transform，如果没有存活玩家则返回 null</returns>
    private Transform FindNearestPlayer()
    {
        // 从 GameSession 获取队伍数据
        var teamData = GameSession.Instance?.GetTeamData();
        if (teamData == null)
        {
            Debug.LogError($"[EnemyBehavior] {name}: GameSession.TeamData 为空，无法查找玩家！");
            return null;
        }
        
        if (teamData.characters == null || teamData.characters.Count == 0)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: TeamData.characters 为空，无法查找玩家！");
            return null;
        }
        
        Transform nearestPlayer = null;
        float nearestDistance = float.MaxValue;
        
        // 遍历所有角色，查找最近的存活玩家
        foreach (var character in teamData.characters)
        {
            // 跳过已死亡的角色
            if (!character.isAlive)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[EnemyBehavior] {name}: 角色 {character.characterID} 已死亡，跳过");
                }
                continue;
            }
            
            // 检查角色的游戏对象引用
            if (character.ballInstance == null)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"[EnemyBehavior] {name}: 角色 {character.characterID} 的 ballInstance 为 null");
                }
                continue;
            }
            
            // 计算距离
            float distance = Vector3.Distance(transform.position, character.ballInstance.transform.position);
            if (showDebugInfo)
            {
                Debug.Log($"[EnemyBehavior] {name}: 角色 {character.characterID} 距离 {distance:F2}");
            }
            
            // 更新最近玩家
            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestPlayer = character.ballInstance.transform;
            }
        }
        
        // 输出查找结果（简洁）
        if (nearestPlayer == null)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: 没有找到存活的玩家");
        }
        else if (showDebugInfo)
        {
            Debug.Log($"[EnemyBehavior] {name}: 最近玩家 {nearestPlayer.name} 距离 {nearestDistance:F2}");
        }
        
        return nearestPlayer;
    }
    
    /// <summary>
    /// 执行攻击阶段
    /// </summary>
    public void ExecuteAttackPhase()
    {
        // ✅ 多角色系统改造：每个阶段前重新查找最近的存活玩家
        player = FindNearestPlayer();
        
        // ✅ 容错处理：如果没有存活玩家，跳过攻击阶段
        if (player == null)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: 攻击阶段 - 找不到存活的玩家，跳过攻击");
            return;
        }
        
        // 使用攻击行为系统执行攻击
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            BehaviorStatus status = attackBehavior.ExecuteAttack(transform, player, enemyData, CurrentLevelConfig, attackRange, attackEffect, runtimeState);
            
            if (status == BehaviorStatus.Success)
            {
                Debug.Log($"EnemyBehavior {name}: 执行攻击成功 - 攻击类型: {CurrentLevelConfig.attackType}");
            }
            else
            {
                Debug.LogWarning($"EnemyBehavior {name}: 执行攻击失败 - 状态: {status}");
            }
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 攻击行为或攻击范围未设置，无法执行攻击！");
        }
    }
    
    
    /// <summary>
    /// 执行预告阶段
    /// </summary>
    public void ExecuteTelegraphPhase()
    {
        // ✅ 多角色系统改造：每个阶段前重新查找最近的存活玩家
        player = FindNearestPlayer();
        
        // ✅ 容错处理：如果没有存活玩家，跳过预告阶段
        if (player == null)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: 预告阶段 - 找不到存活的玩家，跳过预告");
            return;
        }
        
        // 使用攻击行为系统执行预告
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            BehaviorStatus status = attackBehavior.ExecuteTelegraph(transform, player, enemyData, CurrentLevelConfig, attackRange, runtimeState);
            
            if (status == BehaviorStatus.Success)
            {
                Debug.Log($"EnemyBehavior {name}: 执行攻击预告成功 - 攻击类型: {CurrentLevelConfig.attackType}");
            }
            else
            {
                Debug.LogWarning($"EnemyBehavior {name}: 执行攻击预告失败 - 状态: {status}");
            }
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
        // 在移动阶段开始时清理上一个攻击阶段的状态
        if (attackBehavior != null && attackRange != null)
        {
            BehaviorStatus cleanupStatus = attackBehavior.CleanupAttack(transform, attackRange, runtimeState);
            
            if (cleanupStatus == BehaviorStatus.Success)
            {
                Debug.Log($"EnemyBehavior {name}: 清理攻击状态成功");
            }
        }
        
        // ✅ 多角色系统改造：每个阶段前重新查找最近的存活玩家
        player = FindNearestPlayer();
        
        // ✅ 容错处理：如果没有存活玩家，跳过移动阶段
        if (player == null)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: 移动阶段 - 找不到存活的玩家，跳过移动");
            return;
        }
        
        if (movementBehavior != null && CurrentLevelConfig != null)
        {
            // 使用行为系统执行移动
            BehaviorStatus status = movementBehavior.ExecuteMovement(transform, player, enemyData, CurrentLevelConfig, runtimeState, out Vector2 targetPosition);
            
            if (status == BehaviorStatus.Success || status == BehaviorStatus.Running)
            {
                // 从 RuntimeState 读取移动方向
                currentMovementDirection = runtimeState.currentDirection;
                
                // ✅ 调试：输出移动目标位置
                if (showDebugInfo)
                {
                    Vector3 currentPos = transform.position;
                    Vector3 target3D = new Vector3(targetPosition.x, currentPos.y, targetPosition.y);
                    Debug.Log($"EnemyBehavior {name}: 移动目标 - 2D坐标({targetPosition.x:F2}, {targetPosition.y:F2}) -> 3D坐标({target3D.x:F2}, {target3D.y:F2}, {target3D.z:F2}), 当前位置({currentPos.x:F2}, {currentPos.y:F2}, {currentPos.z:F2})");
                }
                
                // 开始平滑移动
                StartCoroutine(MoveToTarget(targetPosition));
            }
            else
            {
                Debug.LogWarning($"EnemyBehavior {name}: 移动行为执行失败 - 状态: {status}");
            }
        }
        else
        {
            Debug.LogWarning($"EnemyBehavior {name}: 移动行为组件未设置，无法执行移动！");
        }

        NotifyEnemyStoppedIfNeeded();
    }
    
    /// <summary>
    /// ✅ 使用几何物理系统平滑移动到目标位置（3D XZ 平面移动，保持Y坐标不变）
    /// 敌人主动移动也视为一次「发射」，由 BallPhysics 负责位移与碰撞
    /// </summary>
    IEnumerator MoveToTarget(Vector2 targetPosition)
    {
        // 如果没有几何物理组件，回退到旧的 Lerp 逻辑（保证兼容性）
        if (ballPhysics == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"EnemyBehavior {name}: BallPhysics 为空，MoveToTarget 回退到直接 Lerp 位移");
            }
            
            Vector3 startPos = transform.position;
            Vector3 targetPos = new Vector3(targetPosition.x, startPos.y, targetPosition.y);
            
            // ✅ 移动前先旋转朝向目标位置
            RotateTowardsPosition(targetPos);
            
            float distance = Vector3.Distance(startPos, targetPos);
            float speed = GetCurrentMoveSpeed();
            float moveTime = distance > 0f && speed > 0f ? distance / speed : 0f;
            
            runtimeState.isMoving = true;
            
            float elapsed = 0f;
            while (elapsed < moveTime)
            {
                elapsed += Time.deltaTime;
                float t = moveTime > 0f ? Mathf.Clamp01(elapsed / moveTime) : 1f;
                transform.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
            
            transform.position = targetPos;
            runtimeState.isMoving = false;
            NotifyEnemyStoppedIfNeeded();
            yield break;
        }
        
        // ✅ 将 2D 逻辑坐标转换为 3D 世界坐标（XZ 平面）
        Vector3 startPosition3D = transform.position;
        Vector3 targetPosition3D = new Vector3(targetPosition.x, startPosition3D.y, targetPosition.y);
        
        float distanceToTarget = Vector3.Distance(startPosition3D, targetPosition3D);
        if (distanceToTarget <= 0.01f)
        {
            runtimeState.isMoving = false;
            NotifyEnemyStoppedIfNeeded();
            yield break;
        }
        
        // ✅ 移动前先旋转朝向目标位置
        RotateTowardsPosition(targetPosition3D);
        
        // 标记开始移动（即使距离为0，也要标记，以便 EnemyManager 正确等待）
        runtimeState.isMoving = true;
        
        // 使用主动移动配置（近似匀速）
        ballPhysics.UseActiveMoveGeometryConfig();
        
        float moveSpeed = GetCurrentMoveSpeed();
        Vector3 moveDir = (targetPosition3D - startPosition3D);
        moveDir.y = 0f;
        moveDir.Normalize();
        
        // 记录当前移动方向，用于行为树查询
        runtimeState.currentDirection = new Vector2(moveDir.x, moveDir.z);
        
        // 将敌人视为一次「发射」——设置几何初速度
        ballPhysics.ApplyExternalGeometryVelocity(moveDir * moveSpeed);
        
        // 允许的到达误差 & 超时保护
        const float arriveThreshold = 0.05f;
        float maxMoveTime = distanceToTarget / Mathf.Max(0.1f, moveSpeed) * 1.5f; // 最多走两倍预估时间
        float elapsedTime = 0f;
        
        while (true)
        {
            // 如果几何物理已经判定停止（速度低于阈值），结束移动
            if (!ballPhysics.IsMoving())
            {
                if (showDebugInfo)
                {
                    Debug.Log($"EnemyBehavior {name}: BallPhysics 停止移动，结束 MoveToTarget");
                }
                break;
            }
            
            // 距离足够近，认为到达目标
            float currentDistance = Vector3.Distance(transform.position, targetPosition3D);
            if (currentDistance <= arriveThreshold)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"EnemyBehavior {name}: 接近目标点（{currentDistance:F3}），结束 MoveToTarget");
                }
                break;
            }
            
            // 超时保护，防止由于反弹等原因长时间不结束
            elapsedTime += Time.deltaTime;
            if (elapsedTime >= maxMoveTime)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"EnemyBehavior {name}: MoveToTarget 超时（{elapsedTime:F2}s），强制结束");
                }
                break;
            }
            
            yield return null;
        }
        
        // 停止几何移动，并切回击退配置
        ballPhysics.ApplyExternalGeometryVelocity(Vector3.zero);
        ballPhysics.UseKnockbackGeometryConfig();
        
        runtimeState.isMoving = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"EnemyBehavior {name}: MoveToTarget 结束，最终位置: {transform.position}");
        }

        NotifyEnemyStoppedIfNeeded();
    }
    
    /// <summary>
    /// 旋转朝向指定位置（XZ平面，只绕Y轴旋转）
    /// </summary>
    /// <param name="targetPosition">目标位置（世界坐标）</param>
    private void RotateTowardsPosition(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f; // 只考虑XZ平面
        
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);
            // 确保只绕Y轴旋转
            float yAngle = targetRotation.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0f, yAngle, 0f);
            
            if (showDebugInfo)
            {
                Debug.Log($"EnemyBehavior {name}: 旋转朝向目标 - 目标位置:{targetPosition}, 方向:{direction.normalized}, Y轴角度:{yAngle:F2}°");
            }
        }
    }
    
    /// <summary>
    /// 获取当前移动方向
    /// </summary>
    public Vector2 GetCurrentMovementDirection()
    {
        return runtimeState.currentDirection;
    }
    
    /// <summary>
    /// 检查是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return runtimeState.isMoving;
    }

    /// <summary>
    /// 在敌人停止时通知 GameEventBus（供回合同步使用）
    /// </summary>
    void NotifyEnemyStoppedIfNeeded()
    {
        if (runtimeState.isMoving)
        {
            return;
        }

        BallPhysics physics = GetComponentInChildren<BallPhysics>();
        if (physics != null)
        {
            GameEventBus.PublishBallStopped(physics);
        }
    }
    
    /// <summary>
    /// 获取当前移动速度
    /// 从 runtimeState 中的阶段配置读取，如果没有则使用默认值
    /// </summary>
    private float GetCurrentMoveSpeed()
    {
        if (CurrentLevelConfig == null) return 3f;
        
        // 优先从 runtimeState 中的阶段配置读取
        if (runtimeState.currentMoveTowardsConfig != null)
            return runtimeState.currentMoveTowardsConfig.moveSpeed;
        if (runtimeState.currentMoveAwayConfig != null)
            return runtimeState.currentMoveAwayConfig.moveSpeed;
        
        // 默认速度
        return 3f;
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
        Debug.Log($"EnemyBehavior {name}: SetEnemyData 被调用，传入数据: {(data != null ? data.info.name : "null")}, 等级: {level}");
        enemyData = data;
        currentLevel = level;  // ✅ 保存等级
        
        if (enemyData != null)
        {
            if (CurrentLevelConfig == null)
            {
                Debug.LogError($"EnemyBehavior {name}: 未找到 Level {level} 配置！");
                return;
            }
            
            Debug.Log($"EnemyBehavior {name}: 设置敌人数据成功 - {enemyData.info.name} Lv{level}");
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
        
        // 创建移动行为 - 使用统一的 PhaseSequence 系统
        if (CurrentLevelConfig.phaseSequenceConfig != null && 
            CurrentLevelConfig.phaseSequenceConfig.phases != null && 
            CurrentLevelConfig.phaseSequenceConfig.phases.Length > 0)
        {
            movementBehavior = new PhaseSequenceMovementBehavior();
            Debug.Log($"EnemyBehavior {name}: 初始化移动行为 - PhaseSequence 系统");
        }
        else
        {
            Debug.LogError($"EnemyBehavior {name}: 未配置 PhaseSequenceConfig，敌人将无法移动！");
        }
        
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
            return;
        }
        
        // ✅ 使用 EnemyStats 扣除血量
        statsManager.SubtractHealth(damage);
        
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
        
        // ✅ 缓存攻击者（用于死亡事件）
        lastAttacker = damageEvent.Source;
        
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
    
    #region 死亡处理
    
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
                attackBehavior.CleanupAttack(transform, attackRange, runtimeState);
                Debug.Log($"EnemyBehavior {name}: 死亡时清理攻击状态");
            }
        }
        
        // 触发死亡特效
        Debug.Log($"EnemyBehavior {name}: 触发死亡特效事件");
        
        // ✅ 获取击杀者角色ID（用于击杀技能）
        string attackerCharacterID = null;
        if (lastAttacker != null)
        {
            attackerCharacterID = TriggerHelper.GetCharacterID(lastAttacker);
        }
        
        // 使用缓存的位置发布死亡事件（包含击杀者信息）
        DeathData deathData = new DeathData
        {
            DeathType = "EnemyDeath",
            Position = cachedEnemyItemPosition,
            Direction = Vector3.zero,
            DeadObject = gameObject,
            DeadObjectTag = gameObject.tag,
            DeathTime = Time.time,
            Attacker = lastAttacker,
            AttackerCharacterID = attackerCharacterID,
            target = gameObject,
            enemyType = EnemyType.Normal // 可根据需要扩展
        };
        GameEventBus.PublishDeath(deathData);
        
        Debug.Log($"EnemyBehavior {name}: 发布死亡事件，击杀者：{attackerCharacterID ?? "无"}");

        // 通知敌人管理器移除该敌人
        if (enemy != null)
        {
            EnemyManager.Instance?.UnregisterEnemy(enemy);
        }
        
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
        // ✅ 多角色系统改造：查找最近的存活玩家
        player = FindNearestPlayer();
        
        // ✅ 容错处理：如果没有存活玩家，跳过
        if (player == null)
        {
            Debug.LogWarning($"[EnemyBehavior] {name}: ShowAttackRange - 找不到存活的玩家，跳过攻击范围显示");
            return;
        }
        
        // 使用攻击行为系统执行预告
        if (attackBehavior != null && attackRange != null && CurrentLevelConfig != null)
        {
            attackBehavior.ExecuteTelegraph(transform, player, enemyData, CurrentLevelConfig, attackRange, runtimeState);
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
        attackBehavior?.CleanupAttack(transform, attackRange, runtimeState);
    }
    
    #endregion
}
