using UnityEngine;

/// <summary>
/// 几何物理原型（G0 阶段专用）：
/// - 不依赖 Rigidbody 速度与摩擦
/// - 使用几何反射 + 简单线性衰减
/// - 仅支持：单球 + 静态墙体（通过 Collider 提供法线）
///
/// 使用方式（建议在独立测试场景中挂载）：
/// - 在球体上挂一个 SphereCollider（或任意 3D Collider，表示半径与形状）
/// - 可选：挂一个 Rigidbody 并勾选 isKinematic（只作为碰撞体承载者，不使用其速度计算）
/// - 将本组件挂到同一 GameObject 上，设置初始方向、速度与参数
/// - 通过 Inspector 的 Debug 选项观察运行状态
///
/// 注意：
/// - 这是 G0 原型脚本，不直接参与正式战斗逻辑
/// - 未来正式几何化版本会合并/重构到 BallPhysics 中
/// </summary>
public class BallPhysicsGeometryPrototype : MonoBehaviour
{
    [Header("几何物理参数")]
    [Tooltip("初始速度（单位/秒）")]
    public float initialSpeed = 10f;

    [Tooltip("初始方向（世界空间，XZ 平面），会在运行时归一化且 Y 分量被忽略")]
    public Vector3 initialDirection = new Vector3(1f, 0f, 1f);

    [Tooltip("每秒线性速度衰减量（单位/秒）")]
    public float linearDamping = 1.5f;

    [Tooltip("墙体碰撞后速度保留比例")]
    [Range(0f, 1f)]
    public float wallBounceFactor = 0.95f;

    [Tooltip("低于该速度视为停止")]
    public float minSpeedThreshold = 0.2f;

    [Header("分段衰减设置")]
    [Tooltip("是否使用“先快后猛降”的分段衰减模型")]
    public bool usePiecewiseDamping = true;

    [Tooltip("高速阶段持续时间（秒）。在此时间内速度衰减很慢，保持较高速度")]
    public float highSpeedPhaseDuration = 1.0f;

    [Tooltip("高速阶段的线性衰减系数（单位/秒，建议较小）")]
    public float highPhaseDamping = 0.3f;

    [Tooltip("减速阶段的线性衰减系数（单位/秒，建议明显大于高速阶段）")]
    public float lowPhaseDamping = 4.0f;

    [Header("碰撞设置 - 球体")]
    [Tooltip("用于检测其他球体的 LayerMask（仅几何原型，用于球↔球碰撞）")]
    public LayerMask ballCollisionMask;

    [Tooltip("球↔球碰撞后速度保留比例（全局弹性）")]
    [Range(0f, 1f)]
    public float ballBounceFactor = 0.98f;

    [Tooltip("自身被击中时速度缩放系数（1 = 标准玩家；重型敌人可设为 <1，轻型敌人可 >1）")]
    public float knockbackScale = 1f;

    [Header("碰撞设置")]
    [Tooltip("SphereCast 的半径，如果为 0 则自动尝试从 SphereCollider 读取半径")]
    public float sphereRadius = 0.0f;

    [Tooltip("用于检测墙体/障碍物的 LayerMask")]
    public LayerMask collisionMask = ~0;

    [Header("调试")]
    public bool showDebugInfo = true;
    public Color debugRayColor = Color.cyan;

    // 运行时状态
    private Vector3 direction;   // 单位向量（XZ 平面）
    private float speed;         // 标量速度

    private bool isMoving = false;
    private float elapsedTime = 0f; // 用于分段衰减的累计时间

    void Start()
    {
        InitializeState();
    }

    void FixedUpdate()
    {
        if (!isMoving) return;

        float dt = Time.fixedDeltaTime;
        SimulateStep(dt);
    }

    /// <summary>
    /// 初始化内部状态（在 Start 或外部重置时调用）
    /// </summary>
    public void InitializeState()
    {
        // 方向归一化，约束在 XZ 平面
        direction = initialDirection;
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            direction = Vector3.forward;
        }
        direction.Normalize();

        speed = Mathf.Max(0f, initialSpeed);
        isMoving = speed > minSpeedThreshold;
        elapsedTime = 0f;

        if (sphereRadius <= 0f)
        {
            // 尝试从 SphereCollider 推断半径
            SphereCollider sc = GetComponent<SphereCollider>();
            if (sc != null)
            {
                sphereRadius = sc.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
            }
        }

        if (showDebugInfo)
        {
            Debug.Log($"[GeometryPrototype] 初始化：pos={transform.position}, dir={direction}, speed={speed:F2}, radius={sphereRadius:F2}");
        }
    }

    /// <summary>
    /// 单步几何模拟（G0：仅处理一次墙体碰撞）
    /// </summary>
    /// <param name="dt">时间步长</param>
    private void SimulateStep(float dt)
    {
        if (speed <= minSpeedThreshold)
        {
            speed = 0f;
            isMoving = false;
            return;
        }

        Vector3 currentPos = transform.position;
        Vector3 displacement = direction * speed * dt;
        float distance = displacement.magnitude;

        if (distance <= 0f)
        {
            isMoving = false;
            return;
        }

        // 使用 SphereCast 预测前方是否会撞到其他球体或墙体
        Ray ray = new Ray(currentPos, direction);

        RaycastHit ballHit;
        bool hitBall = Physics.SphereCast(ray, sphereRadius, out ballHit, distance, ballCollisionMask, QueryTriggerInteraction.Ignore);
        if (hitBall && ballHit.collider != null && ballHit.collider.transform == transform)
        {
            // 忽略自己
            hitBall = false;
        }

        RaycastHit wallHit;
        bool hitWall = Physics.SphereCast(ray, sphereRadius, out wallHit, distance, collisionMask, QueryTriggerInteraction.Ignore);

        if (showDebugInfo)
        {
            Debug.DrawRay(currentPos, direction * distance, debugRayColor, 0.1f);
        }

        if (!hitBall && !hitWall)
        {
            // 没有碰撞：直接位移 + 衰减
            transform.position = currentPos + displacement;
            ApplyDamping(dt);
            return;
        }

        // 选择最近的碰撞（球体或墙）
        float ballT = hitBall ? Mathf.Clamp01(ballHit.distance / distance) : float.PositiveInfinity;
        float wallT = hitWall ? Mathf.Clamp01(wallHit.distance / distance) : float.PositiveInfinity;

        bool resolveBallCollision = ballT < wallT;
        RaycastHit hitInfo = resolveBallCollision ? ballHit : wallHit;
        float t = Mathf.Min(ballT, wallT);

        // 先走到碰撞点附近
        float travelDist = t * distance;
        Vector3 hitPos = currentPos + direction * travelDist;

        // 微小回退，避免卡在表面
        const float backoff = 0.001f;
        transform.position = hitPos - direction * backoff;

        if (resolveBallCollision)
        {
            HandleBallCollision(hitInfo, dt * (1f - t));
        }
        else
        {
            HandleWallCollision(hitInfo, dt * (1f - t));
        }

        if (speed <= minSpeedThreshold)
        {
            speed = 0f;
            isMoving = false;
        }
    }

    /// <summary>
    /// 处理球↔墙碰撞（几何反射 + 速度折损）
    /// </summary>
    private void HandleWallCollision(RaycastHit hitInfo, float remainingDt)
    {
        // 计算反射方向（在 XZ 平面）
        Vector3 n = hitInfo.normal;
        n.y = 0f;
        if (n.sqrMagnitude < 0.0001f)
        {
            n = -direction; // 法线异常时，简单反向
        }
        n.Normalize();

        Vector3 newDir = Vector3.Reflect(direction, n);
        newDir.y = 0f;
        if (newDir.sqrMagnitude < 0.0001f)
        {
            newDir = -direction;
        }
        newDir.Normalize();

        direction = newDir;
        speed *= wallBounceFactor;

        if (showDebugInfo)
        {
            Debug.Log($"[GeometryPrototype] 墙体碰撞：hit={hitInfo.collider.name}, normal={hitInfo.normal}, newDir={direction}, speed={speed:F2}");
        }

        ApplyDamping(remainingDt);
    }

    /// <summary>
    /// 处理球↔球碰撞（等质量近似，法线分量交换）
    /// </summary>
    private void HandleBallCollision(RaycastHit hitInfo, float remainingDt)
    {
        if (hitInfo.collider == null) return;

        BallPhysicsGeometryPrototype other = hitInfo.collider.GetComponent<BallPhysicsGeometryPrototype>() ??
                                             hitInfo.collider.GetComponentInParent<BallPhysicsGeometryPrototype>();
        if (other == null || other == this) return;

        // 当前与对方的速度向量（XZ 平面）
        Vector3 v1 = direction * speed;
        Vector3 v2 = other.direction * other.speed;

        // 碰撞法线：从当前球指向对方
        Vector3 n = (other.transform.position - transform.position);
        n.y = 0f;
        if (n.sqrMagnitude < 0.0001f)
        {
            n = hitInfo.normal;
            n.y = 0f;
        }
        if (n.sqrMagnitude < 0.0001f)
        {
            // 法线异常，直接视作墙碰撞
            HandleWallCollision(hitInfo, remainingDt);
            return;
        }
        n.Normalize();

        // 分解到法线方向（等质量、完全弹性情况下交换法线分量）
        float v1n = Vector3.Dot(v1, n);
        float v2n = Vector3.Dot(v2, n);

        Vector3 v1nVec = v1n * n;
        Vector3 v2nVec = v2n * n;

        Vector3 v1t = v1 - v1nVec;
        Vector3 v2t = v2 - v2nVec;

        Vector3 v1After = v1t + v2nVec;
        Vector3 v2After = v2t + v1nVec;

        // 速度折损：先按全局弹性衰减，再按各自的受击系数缩放
        v1After *= ballBounceFactor * knockbackScale;
        v2After *= ballBounceFactor * other.knockbackScale;

        // 更新当前球状态
        Vector3 dir1 = v1After;
        dir1.y = 0f;
        float speed1 = dir1.magnitude;
        if (speed1 > 0.0001f)
        {
            direction = dir1 / speed1;
            speed = speed1;
            isMoving = true;
        }
        else
        {
            speed = 0f;
            isMoving = false;
        }

        // 更新对方球状态
        Vector3 dir2 = v2After;
        dir2.y = 0f;
        float speed2 = dir2.magnitude;
        if (speed2 > 0.0001f)
        {
            other.direction = dir2 / speed2;
            other.speed = speed2;
            other.isMoving = true;
        }
        else
        {
            other.speed = 0f;
            other.isMoving = false;
        }

        if (showDebugInfo)
        {
            Debug.Log($"[GeometryPrototype] 球体碰撞：self_speed={speed:F2}, other_speed={other.speed:F2}, n={n}");
        }

        // 对当前球应用剩余时间的衰减（对方球的衰减由其自身 FixedUpdate 处理）
        ApplyDamping(remainingDt);
    }

    /// <summary>
    /// 应用速度衰减（支持简单线性或分段线性）
    /// </summary>
    private void ApplyDamping(float dt)
    {
        if (dt <= 0f) return;

        // 分段模型：前一段几乎不减速，后一段快速衰减
        if (usePiecewiseDamping)
        {
            elapsedTime += dt;

            // 高速阶段：衰减很小，视觉上“先滑一大段”
            float damping = elapsedTime < highSpeedPhaseDuration
                ? highPhaseDamping
                : lowPhaseDamping;

            if (damping <= 0f) return;

            speed = Mathf.Max(0f, speed - damping * dt);
        }
        else
        {
            if (linearDamping <= 0f) return;
            speed = Mathf.Max(0f, speed - linearDamping * dt);
        }
    }
}


