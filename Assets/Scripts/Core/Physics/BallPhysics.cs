using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("物理数据")]
    public BallData ballData;
    
    [Header("模拟设置")]
    [Tooltip("是否为影子场景模拟模式（禁用Update和事件发布）")]
    public bool isSimulationMode = false;
    
    private Rigidbody2D rb;
    private Collider2D ballCollider;  // 改为通用Collider2D，支持Circle/Box/Polygon等
    private PhysicsMaterial2D material;
    private bool isInitialized = false;
    
    // 动态物理参数缓存
    private float lastBounciness = -1f;
    private float lastDamping = -1f;
    private float lastUpdateTime = 0f;
    
    // 时间阻尼相关变量
    private float ballStartTime = 0f;
    private bool isMoving = false;
    
    // 模拟模式专用时间追踪
    private float simulationLastUpdateTime = 0f;
    
    // 反弹方向检测
    private Vector2 lastReflectionDirection = Vector2.zero;
    
    // 事件（已移除组件级事件，统一使用GameEventBus）
    
    void Start()
    {
        InitializePhysics();
    }
    
    void Update()
    {
        // 物理相关逻辑已移至FixedUpdate，与物理引擎同步
    }
    
    void FixedUpdate()
    {
        // 模拟模式下不执行（由手动调用控制）
        // 所有物理相关逻辑在FixedUpdate中执行，与物理引擎完全同步
        if (isInitialized && !isSimulationMode)
        {
            CheckMovement();
            UpdateDynamicPhysics();
        }
    }
    
    /// <summary>
    /// 初始化物理组件（公共方法，供影子场景手动调用）
    /// </summary>
    public void InitializePhysics()
    {
        if (ballData == null)
        {
            Debug.LogError($"BallPhysics on {gameObject.name}: BallData is null!");
            return;
        }
        
        // 设置刚体
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
        }
        
        rb.mass = ballData.mass;
        rb.gravityScale = 0f;
        rb.linearDamping = ballData.linearDamping;
        rb.angularDamping = 0f; // 不需要角阻尼，因为禁用了旋转
        rb.freezeRotation = true;
        
        // 设置碰撞器（支持任意Collider2D类型）
        ballCollider = GetComponent<Collider2D>();
        if (ballCollider == null)
        {
            // 如果没有任何碰撞器，默认添加CircleCollider2D
            ballCollider = gameObject.AddComponent<CircleCollider2D>();
            Debug.Log($"BallPhysics: {gameObject.name} 没有碰撞器，已添加 CircleCollider2D");
        }
        else
        {
            Debug.Log($"BallPhysics: {gameObject.name} 检测到碰撞器类型: {ballCollider.GetType().Name}");
        }
        
        // 设置碰撞器属性
        if (ballCollider != null)
        {
            ballCollider.isTrigger = false;
        }
        
        // 创建并应用物理材质（支持所有Collider2D类型）
        if (ballCollider != null)
        {
            material = new PhysicsMaterial2D("BallMaterial");
            material.bounciness = ballData.bounceDamping; // 使用BallData中的反弹系数
            material.friction = ballData.friction; // 使用BallData中的摩擦系数
            ballCollider.sharedMaterial = material;
        }
        
        // 初始化动态参数缓存
        lastBounciness = ballData.bounceDamping;
        lastDamping = ballData.linearDamping;
        
        isInitialized = true;
        Debug.Log($"BallPhysics initialized for {gameObject.name}");
    }
    
    void CheckMovement()
    {
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // 确保球不会旋转
        if (rb.angularVelocity != 0f)
        {
            rb.angularVelocity = 0f;
        }
        
        // 记录运动状态
        if (currentSpeed > ballData.stopThreshold)
        {
            // 球在运动，记录开始运动时间
            if (!isMoving)
            {
                isMoving = true;
                ballStartTime = Time.fixedTime;
                Debug.Log($"BallPhysics: 球开始运动，记录时间 {ballStartTime:F2}");
                // 发布到GameEventBus（模拟模式下不发布）
                if (!isSimulationMode)
                {
                    GameEventBus.PublishBallStarted(this);
                }
            }
        }
        else
        {
            // 球停止运动 - 只在状态变化时发布事件
            if (isMoving)
            {
                isMoving = false;
                float movementDuration = Time.fixedTime - ballStartTime;
                Debug.Log($"BallPhysics: 球停止运动，运动时长 {movementDuration:F2} 秒");
                
                // 发布球停止事件（模拟模式下不发布）
                if (!isSimulationMode)
                {
                    GameEventBus.PublishBallStopped(this);
                }
            }
            
            // 如果速度低于停止阈值，强制停止
            if (currentSpeed <= ballData.stopThreshold && currentSpeed > 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
            else if (currentSpeed <= 0.01f)
            {
                // 速度极低时也认为已停止
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
            }
        }
        
        // 限制最大速度
        if (currentSpeed > ballData.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * ballData.maxSpeed;
        }
    }
    
    /// <summary>
    /// 计算动态物理参数（纯函数，无副作用）
    /// </summary>
    /// <param name="currentTime">当前时间</param>
    /// <param name="currentSpeed">当前速度</param>
    /// <returns>计算得到的弹性系数和阻尼值</returns>
    private (float bounciness, float damping) CalculateDynamicPhysics(float currentTime, float currentSpeed)
    {
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / ballData.maxSpeed);
        
        // 计算动态弹性系数
        float bounciness = ballData.speedToBounciness.Evaluate(normalizedSpeed);
        bounciness = Mathf.Lerp(ballData.minBounciness, ballData.maxBounciness, bounciness);
        
        // 计算动态阻尼
        float damping = ballData.speedToDamping.Evaluate(normalizedSpeed);
        damping = Mathf.Lerp(ballData.minDamping, ballData.maxDamping, damping);
        
        // 添加时间阻尼
        if (ballData.enableTimeDamping && isMoving)
        {
            float timeSinceStart = currentTime - ballStartTime;
            if (timeSinceStart > ballData.timeDampingStartTime)
            {
                float timeDamping = Mathf.Min(
                    ballData.timeDampingRate * (timeSinceStart - ballData.timeDampingStartTime),
                    ballData.maxTimeDamping
                );
                damping += timeDamping;
            }
        }
        
        return (bounciness, damping);
    }
    
    /// <summary>
    /// 应用动态物理参数到物理组件
    /// </summary>
    /// <param name="targetBounciness">目标弹性系数</param>
    /// <param name="targetDamping">目标阻尼值</param>
    private void ApplyDynamicPhysics(float targetBounciness, float targetDamping)
    {
        // 检查参数变化是否超过阈值
        bool bouncinessChanged = Mathf.Abs(targetBounciness - lastBounciness) > ballData.updateThreshold;
        bool dampingChanged = Mathf.Abs(targetDamping - lastDamping) > ballData.updateThreshold;
        
        // 更新弹性系数
        if (bouncinessChanged && material != null)
        {
            material.bounciness = targetBounciness;
            lastBounciness = targetBounciness;
        }
        
        // 更新阻尼
        if (dampingChanged)
        {
            rb.linearDamping = targetDamping;
            lastDamping = targetDamping;
        }
    }
    
    void UpdateDynamicPhysics()
    {
        // 检查更新间隔
        if (Time.fixedTime - lastUpdateTime < ballData.updateInterval)
        {
            return;
        }
        
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // 计算动态物理参数
        var (targetBounciness, targetDamping) = CalculateDynamicPhysics(Time.fixedTime, currentSpeed);
        
        // 应用参数到物理组件
        ApplyDynamicPhysics(targetBounciness, targetDamping);
        
        // 更新缓存时间
        lastUpdateTime = Time.fixedTime;
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        BallPhysics otherBall = collision.gameObject.GetComponent<BallPhysics>();
        if (otherBall != null)
        {
            // 触发球体碰撞事件（模拟模式下不发布）
            if (!isSimulationMode)
            {
                GameEventBus.PublishBallCollision(this, otherBall);
            }
        }
        
        // ✅ 统一发布通用碰撞事件（供 DamageSystem、PlayerBehavior 等处理）
        // 所有球体（玩家、敌人）的碰撞都通过这里统一发布
        if (!isSimulationMode)
        {
            GameEventBus.PublishCollision(CollisionEvent.Create(gameObject, collision));
        }
        
        // 处理墙面碰撞的角度修正
        if (collision.gameObject.CompareTag("Wall"))
        {
            HandleWallCollision(collision);
        }
    }
    
    void HandleWallCollision(Collision2D collision)
    {
        // 获取墙面法向量
        Vector2 wallNormal = collision.contacts[0].normal;
        
        // 计算当前速度
        float currentSpeed = rb.linearVelocity.magnitude;
        Vector2 velocityDirection = rb.linearVelocity.normalized;
        
        // 计算反弹角度
        float angle = Vector2.Angle(velocityDirection, -wallNormal);
        
        // 计算标准反射方向
        Vector2 reflectionDirection = Vector2.Reflect(velocityDirection, wallNormal);
        
        // 使用纯物理反射，不进行角度修正
        // 让物理引擎自然处理反弹
        
        
        // 记录这次反射方向
        lastReflectionDirection = reflectionDirection;
    }
    
    
    
    public void ApplyForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        if (rb != null)
        {
            rb.AddForce(force, mode);
        }
    }
    
    public void SetVelocity(Vector2 velocity)
    {
        if (rb != null)
        {
            // 发射时使用固定的物理参数，确保一致性
            SetFixedPhysicsForLaunch();
            rb.linearVelocity = velocity;
        }
        else
        {
            Debug.LogError("BallPhysics.SetVelocity: rb 为 null！");
        }
    }
    
    // 发射时设置固定的物理参数
    void SetFixedPhysicsForLaunch()
    {
        if (material != null)
        {
            // 使用基础反弹系数，不使用动态值
            material.bounciness = ballData.bounceDamping;
            material.friction = ballData.friction;
        }
        
        // 使用基础阻尼，不使用动态值
        if (rb != null)
        {
            rb.linearDamping = ballData.linearDamping;
            // 确保刚体处于正确状态
            rb.angularVelocity = 0f;
        }
        
        // 重置动态参数缓存，避免动态系统干扰
        lastBounciness = ballData.bounceDamping;
        lastDamping = ballData.linearDamping;
        lastUpdateTime = Time.fixedTime;
        
        // 重置时间阻尼状态
        isMoving = false;
        ballStartTime = 0f;
    }
    
    // 公共方法：重置球体状态
    public void ResetBallState()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
        SetFixedPhysicsForLaunch();
    }
    
    public Vector2 GetVelocity()
    {
        return rb != null ? rb.linearVelocity : Vector2.zero;
    }
    
    public float GetSpeed()
    {
        return rb != null ? rb.linearVelocity.magnitude : 0f;
    }
    
    public bool IsMoving()
    {
        return rb != null && rb.linearVelocity.magnitude > ballData.stopThreshold;
    }
    
    public void ResetBall()
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
        }
    }
    
    /// <summary>
    /// 获取球体的实际半径（考虑缩放）
    /// 注：此方法主要用于旧的手动轨迹预测，新的物理模拟系统不需要
    /// </summary>
    public float GetRadius()
    {
        if (ballCollider == null)
        {
            return 0.5f; // 默认值
        }
        
        // 根据不同Collider类型返回近似半径
        if (ballCollider is CircleCollider2D circleCollider)
        {
            return circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        else if (ballCollider is BoxCollider2D boxCollider)
        {
            // Box的近似半径：取宽高的平均值的一半
            float avgSize = (boxCollider.size.x + boxCollider.size.y) / 2f;
            return avgSize * 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);
        }
        else if (ballCollider is PolygonCollider2D polygonCollider)
        {
            // Polygon的近似半径：基于bounds
            Bounds bounds = polygonCollider.bounds;
            float avgExtent = (bounds.extents.x + bounds.extents.y) / 2f;
            return avgExtent;
        }
        else
        {
            // 其他类型：基于bounds
            Bounds bounds = ballCollider.bounds;
            float avgExtent = (bounds.extents.x + bounds.extents.y) / 2f;
            return avgExtent;
        }
    }
    
    #region 模拟模式专用方法
    
    /// <summary>
    /// 初始化模拟状态（用于影子场景）
    /// 设置初始状态，使物理参数计算从0开始
    /// </summary>
    public void InitializeSimulationState()
    {
        if (!isSimulationMode)
        {
            Debug.LogWarning("InitializeSimulationState 应该在 isSimulationMode = true 时调用");
        }
        
        // 重置时间追踪
        ballStartTime = 0f;
        simulationLastUpdateTime = 0f;
        
        // 设置为运动状态（因为即将开始模拟）
        isMoving = true;
        
        // 重置动态参数缓存
        lastBounciness = ballData.bounceDamping;
        lastDamping = ballData.linearDamping;
        
        Debug.Log($"BallPhysics: 初始化模拟状态完成");
    }
    
    /// <summary>
    /// 手动更新物理参数（用于影子场景模拟）
    /// 保持与主场景相同的更新频率和计算逻辑
    /// </summary>
    /// <param name="simulationTime">当前累积的模拟时间（秒）</param>
    public void ManualPhysicsUpdate(float simulationTime)
    {
        if (!isInitialized) return;
        
        // 检查更新间隔（与主场景保持一致）
        if (simulationTime - simulationLastUpdateTime < ballData.updateInterval)
        {
            // 即使不更新参数，也要执行物理约束
            EnforcePhysicsConstraints();
            return;
        }
        
        float currentSpeed = rb.linearVelocity.magnitude;
        
        // 使用相同的计算逻辑（复用纯函数）
        var (targetBounciness, targetDamping) = CalculateDynamicPhysics(simulationTime, currentSpeed);
        
        // 应用参数到物理组件
        ApplyDynamicPhysics(targetBounciness, targetDamping);
        
        // 更新模拟时间戳
        simulationLastUpdateTime = simulationTime;
        
        // 执行物理约束
        EnforcePhysicsConstraints();
    }
    
    /// <summary>
    /// 执行物理约束（速度限制、禁止旋转）
    /// </summary>
    private void EnforcePhysicsConstraints()
    {
        if (rb == null) return;
        
        // 限制最大速度
        float currentSpeed = rb.linearVelocity.magnitude;
        if (currentSpeed > ballData.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * ballData.maxSpeed;
        }
        
        // 确保球不会旋转
        if (rb.angularVelocity != 0f)
        {
            rb.angularVelocity = 0f;
        }
    }
    
    #endregion
}

