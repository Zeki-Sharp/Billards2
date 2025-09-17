using UnityEngine;

public class BallPhysics : MonoBehaviour
{
    [Header("物理数据")]
    public BallData ballData;
    
    private Rigidbody2D rb;
    private CircleCollider2D ballCollider;
    private PhysicsMaterial2D material;
    private bool isInitialized = false;
    
    // 动态物理参数缓存
    private float lastBounciness = -1f;
    private float lastDamping = -1f;
    private float lastUpdateTime = 0f;
    
    // 时间阻尼相关变量
    private float ballStartTime = 0f;
    private bool isMoving = false;
    
    // 调试设置
    private bool enableDebugLog = true;
    
    // 反弹方向检测
    private Vector2 lastReflectionDirection = Vector2.zero;
    
    // 事件
    public System.Action<BallPhysics> OnBallStopped;
    public System.Action<BallPhysics, BallPhysics> OnBallCollision;
    
    void Start()
    {
        InitializePhysics();
    }
    
    void Update()
    {
        if (isInitialized)
        {
            CheckMovement();
            UpdateDynamicPhysics();
        }
    }
    
    void InitializePhysics()
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
        
        // 设置碰撞器
        ballCollider = GetComponent<CircleCollider2D>();
        if (ballCollider == null)
        {
            // 检查对象上是否已经有任何Collider2D
            Collider2D existingCollider = GetComponent<Collider2D>();
            if (existingCollider == null)
            {
                // 如果没有任何碰撞器，才添加CircleCollider2D
                ballCollider = gameObject.AddComponent<CircleCollider2D>();
            }
            else
            {
                Debug.LogWarning($"对象 {gameObject.name} 上已存在其他类型的碰撞器 ({existingCollider.GetType().Name})，未添加 CircleCollider2D");
            }
        }
        
        // 只有当ballCollider不为null时才设置其属性
        if (ballCollider != null)
        {
            ballCollider.radius = ballData.radius;
            ballCollider.isTrigger = false;
        }
        
        // 创建物理材质（只有当ballCollider存在时才设置）
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
                ballStartTime = Time.time;
                Debug.Log($"BallPhysics: 球开始运动，记录时间 {ballStartTime:F2}");
            }
        }
        else
        {
            // 球停止运动
            if (isMoving)
            {
                isMoving = false;
                float movementDuration = Time.time - ballStartTime;
                Debug.Log($"BallPhysics: 球停止运动，运动时长 {movementDuration:F2} 秒");
            }
            
            // 如果速度低于停止阈值，强制停止
            if (currentSpeed <= ballData.stopThreshold && currentSpeed > 0.01f)
            {
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                OnBallStopped?.Invoke(this);
            }
            else if (currentSpeed <= 0.01f)
            {
                // 速度极低时也认为已停止
                rb.linearVelocity = Vector2.zero;
                rb.angularVelocity = 0f;
                OnBallStopped?.Invoke(this);
            }
        }
        
        // 限制最大速度
        if (currentSpeed > ballData.maxSpeed)
        {
            rb.linearVelocity = rb.linearVelocity.normalized * ballData.maxSpeed;
        }
    }
    
    void UpdateDynamicPhysics()
    {
        // 检查更新间隔
        if (Time.time - lastUpdateTime < ballData.updateInterval)
        {
            return;
        }
        
        float currentSpeed = rb.linearVelocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / ballData.maxSpeed);
        
        // 计算动态弹性系数
        float targetBounciness = ballData.speedToBounciness.Evaluate(normalizedSpeed);
        targetBounciness = Mathf.Lerp(ballData.minBounciness, ballData.maxBounciness, targetBounciness);
        
        // 计算动态阻尼
        float targetDamping = ballData.speedToDamping.Evaluate(normalizedSpeed);
        targetDamping = Mathf.Lerp(ballData.minDamping, ballData.maxDamping, targetDamping);
        
        // 添加时间阻尼
        if (ballData.enableTimeDamping && isMoving)
        {
            float timeSinceStart = Time.time - ballStartTime;
            if (timeSinceStart > ballData.timeDampingStartTime)
            {
                float timeDamping = Mathf.Min(
                    ballData.timeDampingRate * (timeSinceStart - ballData.timeDampingStartTime),
                    ballData.maxTimeDamping
                );
                targetDamping += timeDamping;
                
                // if (enableDebugLog)
                // {
                //     Debug.Log($"BallPhysics: 时间阻尼 - 运动时长: {timeSinceStart:F2}s, 时间阻尼: {timeDamping:F2}, 总阻尼: {targetDamping:F2}");
                // }
            }
        }
        
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
        
        // 更新缓存时间
        if (bouncinessChanged || dampingChanged)
        {
            lastUpdateTime = Time.time;
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        BallPhysics otherBall = collision.gameObject.GetComponent<BallPhysics>();
        if (otherBall != null)
        {
            // 触发球体碰撞事件
            OnBallCollision?.Invoke(this, otherBall);
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
            Debug.Log($"BallPhysics.SetVelocity: 输入速度={velocity}, 速度大小={velocity.magnitude}");
            
            // 发射时使用固定的物理参数，确保一致性
            SetFixedPhysicsForLaunch();
            rb.linearVelocity = velocity;
            
            Debug.Log($"BallPhysics.SetVelocity: 设置后速度={rb.linearVelocity}, 速度大小={rb.linearVelocity.magnitude}");
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
        lastUpdateTime = Time.time;
        
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
}
