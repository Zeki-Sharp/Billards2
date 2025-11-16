using UnityEngine;
using Core.Physics.Geometry;

public class BallPhysics : MonoBehaviour
{
    [Header("物理数据")]
    public BallData ballData;
    
    // 3D 物理组件（几何模拟依附的可视刚体）
    private Rigidbody rb3D;
    private Collider ballCollider3D;
    private PhysicsMaterial material3D;
    private bool isInitialized = false;
    
    #region 几何物理重构（Phase G）字段
    
    [Header("几何物理配置")]
    [Tooltip("阶段性开关：启用后将逐步替换为几何物理流程（当前阶段默认开启）")]
    public bool enableGeometrySimulation = true;
    
    [Header("几何物理 - 场景配置")]
    [Tooltip("几何模拟：用于检测墙体/障碍的 LayerMask")]
    public LayerMask geometryWallMask = ~0;
    
    [Tooltip("几何模拟：用于检测其他球体的 LayerMask")]
    public LayerMask geometryBallMask = 0;
    
    [Tooltip("几何模拟：SphereCast 半径（0 则自动从 SphereCollider 推断）")]
    public float geometrySphereRadius = 0f;
    
    [Header("几何物理 - 调试")]
    [Tooltip("几何模拟：初始速度（调试/自定义发射时使用）")]
    public float geometryInitialSpeed = 0f;
    
    [Tooltip("几何模拟：初始方向（XZ 平面），调试用")]
    public Vector3 geometryInitialDirection = Vector3.forward;
    
    public bool showGeometryDebug = false;
    public Color geometryDebugRayColor = Color.cyan;
    
    [Header("地面对齐")]
    [Tooltip("脚底位置参考点（Transform）。如果设置，初始化时会向下检测地面，确保脚底贴地")]
    [SerializeField]
    private Transform footPositionRef;
    
    // 几何模拟运行时状态（从 ballData 读取配置）
    private Vector3 geometryDirection = Vector3.forward;
    private float geometrySpeed = 0f;
    private bool geometryIsMoving = false;
    private float geometryElapsedTime = 0f;
    private float ballStartTime = 0f;
    private bool geometryAutoEnableWarningLogged = false;
    private const float GEOMETRY_SURFACE_BACKOFF = 0.001f;
    
    // 从 ballData 读取的几何参数（运行时缓存）
    private float geometryMinSpeedThreshold;
    private float geometryHighSpeedPhaseDuration;
    private float geometryHighPhaseDamping;
    private float geometryLowPhaseDamping;
    private float geometryWallBounceFactor;
    private float geometryBallBounceFactor;
    private float geometryKnockbackScale;
    
    #endregion
    
    // 事件（已移除组件级事件，统一使用GameEventBus）
    
    void Start()
    {
        InitializePhysics();
    }
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyGeometryConfigFromData();
        }
    }
#endif
    
    void Update()
    {
        // 物理相关逻辑已移至FixedUpdate，与物理引擎同步
    }
    
    void FixedUpdate()
    {
        if (!isInitialized)
        {
            return;
        }
        
        EnsureGeometrySimulationEnabled();
        SimulateGeometryStep(Time.fixedDeltaTime);
    }
    
    private void EnsureGeometrySimulationEnabled()
    {
        if (enableGeometrySimulation)
        {
            return;
        }
        
        if (!geometryAutoEnableWarningLogged)
        {
            Debug.LogWarning($"BallPhysics on {gameObject.name}: 几何物理被关闭，但当前版本已移除旧物理，已自动启用。");
            geometryAutoEnableWarningLogged = true;
        }
        enableGeometrySimulation = true;
    }
    
    private void SimulateGeometryStep(float dt)
    {
        if (!enableGeometrySimulation)
        {
            return;
        }
        
        if (geometrySpeed <= geometryMinSpeedThreshold)
        {
            geometrySpeed = 0f;
            OnGeometryMovementStopped();
            return;
        }
        
        OnGeometryMovementStarted();
        
        Vector3 currentPos = transform.position;
        Vector3 displacement = geometryDirection * geometrySpeed * dt;
        float distance = displacement.magnitude;
        if (distance <= 0f)
        {
            geometrySpeed = 0f;
            OnGeometryMovementStopped();
            return;
        }
        
        float sphereRadius = GetEffectiveGeometrySphereRadius();
        Ray ray = new Ray(currentPos, geometryDirection);
        
        RaycastHit ballHit = default;
        bool hitBall = geometryBallMask != 0 &&
                       Physics.SphereCast(ray, sphereRadius, out ballHit, distance, geometryBallMask, QueryTriggerInteraction.Ignore);
        if (hitBall && IsSelfCollider(ballHit.collider))
        {
            hitBall = false;
        }
        
        RaycastHit wallHit = default;
        bool hitWall = Physics.SphereCast(ray, sphereRadius, out wallHit, distance, geometryWallMask, QueryTriggerInteraction.Ignore);
        if (hitWall && IsSelfCollider(wallHit.collider))
        {
            hitWall = false;
        }
        
        if (!hitBall && !hitWall)
        {
            transform.position = currentPos + displacement;
            ApplyGeometryDamping(dt);
            return;
        }
        
        float ballT = hitBall ? Mathf.Clamp01(ballHit.distance / distance) : float.PositiveInfinity;
        float wallT = hitWall ? Mathf.Clamp01(wallHit.distance / distance) : float.PositiveInfinity;
        
        bool resolveBall = ballT < wallT;
        RaycastHit hitInfo = resolveBall ? ballHit : wallHit;
        float travelT = Mathf.Min(ballT, wallT);
        if (float.IsInfinity(travelT))
        {
            transform.position = currentPos + displacement;
            ApplyGeometryDamping(dt);
            return;
        }
        
        float travelDistance = travelT * distance;
        Vector3 hitPos = currentPos + geometryDirection * travelDistance;
        transform.position = hitPos - geometryDirection * GEOMETRY_SURFACE_BACKOFF;
        
        float remainingDt = Mathf.Max(0f, dt * (1f - travelT));
        if (resolveBall)
        {
            HandleGeometryBallCollision(hitInfo, remainingDt);
        }
        else
        {
            HandleGeometryWallCollision(hitInfo, remainingDt);
        }
        
        if (geometrySpeed <= geometryMinSpeedThreshold)
        {
            geometrySpeed = 0f;
            OnGeometryMovementStopped();
        }
    }
    
    private void HandleGeometryWallCollision(RaycastHit hitInfo, float remainingDt)
    {
        Vector3 normal = hitInfo.normal;
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = -geometryDirection;
        }
        normal.Normalize();
        
        Vector3 reflected = Vector3.Reflect(geometryDirection, normal);
        reflected.y = 0f;
        if (reflected.sqrMagnitude < 0.0001f)
        {
            reflected = -geometryDirection;
        }
        reflected.Normalize();
        
        geometryDirection = reflected;
        geometrySpeed *= geometryWallBounceFactor;
        
        if (hitInfo.collider != null)
        {
            PublishGeometryCollisionEvent(hitInfo.collider.gameObject, hitInfo.point, normal);
        }
        
        ApplyGeometryDamping(remainingDt);
    }
    
    private void HandleGeometryBallCollision(RaycastHit hitInfo, float remainingDt)
    {
        Collider otherCollider = hitInfo.collider;
        if (otherCollider == null)
        {
            HandleGeometryWallCollision(hitInfo, remainingDt);
            return;
        }
        
        BallPhysics other = otherCollider.GetComponentInParent<BallPhysics>();
        if (other == null || other == this)
        {
            HandleGeometryWallCollision(hitInfo, remainingDt);
            return;
        }
        
        Vector3 normal = (other.transform.position - transform.position);
        normal.y = 0f;
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = hitInfo.normal;
            normal.y = 0f;
        }
        if (normal.sqrMagnitude < 0.0001f)
        {
            normal = -geometryDirection;
        }
        normal.Normalize();
        
        Vector3 v1 = geometryDirection * geometrySpeed;
        Vector3 v2 = other.GetGeometryDirection() * other.GetGeometrySpeed();
        
        float v1n = Vector3.Dot(v1, normal);
        float v2n = Vector3.Dot(v2, normal);
        
        Vector3 v1t = v1 - v1n * normal;
        Vector3 v2t = v2 - v2n * normal;
        
        Vector3 v1After = v1t + v2n * normal;
        Vector3 v2After = v2t + v1n * normal;
        
        v1After *= geometryBallBounceFactor * geometryKnockbackScale;
        v2After *= other.GetGeometryBallBounceFactor() * other.GetGeometryKnockbackScale();
        
        ApplyGeometryVelocity(v1After, false);
        other.ApplyExternalGeometryVelocity(v2After);
        
        GameEventBus.PublishBallCollision(this, other);
        PublishGeometryCollisionEvent(other.gameObject, hitInfo.point, normal);
        
        ApplyGeometryDamping(remainingDt);
    }
    
    private void ApplyGeometryDamping(float dt)
    {
        if (dt <= 0f || geometrySpeed <= 0f)
        {
            return;
        }
        
        geometryElapsedTime += dt;
        float damping = geometryHighPhaseDamping;
        if (geometryElapsedTime > geometryHighSpeedPhaseDuration)
        {
            damping = geometryLowPhaseDamping;
        }
        
        geometrySpeed = Mathf.Max(0f, geometrySpeed - damping * dt);
    }
    
    private void ApplyGeometryVelocity(Vector3 velocity, bool resetElapsedTime)
    {
        UpdateGeometryVelocity(velocity, resetElapsedTime);
    }
    
    internal void ApplyExternalGeometryVelocity(Vector3 velocity)
    {
        UpdateGeometryVelocity(velocity, true);
    }
    
    private void UpdateGeometryVelocity(Vector3 velocity, bool resetElapsedTime)
    {
        velocity.y = 0f;
        float newSpeed = velocity.magnitude;
        if (newSpeed <= geometryMinSpeedThreshold)
        {
            geometrySpeed = 0f;
            OnGeometryMovementStopped();
            return;
        }
        
        if (velocity.sqrMagnitude > 0f)
        {
            Vector3 newDir = velocity.normalized;
            newDir.y = 0f;
            if (newDir.sqrMagnitude < 0.0001f)
            {
                newDir = Vector3.forward;
            }
            geometryDirection = newDir.normalized;
        }
        
        geometrySpeed = newSpeed;
        if (resetElapsedTime)
        {
            geometryElapsedTime = 0f;
        }
        OnGeometryMovementStarted();
    }
    
    private void PublishGeometryCollisionEvent(GameObject target, Vector3 contactPoint, Vector3 normal)
    {
        if (target == null)
        {
            return;
        }
        
        CollisionEvent evt = new CollisionEvent
        {
            Source = gameObject,
            Target = target,
            ContactPoint = new Vector2(contactPoint.x, contactPoint.z),
            ContactNormal = new Vector2(normal.x, normal.z),
            Velocity = geometrySpeed,
            CollisionTime = Time.time
        };
        GameEventBus.PublishCollision(evt);
    }
    
    private void OnGeometryMovementStarted()
    {
        if (geometryIsMoving)
        {
            return;
        }
        
        geometryIsMoving = true;
        ballStartTime = Time.fixedTime;
        GameEventBus.PublishBallStarted(this);
    }
    
    private void OnGeometryMovementStopped()
    {
        if (!geometryIsMoving)
        {
            geometrySpeed = 0f;
            return;
        }
        
        geometryIsMoving = false;
        geometrySpeed = 0f;
        GameEventBus.PublishBallStopped(this);
    }
    
    private float GetEffectiveGeometrySphereRadius()
    {
        if (geometrySphereRadius > 0f)
        {
            return geometrySphereRadius;
        }
        
        SphereCollider sphereCollider = ballCollider3D as SphereCollider;
        if (sphereCollider != null)
        {
            geometrySphereRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            return geometrySphereRadius;
        }
        
        return 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
    }
    
    private bool IsSelfCollider(Collider collider)
    {
        if (collider == null)
        {
            return true;
        }
        return collider == ballCollider3D || collider.transform == transform;
    }
    
    /// <summary>
    /// 初始化物理组件（公共方法，供影子场景手动调用）
    /// S2: 重写为仅支持3D，配置为kinematic模式，为几何模拟做准备
    /// </summary>
    public void InitializePhysics()
    {
        if (ballData == null)
        {
            Debug.LogError($"BallPhysics on {gameObject.name}: BallData is null!");
            return;
        }
        
        ApplyGeometryConfigFromData();
        
        
        // ==== 3D 物理初始化（S2：移除2D分支，统一使用3D） ====
        
        rb3D = GetComponent<Rigidbody>();
        if (rb3D == null)
        {
            rb3D = gameObject.AddComponent<Rigidbody>();
        }
        
        // S2: 配置为kinematic，几何模拟不依赖物理引擎的速度计算
        rb3D.isKinematic = true;
        rb3D.useGravity = false;
        rb3D.linearDamping = 0f;  // 几何模拟中不使用物理阻尼
        rb3D.angularDamping = 0f;
        // 只锁定 Y 轴位移，允许旋转（用于视觉效果）
        rb3D.constraints = RigidbodyConstraints.FreezePositionY;
        
        // 设置 3D 碰撞器（确保是SphereCollider，用于几何模拟的半径推断）
        ballCollider3D = GetComponent<Collider>();
        if (ballCollider3D == null)
        {
            ballCollider3D = gameObject.AddComponent<SphereCollider>();
            Debug.Log($"BallPhysics: {gameObject.name} 没有3D碰撞器，已添加 SphereCollider");
        }
        else
        {
            Debug.Log($"BallPhysics: {gameObject.name} 检测到3D碰撞器类型: {ballCollider3D.GetType().Name}");
        }
        
        // S2: 确保是SphereCollider，用于推断geometrySphereRadius
        SphereCollider sphereCollider = ballCollider3D as SphereCollider;
        if (sphereCollider == null)
        {
            Debug.LogWarning($"BallPhysics: {gameObject.name} 的碰撞器不是SphereCollider，无法自动推断半径。请手动设置geometrySphereRadius。");
        }
        else
        {
            // 推断几何模拟半径（考虑缩放）
            if (geometrySphereRadius <= 0f)
            {
                geometrySphereRadius = sphereCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Debug.Log($"BallPhysics: {gameObject.name} 自动推断geometrySphereRadius = {geometrySphereRadius:F3}");
            }
        }
        
        ballCollider3D.isTrigger = false;
        
        // 创建并应用 3D 物理材质（保留用于碰撞检测，但几何模拟不依赖其参数）
        material3D = new PhysicsMaterial("BallMaterial3D");
        material3D.bounciness = ballData.bounceDamping;
        material3D.dynamicFriction = ballData.friction;
        material3D.staticFriction = ballData.friction;
        ballCollider3D.material = material3D;
        
        // S2: 初始化几何模拟状态
        geometryDirection = geometryInitialDirection;
        geometryDirection.y = 0f;
        if (geometryDirection.sqrMagnitude < 0.0001f)
        {
            geometryDirection = Vector3.forward;
        }
        geometryDirection.Normalize();
        
        geometrySpeed = geometryInitialSpeed > 0f ? geometryInitialSpeed : 0f;
        geometryIsMoving = geometrySpeed > geometryMinSpeedThreshold;
        geometryElapsedTime = 0f;
        
        // ✅ 地面对齐：如果设置了脚底点，初始化时检测地面并调整Y位置
        if (footPositionRef != null)
        {
            AlignToGround();
        }
        
        isInitialized = true;
        Debug.Log($"BallPhysics initialized for {gameObject.name} (3D kinematic mode, geometrySphereRadius={geometrySphereRadius:F3})");
    }
    
    /// <summary>
    /// ✅ 地面对齐：计算物体应该的Y位置（2D的(x,y) -> 3D的(x,z,y)，其中z=脚底偏移）
    /// </summary>
    private void AlignToGround()
    {
        if (footPositionRef == null)
        {
            // 如果没有脚底点，保持当前位置（假设物体中心就是定位点）
            Debug.LogWarning($"BallPhysics {gameObject.name}: 未设置 footPositionRef，无法对齐地面！");
            return;
        }
        
        // 计算脚底相对于物体中心的偏移（考虑缩放）
        // 如果脚底点在本地坐标 (0, -z, 0)，则 footOffsetY = -z * scaleY
        float footOffsetY = footPositionRef.localPosition.y * transform.lossyScale.y;
        
        // 如果脚底在本地 Y = -z，那么要让脚底在地面（Y=0），物体中心应该在 Y = z
        // 例如：脚底在 localY = -0.5，要让脚底在 Y=0，则物体中心应该在 Y = 0.5
        float groundY = 0f; // 地面在Y=0
        float targetCenterY = groundY - footOffsetY; // 地面Y - 脚底偏移
        
        // 设置物体位置（只修改Y，保持XZ不变）
        Vector3 currentPos = transform.position;
        Vector3 newPos = new Vector3(currentPos.x, targetCenterY, currentPos.z);
        transform.position = newPos;
        
        Debug.Log($"BallPhysics {gameObject.name}: 地面对齐 - 当前位置Y={currentPos.y:F3}, 脚底本地Y={footPositionRef.localPosition.y:F3}, 脚底偏移={footOffsetY:F3}, 目标中心Y={targetCenterY:F3}, 新位置={newPos}");
    }

    
    
    private void ApplyGeometryConfigFromData()
    {
        if (ballData == null)
        {
            return;
        }
        
        geometryMinSpeedThreshold = Mathf.Max(0.001f, ballData.geometryMinSpeedThreshold);
        geometryHighSpeedPhaseDuration = Mathf.Max(0f, ballData.geometryHighSpeedPhaseDuration);
        geometryHighPhaseDamping = Mathf.Max(0f, ballData.geometryHighPhaseDamping);
        geometryLowPhaseDamping = Mathf.Max(0f, ballData.geometryLowPhaseDamping);
        geometryWallBounceFactor = Mathf.Clamp01(ballData.geometryWallBounceFactor);
        geometryBallBounceFactor = Mathf.Clamp01(ballData.geometryBallBounceFactor);
        geometryKnockbackScale = Mathf.Max(0f, ballData.geometryKnockbackScale);
    }
    
    /// <summary>
    /// 获取几何球体碰撞速度保留比例（供其他球体访问）
    /// </summary>
    public float GetGeometryBallBounceFactor()
    {
        return geometryBallBounceFactor;
    }
    
    /// <summary>
    /// 获取几何 Knockback 缩放（供其他球体访问）
    /// </summary>
    public float GetGeometryKnockbackScale()
    {
        return geometryKnockbackScale;
    }
    
    public GeometrySimulationConfig CreateGeometryConfig()
    {
        return new GeometrySimulationConfig
        {
            MinSpeedThreshold = geometryMinSpeedThreshold,
            HighSpeedPhaseDuration = geometryHighSpeedPhaseDuration,
            HighPhaseDamping = geometryHighPhaseDamping,
            LowPhaseDamping = geometryLowPhaseDamping,
            WallBounceFactor = geometryWallBounceFactor,
            BallBounceFactor = geometryBallBounceFactor,
            KnockbackScale = geometryKnockbackScale,
            SphereRadius = GetEffectiveGeometrySphereRadius(),
            WallMask = geometryWallMask,
            BallMask = geometryBallMask,
            ShowDebug = showGeometryDebug,
            DebugColor = geometryDebugRayColor,
            SourceTransform = transform,
            SourceCollider = ballCollider3D
        };
    }
    
    /// <summary>
    /// 获取几何方向（供其他球体访问）
    /// </summary>
    public Vector3 GetGeometryDirection()
    {
        return geometryDirection;
    }
    
    /// <summary>
    /// 获取几何速度（供其他球体访问）
    /// </summary>
    public float GetGeometrySpeed()
    {
        return geometrySpeed;
    }
    
    /// <summary>
    /// 应用动态物理参数到物理组件
    /// </summary>
    /// <param name="targetBounciness">目标弹性系数</param>
    /// <param name="targetDamping">目标阻尼值</param>
    
    
    public void ApplyForce(Vector2 force, ForceMode2D mode = ForceMode2D.Impulse)
    {
        _ = mode; // 兼容旧接口，保留参数但不再使用
        Vector3 currentVelocity = geometryDirection * geometrySpeed;
        Vector3 force3D = new Vector3(force.x, 0f, force.y);
        currentVelocity += force3D;
        ApplyGeometryVelocity(currentVelocity, false);
    }
    
    public void SetVelocity(Vector2 velocity)
        {
            // 发射时使用固定的物理参数，确保一致性
            SetFixedPhysicsForLaunch();
        Vector3 v3 = new Vector3(velocity.x, 0f, velocity.y);
        ApplyGeometryVelocity(v3, true);
    }
    
    // 发射时设置固定的物理参数（几何版：只同步 3D 物理材质 + 几何计时）
    void SetFixedPhysicsForLaunch()
    {
        if (material3D != null)
        {
            material3D.bounciness = ballData.bounceDamping;
            material3D.dynamicFriction = ballData.friction;
            material3D.staticFriction = ballData.friction;
        }
        
        // 重置几何衰减计时，让每次发射都从“高速阶段”开始
        geometryElapsedTime = 0f;
    }
    
    // 公共方法：重置球体状态
    public void ResetBallState()
    {
        geometrySpeed = 0f;
        geometryDirection = Vector3.forward;
        geometryElapsedTime = 0f;
        OnGeometryMovementStopped();
        SetFixedPhysicsForLaunch();
    }
    
    public Vector2 GetVelocity()
    {
        Vector3 v = geometryDirection * geometrySpeed;
        return new Vector2(v.x, v.z);
    }
    
    public float GetSpeed()
    {
        return geometrySpeed;
    }
    
    public bool IsMoving()
    {
        return geometryIsMoving;
    }
    
    public void ResetBall()
    {
        ResetBallState();
    }
    

}

