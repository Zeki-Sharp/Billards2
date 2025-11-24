using UnityEngine;
using Core.Physics.Geometry;

public class BallPhysics : MonoBehaviour
{
    [Header("物理数据")]
    public BallData ballData;
    
    [Tooltip("（可选）主动移动时使用的几何物理配置。如果为空，则复用上面的 ballData 配置")]
    public BallData moveBallData;
    
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
    
    [Header("几何物理 - 调试")]
    [Tooltip("几何模拟：SphereCast 半径（高级选项，0=自动从碰撞器推断，>0=手动设置。通常不需要修改，系统会自动从碰撞器推断）")]
    public float geometrySphereRadius = 0f;
    [Tooltip("几何模拟：初始速度（调试/自定义发射时使用）")]
    public float geometryInitialSpeed = 0f;
    
    [Tooltip("几何模拟：初始方向（XZ 平面），调试用")]
    public Vector3 geometryInitialDirection = Vector3.forward;
    
    public bool showGeometryDebug = false;
    public Color geometryDebugRayColor = Color.cyan;
    
    // ✅ 地面对齐已迁移到 GroundAlignAnchor 组件，不再使用 footPositionRef
    
    // 几何模拟运行时状态（从 ballData 读取配置）
    private Vector3 geometryDirection = Vector3.forward;
    private float geometrySpeed = 0f;
    private bool geometryIsMoving = false;
    private float geometryElapsedTime = 0f;
    private float ballStartTime = 0f;
    private bool geometryAutoEnableWarningLogged = false;
    private const float GEOMETRY_SURFACE_BACKOFF = 0.001f;
    private const int MAX_COLLISION_ITERATIONS = 10;  // 最大碰撞迭代次数，防止无限递归
    private const float MIN_STEP_DISTANCE = 0.0001f;  // 最小步进距离，避免无限细分
    private const int MAX_OVERLAP_RESOLVE_ITERATIONS = 3;  // 最大重叠解决迭代次数（增加迭代次数，让修正更渐进）
    private const float MAX_SINGLE_CORRECTION_DISTANCE = 0.02f;  // 单次修正的最大距离，防止突然飞出（约球半径的0.04-0.08倍，更平滑更不明显）
    
    // 从 ballData 读取的几何参数（运行时缓存）
    private float geometryMinSpeedThreshold;
    private float geometryHighSpeedPhaseDuration;
    private float geometryHighPhaseDamping;
    private float geometryLowPhaseDamping;
    private float geometryWallBounceFactor;
    private float geometryBallBounceFactor;
    private float geometryKnockbackScale;
    
    /// <summary>
    /// 几何运动模式：区分「击退/发射」与「主动移动」两种配置
    /// </summary>
    private enum GeometryMotionMode
    {
        /// <summary>默认模式：玩家发射、击退等使用（使用 ballData）</summary>
        Knockback = 0,
        /// <summary>主动移动模式：小兵行走等使用（使用 moveBallData，若为空则退回 ballData）</summary>
        ActiveMove = 1
    }
    
    [SerializeField]
    private GeometryMotionMode geometryMotionMode = GeometryMotionMode.Knockback;
    
    // 缓存两套几何参数：击退/发射用 与 主动移动用
    private float knockbackMinSpeedThreshold;
    private float knockbackHighSpeedPhaseDuration;
    private float knockbackHighPhaseDamping;
    private float knockbackLowPhaseDamping;
    private float knockbackWallBounceFactor;
    private float knockbackBallBounceFactor;
    private float knockbackKnockbackScale;
    
    private float moveMinSpeedThreshold;
    private float moveHighSpeedPhaseDuration;
    private float moveHighPhaseDamping;
    private float moveLowPhaseDamping;
    private float moveWallBounceFactor;
    private float moveBallBounceFactor;
    private float moveKnockbackScale;
    
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
        
        // ✅ 使用递归子步进处理碰撞，确保处理所有碰撞
        SimulateGeometryStepRecursive(dt, 0);
        
        if (geometrySpeed <= geometryMinSpeedThreshold)
        {
            geometrySpeed = 0f;
            OnGeometryMovementStopped();
        }
    }
    
    /// <summary>
    /// 递归子步进碰撞处理（参考成熟物理引擎的实现）
    /// 每次处理一个碰撞，然后递归处理剩余时间，直到没有碰撞或达到最大迭代次数
    /// </summary>
    private void SimulateGeometryStepRecursive(float dt, int iteration)
    {
        // 防止无限递归
        if (iteration >= MAX_COLLISION_ITERATIONS)
        {
            if (showGeometryDebug)
            {
                Debug.LogWarning($"BallPhysics {gameObject.name}: 达到最大碰撞迭代次数 {MAX_COLLISION_ITERATIONS}，停止递归");
            }
            return;
        }
        
        if (geometrySpeed <= geometryMinSpeedThreshold || dt <= 0f)
        {
            return;
        }
        
        Vector3 currentPos = transform.position;
        
        // ✅ 关键修复：在 Cast 之前先检查并解决重叠问题
        // 如果球已经与墙/其他球重叠，Cast 会失效，必须先解决重叠
        ResolveOverlapsBeforeCast();
        currentPos = transform.position;  // 更新位置，因为可能被修正了
        
        Vector3 displacement = geometryDirection * geometrySpeed * dt;
        float distance = displacement.magnitude;
        
        // 如果移动距离太小，直接应用衰减并返回
        if (distance < MIN_STEP_DISTANCE)
        {
            ApplyGeometryDamping(dt);
            return;
        }
        
        Ray ray = new Ray(currentPos, geometryDirection);
        
        // ✅ 根据自身碰撞器类型选择对应的 Cast 方法（BoxCast/CapsuleCast/SphereCast）
        RaycastHit ballHit = default;
        bool hitBall = geometryBallMask != 0 && 
                       PerformColliderCast(ray, distance, geometryBallMask, out ballHit);
        if (hitBall && IsSelfCollider(ballHit.collider))
        {
            hitBall = false;
        }
        
        // ✅ 调试：检查是否检测到球体碰撞
        if (showGeometryDebug && hitBall && ballHit.collider != null)
        {
            GameObject hitObj = ballHit.collider.gameObject;
            int hitLayer = hitObj.layer;
            Debug.Log($"BallPhysics {gameObject.name}: 检测到球体碰撞 - 对象:{hitObj.name}, Layer:{hitLayer}, geometryBallMask:{geometryBallMask.value}");
        }
        
        RaycastHit wallHit = default;
        bool hitWall = PerformColliderCast(ray, distance, geometryWallMask, out wallHit);
        if (hitWall && IsSelfCollider(wallHit.collider))
        {
            hitWall = false;
        }
        
        // ✅ 调试：检查是否检测到墙壁碰撞（可能误检测到敌人）
        if (showGeometryDebug && hitWall && wallHit.collider != null)
        {
            GameObject hitObj = wallHit.collider.gameObject;
            int hitLayer = hitObj.layer;
            BallPhysics otherBall = hitObj.GetComponentInParent<BallPhysics>();
            if (otherBall != null)
            {
                Debug.LogWarning($"BallPhysics {gameObject.name}: 在 geometryWallMask 中检测到球体 - 对象:{hitObj.name}, Layer:{hitLayer}, 这应该是球体碰撞而不是墙壁碰撞！");
            }
        }
        
        // 如果没有碰撞，直接移动并应用衰减
        if (!hitBall && !hitWall)
        {
            transform.position = currentPos + displacement;
            ApplyGeometryDamping(dt);
            return;
        }
        
        // 选择最近的碰撞
        float ballT = hitBall ? Mathf.Clamp01(ballHit.distance / distance) : float.PositiveInfinity;
        float wallT = hitWall ? Mathf.Clamp01(wallHit.distance / distance) : float.PositiveInfinity;
        
        bool resolveBall = ballT < wallT;
        RaycastHit hitInfo = resolveBall ? ballHit : wallHit;
        float travelT = Mathf.Min(ballT, wallT);
        
        if (float.IsInfinity(travelT) || travelT <= 0f)
        {
            // 如果碰撞距离无效，直接移动并应用衰减
            transform.position = currentPos + displacement;
            ApplyGeometryDamping(dt);
            return;
        }
        
        // 移动到碰撞点（精确位置，使用更小的回退距离）
        float travelDistance = travelT * distance;
        Vector3 hitPos = currentPos + geometryDirection * travelDistance;
        
        // ✅ 使用更精确的回退：确保球不会与碰撞体重叠
        // 回退距离应该至少等于球的半径，但这里使用较小的值避免过度回退
        float backoffDistance = Mathf.Max(GEOMETRY_SURFACE_BACKOFF, GetEffectiveGeometrySphereRadius() * 0.1f);
        transform.position = hitPos - geometryDirection * backoffDistance;
        
        // 计算剩余时间
        float remainingDt = dt * (1f - travelT);
        
        // 处理碰撞（更新速度方向，衰减在移动过程中应用）
        // 注意：这里传入 travelT * dt 作为已移动的时间，用于衰减计算
        float traveledDt = dt * travelT;
        
        if (resolveBall)
        {
            HandleGeometryBallCollision(hitInfo, traveledDt);
        }
        else
        {
            // ✅ 调试：检查是否误检测为墙壁（应该是球体碰撞）
            if (showGeometryDebug && hitInfo.collider != null)
            {
                GameObject hitObj = hitInfo.collider.gameObject;
                BallPhysics otherBall = hitObj.GetComponentInParent<BallPhysics>();
                if (otherBall != null)
                {
                    Debug.LogWarning($"BallPhysics {gameObject.name}: 碰撞被判断为墙壁，但目标是球体({hitObj.name})！这可能是 LayerMask 配置问题。");
                }
            }
            HandleGeometryWallCollision(hitInfo, traveledDt);
        }
        
        // ✅ 关键：递归处理剩余时间，处理后续可能的碰撞
        if (remainingDt > 0.0001f && geometrySpeed > geometryMinSpeedThreshold)
        {
            SimulateGeometryStepRecursive(remainingDt, iteration + 1);
        }
        else if (remainingDt > 0.0001f)
        {
            // 如果没有剩余时间或速度太小，只应用剩余时间的衰减
            ApplyGeometryDamping(remainingDt);
        }
    }
    
    /// <summary>
    /// 在 Cast 之前解决重叠问题（关键修复：防止 Cast 失效）
    /// 如果球已经与墙/其他球重叠，Cast 无法检测到，必须先解决重叠
    /// </summary>
    private void ResolveOverlapsBeforeCast()
    {
        if (ballCollider3D == null)
        {
            return;
        }
        
        Vector3 currentPos = transform.position;
        float radius = GetEffectiveGeometrySphereRadius();
        
        // 迭代解决重叠（最多2次，避免性能问题）
        for (int i = 0; i < MAX_OVERLAP_RESOLVE_ITERATIONS; i++)
        {
            bool resolved = false;
            
            // 1. 检查与墙的重叠
            if (geometryWallMask != 0)
            {
                Collider[] wallOverlaps = Physics.OverlapSphere(currentPos, radius, geometryWallMask, QueryTriggerInteraction.Ignore);
                foreach (var wallCollider in wallOverlaps)
                {
                    if (wallCollider == null || IsSelfCollider(wallCollider))
                    {
                        continue;
                    }
                    
                    // 使用 ComputePenetration 计算推出向量
                    Vector3 direction;
                    float distance;
                    if (Physics.ComputePenetration(
                        ballCollider3D, currentPos, transform.rotation,
                        wallCollider, wallCollider.transform.position, wallCollider.transform.rotation,
                        out direction, out distance))
                    {
                        // 归一化方向到 XZ 平面
                        direction = NormalizeDirectionXZ(direction, currentPos, wallCollider.bounds.center);
                        
                        // 计算平滑修正距离
                        float totalPushDistance = distance + GEOMETRY_SURFACE_BACKOFF;
                        float actualPushDistance = CalculateSmoothCorrectionDistance(totalPushDistance, distance);
                        
                        Vector3 pushOut = direction * actualPushDistance;
                        transform.position = currentPos + pushOut;
                        currentPos = transform.position;
                        resolved = true;
                        
                        if (showGeometryDebug)
                        {
                            Debug.Log($"BallPhysics {gameObject.name}: 解决墙重叠 - 墙:{wallCollider.name}, 重叠深度:{distance:F4}, 实际推出:{actualPushDistance:F4}");
                        }
                    }
                }
            }
            
            // 2. 检查与其他球的重叠
            if (geometryBallMask != 0)
            {
                Collider[] ballOverlaps = Physics.OverlapSphere(currentPos, radius, geometryBallMask, QueryTriggerInteraction.Ignore);
                foreach (var otherCollider in ballOverlaps)
                {
                    if (otherCollider == null || IsSelfCollider(otherCollider))
                    {
                        continue;
                    }
                    
                    BallPhysics other = otherCollider.GetComponentInParent<BallPhysics>();
                    if (other == null || other == this || other.ballCollider3D == null)
                    {
                        continue;
                    }
                    
                    // 计算两球中心距离和方向
                    Vector3 otherPos = other.transform.position;
                    Vector3 centerToCenter = otherPos - currentPos;
                    float centerDistance = centerToCenter.magnitude;
                    centerToCenter = NormalizeDirectionXZ(centerToCenter, Vector3.zero, Vector3.zero);
                    
                    // 计算两球半径之和
                    float otherRadius = other.GetEffectiveGeometrySphereRadius();
                    float minSeparationDistance = radius + otherRadius + GEOMETRY_SURFACE_BACKOFF;
                    
                    // 如果两球重叠，进行分离（只移动当前球，避免竞争条件）
                    if (centerDistance < minSeparationDistance)
                    {
                        float overlapDepth = minSeparationDistance - centerDistance;
                        
                        // 计算平滑修正距离（球重叠不需要额外的 backoff，因为已经在 minSeparationDistance 中包含了）
                        float actualMoveDistance = CalculateSmoothCorrectionDistance(overlapDepth, overlapDepth);
                        
                        // 只移动当前球，避免两个球同时修改位置导致竞争
                        Vector3 separationDir = -centerToCenter;  // 当前球远离对方
                        Vector3 selfMove = separationDir * actualMoveDistance;
                        transform.position = currentPos + selfMove;
                        currentPos = transform.position;
                        resolved = true;
                        
                        if (showGeometryDebug || other.showGeometryDebug)
                        {
                            Debug.Log($"BallPhysics 解决球重叠 - {gameObject.name} 和 {other.gameObject.name}, 重叠深度:{overlapDepth:F4}, 实际修正:{actualMoveDistance:F4}");
                        }
                    }
                }
            }
            
            // 如果这一轮没有解决任何重叠，可以提前退出
            if (!resolved)
            {
                break;
            }
        }
    }
    
    /// <summary>
    /// 归一化方向向量到 XZ 平面（提取的公共方法）
    /// </summary>
    private Vector3 NormalizeDirectionXZ(Vector3 direction, Vector3 fallbackFrom, Vector3 fallbackTo)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.0001f)
        {
            // 如果方向无效，使用备用方向
            if (fallbackFrom != Vector3.zero && fallbackTo != Vector3.zero)
            {
                direction = (fallbackFrom - fallbackTo);
                direction.y = 0f;
            }
            if (direction.sqrMagnitude < 0.0001f)
            {
                direction = Vector3.forward;  // 最后的兜底
            }
        }
        return direction.normalized;
    }
    
    /// <summary>
    /// 计算平滑修正距离（提取的公共方法，避免突然飞出）
    /// 策略：如果重叠不深，完全修正；如果重叠很深，只修正一部分，让它在后续帧中逐渐修正
    /// </summary>
    private float CalculateSmoothCorrectionDistance(float totalDistance, float overlapDepth)
    {
        if (totalDistance <= MAX_SINGLE_CORRECTION_DISTANCE)
        {
            // 重叠不深，完全修正
            return totalDistance;
        }
        else
        {
            // 重叠很深，只修正一部分（更保守的策略，修正比例更小）
            // 使用更小的修正比例（0.15），让它在后续帧中逐渐修正，避免突然飞出
            // 同时限制最大修正距离，确保不会一次性修正太多
            float correctionRatio = 0.15f;  // 从 0.3f 减小到 0.15f，修正更渐进
            return Mathf.Min(MAX_SINGLE_CORRECTION_DISTANCE, overlapDepth * correctionRatio + GEOMETRY_SURFACE_BACKOFF);
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
        
        // 检查是否有障碍物需要修改反弹系数
        float bounceFactor = geometryWallBounceFactor;
        if (hitInfo.collider != null)
        {
            BaseLevelHazard hazard = hitInfo.collider.GetComponent<BaseLevelHazard>();
            if (hazard != null)
            {
                float? modifiedFactor = hazard.ModifyBounceFactor(gameObject, geometrySpeed, geometryWallBounceFactor);
                if (modifiedFactor.HasValue)
                {
                    bounceFactor = modifiedFactor.Value;
                }
                
                // 处理冷却和特效
                hazard.HandleCollisionModification(gameObject);
            }
        }
        
        geometrySpeed *= bounceFactor;
        
        if (hitInfo.collider != null)
        {
            // 将几何墙体碰撞转换为统一的 CollisionEvent（用于伤害与特效系统）
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
        
        // ✅ 调试：输出击退信息
        if (showGeometryDebug || other.showGeometryDebug)
        {
            Debug.Log($"BallPhysics 碰撞击退 - {gameObject.name}(速度:{geometrySpeed:F2}) 撞击 {other.gameObject.name}(速度:{other.GetGeometrySpeed():F2}), 击退速度:{v2After.magnitude:F2}, 方向:{v2After.normalized}");
        }
        
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
        
        // ✅ 调试：输出速度更新信息
        if (showGeometryDebug && newSpeed > 0f)
        {
            Debug.Log($"BallPhysics {gameObject.name}: 更新速度 - 新速度:{newSpeed:F3}, 阈值:{geometryMinSpeedThreshold:F3}, 方向:{velocity.normalized}");
        }
        
        if (newSpeed <= geometryMinSpeedThreshold)
        {
            if (showGeometryDebug && velocity.sqrMagnitude > 0f)
            {
                Debug.LogWarning($"BallPhysics {gameObject.name}: 击退速度 {newSpeed:F3} 小于阈值 {geometryMinSpeedThreshold:F3}，被过滤");
            }
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
        
        // 将 3D 接触点与法线映射到 CollisionEvent：
        // - ContactPoint3D：真实 3D 碰撞点（用于特效定位）
        // - ContactPoint：XZ 平面投影（兼容旧 2D 逻辑）
        // - ContactNormal：XZ 平面法线（用于逻辑计算）
        Vector2 contactPoint2D = new Vector2(contactPoint.x, contactPoint.z);
        Vector2 contactNormal2D = new Vector2(normal.x, normal.z);

        CollisionEvent evt = new CollisionEvent
        {
            Source = gameObject,
            Target = target,
            ContactPoint = contactPoint2D,
            ContactNormal = contactNormal2D,
            Velocity = geometrySpeed,
            CollisionTime = Time.time,
            ContactPoint3D = contactPoint
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
    
    /// <summary>
    /// ✅ 根据自身碰撞器类型执行对应的 Cast 检测（BoxCast/CapsuleCast/SphereCast）
    /// </summary>
    private bool PerformColliderCast(Ray ray, float distance, LayerMask layerMask, out RaycastHit hit)
    {
        if (ballCollider3D == null)
        {
            hit = default;
            return false;
        }
        
        Vector3 lossyScale = transform.lossyScale;
        Quaternion rotation = transform.rotation;
        
        // 根据碰撞器类型选择对应的 Cast 方法
        if (ballCollider3D is BoxCollider boxCollider)
        {
            // BoxCollider：使用 BoxCast（精确匹配立方体形状）
            Vector3 halfExtents = Vector3.Scale(boxCollider.size * 0.5f, lossyScale);
            // BoxCast 从当前位置（考虑碰撞器的中心偏移）开始检测
            Vector3 castCenter = transform.TransformPoint(boxCollider.center);
            // 注意：BoxCast 的 center 参数是盒子中心，但检测是从这个位置沿着 direction 方向移动
            // 我们需要确保检测从当前物体的实际位置开始，所以使用当前位置作为起点
            return Physics.BoxCast(castCenter, halfExtents, geometryDirection, out hit, rotation, distance, layerMask, QueryTriggerInteraction.Ignore);
        }
        else if (ballCollider3D is CapsuleCollider capsuleCollider)
        {
            // CapsuleCollider：使用 CapsuleCast
            Vector3 point1 = transform.TransformPoint(capsuleCollider.center + Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius));
            Vector3 point2 = transform.TransformPoint(capsuleCollider.center - Vector3.up * (capsuleCollider.height * 0.5f - capsuleCollider.radius));
            float radius = capsuleCollider.radius * Mathf.Max(lossyScale.x, lossyScale.z);
            return Physics.CapsuleCast(point1, point2, radius, geometryDirection, out hit, distance, layerMask, QueryTriggerInteraction.Ignore);
        }
        else if (ballCollider3D is SphereCollider sphereCollider)
        {
            // SphereCollider：使用 SphereCast，直接读取半径
            float radius = sphereCollider.radius * Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
            return Physics.SphereCast(ray, radius, out hit, distance, layerMask, QueryTriggerInteraction.Ignore);
        }
        else
        {
            // 其他类型（MeshCollider 等）：使用 SphereCast，从 bounds 推断半径
            float radius = GetEffectiveGeometrySphereRadius();
            return Physics.SphereCast(ray, radius, out hit, distance, layerMask, QueryTriggerInteraction.Ignore);
        }
    }
    
    /// <summary>
    /// 获取有效的几何球体半径（用于 SphereCast 的兜底情况，如 MeshCollider）
    /// 注意：BoxCollider、CapsuleCollider、SphereCollider 都使用各自的 Cast 方法，不需要这个方法
    /// </summary>
    private float GetEffectiveGeometrySphereRadius()
    {
        // 如果手动设置了，直接返回
        if (geometrySphereRadius > 0f)
        {
            return geometrySphereRadius;
        }
        
        // 如果碰撞器未初始化，返回默认值
        if (ballCollider3D == null)
        {
            return 0.5f * Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);
        }
        
        Vector3 lossyScale = transform.lossyScale;
        
        // SphereCollider：直接使用半径（虽然 SphereCollider 会用 SphereCast，但这里作为兜底）
        if (ballCollider3D is SphereCollider sphereCollider)
        {
            float radius = sphereCollider.radius * Mathf.Max(lossyScale.x, lossyScale.y, lossyScale.z);
            geometrySphereRadius = radius;
            return radius;
        }
        
        // MeshCollider 或其他没有对应 Cast 方法的类型：使用 bounds 推断半径
        Bounds bounds = ballCollider3D.bounds;
        if (bounds.size.sqrMagnitude > 0.001f)
        {
            Vector3 scaledSize = Vector3.Scale(bounds.size, lossyScale);
            float inferredRadius = Mathf.Max(scaledSize.x, scaledSize.y, scaledSize.z) * 0.5f;
            geometrySphereRadius = inferredRadius;
            return inferredRadius;
        }
        
        // 如果推断失败，使用默认值
        float defaultRadius = 0.5f * Mathf.Max(lossyScale.x, lossyScale.z);
        geometrySphereRadius = defaultRadius;
        Debug.LogWarning($"BallPhysics: {gameObject.name} 无法从碰撞器推断半径，使用默认值 {defaultRadius:F3}");
        return defaultRadius;
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
        
        // ✅ 设置 3D 碰撞器（支持任意类型，会自动推断等效半径）
        ballCollider3D = GetComponent<Collider>();
        if (ballCollider3D == null)
        {
            // 如果没有碰撞器，添加一个默认的 SphereCollider
            ballCollider3D = gameObject.AddComponent<SphereCollider>();
            Debug.Log($"BallPhysics: {gameObject.name} 没有3D碰撞器，已添加默认 SphereCollider");
        }
        else
        {
            Debug.Log($"BallPhysics: {gameObject.name} 检测到3D碰撞器类型: {ballCollider3D.GetType().Name}，将自动推断等效半径");
        }
        
        // ✅ 自动推断几何模拟半径（支持多种碰撞器类型）
        // 如果 geometrySphereRadius <= 0，会在 GetEffectiveGeometrySphereRadius() 中自动推断
        if (geometrySphereRadius <= 0f)
        {
            // 预计算并缓存半径（避免每次调用都重新计算）
            GetEffectiveGeometrySphereRadius();
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
        
        // ✅ 地面对齐已迁移到 GroundAlignAnchor 组件，在 Awake() 中自动执行
        
        isInitialized = true;
        Debug.Log($"BallPhysics initialized for {gameObject.name} (3D kinematic mode, geometrySphereRadius={geometrySphereRadius:F3})");
    }
    
    // ✅ AlignToGround() 方法已移除，地面对齐功能已迁移到 GroundAlignAnchor 组件

    
    
    private void ApplyGeometryConfigFromData()
    {
        // 如果两份配置都为空，直接返回
        if (ballData == null && moveBallData == null)
        {
            return;
        }
        
        // 击退/发射使用的配置（优先使用 ballData）
        BallData knockbackData = ballData != null ? ballData : moveBallData;
        // 主动移动使用的配置（优先使用 moveBallData，若为空则退回到 knockbackData）
        BallData moveData = moveBallData != null ? moveBallData : knockbackData;
        
        // 缓存击退/发射配置
        knockbackMinSpeedThreshold = Mathf.Max(0.001f, knockbackData.geometryMinSpeedThreshold);
        knockbackHighSpeedPhaseDuration = Mathf.Max(0f, knockbackData.geometryHighSpeedPhaseDuration);
        knockbackHighPhaseDamping = Mathf.Max(0f, knockbackData.geometryHighPhaseDamping);
        knockbackLowPhaseDamping = Mathf.Max(0f, knockbackData.geometryLowPhaseDamping);
        knockbackWallBounceFactor = Mathf.Clamp01(knockbackData.geometryWallBounceFactor);
        knockbackBallBounceFactor = Mathf.Clamp01(knockbackData.geometryBallBounceFactor);
        knockbackKnockbackScale = Mathf.Max(0f, knockbackData.geometryKnockbackScale);
        
        // 缓存主动移动配置（通常阻尼会比击退小，以近似匀速）
        moveMinSpeedThreshold = Mathf.Max(0.001f, moveData.geometryMinSpeedThreshold);
        moveHighSpeedPhaseDuration = Mathf.Max(0f, moveData.geometryHighSpeedPhaseDuration);
        moveHighPhaseDamping = Mathf.Max(0f, moveData.geometryHighPhaseDamping);
        moveLowPhaseDamping = Mathf.Max(0f, moveData.geometryLowPhaseDamping);
        moveWallBounceFactor = Mathf.Clamp01(moveData.geometryWallBounceFactor);
        moveBallBounceFactor = Mathf.Clamp01(moveData.geometryBallBounceFactor);
        moveKnockbackScale = Mathf.Max(0f, moveData.geometryKnockbackScale);
        
        // 根据当前运动模式应用生效配置（默认是 Knockback，以兼容现有行为）
        ApplyActiveGeometryConfigForCurrentMode();
    }
    
    /// <summary>
    /// 根据当前几何运动模式，将缓存的配置应用到运行时参数上
    /// </summary>
    private void ApplyActiveGeometryConfigForCurrentMode()
    {
        switch (geometryMotionMode)
        {
            case GeometryMotionMode.ActiveMove:
                geometryMinSpeedThreshold = moveMinSpeedThreshold;
                geometryHighSpeedPhaseDuration = moveHighSpeedPhaseDuration;
                geometryHighPhaseDamping = moveHighPhaseDamping;
                geometryLowPhaseDamping = moveLowPhaseDamping;
                geometryWallBounceFactor = moveWallBounceFactor;
                geometryBallBounceFactor = moveBallBounceFactor;
                geometryKnockbackScale = moveKnockbackScale;
                break;
            
            case GeometryMotionMode.Knockback:
            default:
                geometryMinSpeedThreshold = knockbackMinSpeedThreshold;
                geometryHighSpeedPhaseDuration = knockbackHighSpeedPhaseDuration;
                geometryHighPhaseDamping = knockbackHighPhaseDamping;
                geometryLowPhaseDamping = knockbackLowPhaseDamping;
                geometryWallBounceFactor = knockbackWallBounceFactor;
                geometryBallBounceFactor = knockbackBallBounceFactor;
                geometryKnockbackScale = knockbackKnockbackScale;
                break;
        }
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
    
    /// <summary>
    /// 切换到「主动移动」运动模式（使用 moveBallData 配置）
    /// </summary>
    public void UseActiveMoveGeometryConfig()
    {
        geometryMotionMode = GeometryMotionMode.ActiveMove;
        ApplyActiveGeometryConfigForCurrentMode();
    }
    
    /// <summary>
    /// 切换到「击退/发射」运动模式（使用 ballData 配置）
    /// </summary>
    public void UseKnockbackGeometryConfig()
    {
        geometryMotionMode = GeometryMotionMode.Knockback;
        ApplyActiveGeometryConfigForCurrentMode();
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

