using UnityEngine;

/// <summary>
/// 运动距离预测器 - 基于物理参数预测球的运动距离
/// 
/// 【核心职责】：
/// - 根据力度和物理参数预测运动距离
/// - 考虑复杂的阻尼系统（线性、速度、时间阻尼）
/// - 提供缓存机制优化性能
/// - 控制更新频率避免过度计算
/// 
/// 【设计原则】：
/// - MonoBehaviour组件，可配置在Inspector中
/// - 高性能，支持缓存和频率控制
/// - 可配置，支持不同精度模式
/// - 可测试，逻辑清晰独立
/// </summary>
public class MovementDistancePredictor : MonoBehaviour
{
    #region 配置参数
    
    [Header("性能设置")]
    public float updateInterval = 0.1f;        // 更新间隔（秒）
    public float cacheThreshold = 0.5f;        // 缓存阈值（力度变化）
    public bool enableCaching = false;         // 临时禁用缓存强制重新计算
    
    [Header("物理模拟设置")]
    public float simulationTimeStep = 0.005f;  // 模拟时间步长（更小更精确）
    public int maxSimulationSteps = 1000;      // 最大模拟步数
    public float simulationStopThreshold = 0.01f; // 模拟停止阈值（更小）
    
    [Header("偏差校正设置")]
    [Tooltip("手动配置的偏差校正值，会加到每次预测结果中")]
    public float distanceOffset = 0f;          // 距离偏差校正值
    [Tooltip("是否启用偏差校正")]
    public bool enableDistanceOffset = true;   // 是否启用偏差校正
    
    #endregion
    
    #region 私有变量
    
    // 缓存系统
    private DistanceCache distanceCache;
    
    // 更新频率控制
    private float lastUpdateTime = 0f;
    private float lastPredictedDistance = 0f;
    
    // 调试信息
    private bool showDebugInfo = false;
    
    // 物理模拟相关
    private GameObject tempBall;
    private Rigidbody2D tempRb;
    private CircleCollider2D tempCollider;
    private PhysicsMaterial2D tempMaterial;
    
    // 动态物理系统相关（复制自BallPhysics）
    private float lastBounciness = 0f;
    private float lastDamping = 0f;
    private float lastPhysicsUpdateTime = 0f; // 重命名避免冲突
    private bool isMoving = false;
    private float ballStartTime = 0f;
    private BallData currentBallData; // 存储当前模拟使用的BallData
    
    #endregion
    
    #region Unity生命周期
    
    /// <summary>
    /// 初始化
    /// </summary>
    void Start()
    {
        // 初始化缓存
        distanceCache = new DistanceCache(cacheThreshold);
        
        if (showDebugInfo)
        {
            Debug.Log("[MovementDistancePredictor] 初始化完成");
        }
    }
    
    #endregion
    
    #region 主要接口
    
    /// <summary>
    /// 预测运动距离（主要接口）
    /// </summary>
    /// <param name="initialVelocity">初始速度（力度）</param>
    /// <param name="ballData">球的物理数据</param>
    /// <returns>预测的运动距离</returns>
    public float PredictMovementDistance(float initialVelocity, BallData ballData)
    {
        Debug.Log($"[MovementDistancePredictor] 开始预测: 力度={initialVelocity:F2}, BallData={(ballData != null ? "有效" : "null")}");
        
        if (ballData == null)
        {
            Debug.LogError("[MovementDistancePredictor] BallData为null，返回0距离");
            return 0f;
        }
        
        // 检查更新频率
        if (!ShouldUpdate())
        {
            Debug.Log($"[MovementDistancePredictor] 使用上次结果: 距离={lastPredictedDistance:F2}");
            return lastPredictedDistance;
        }
        
        float predictedDistance;
        
        // 尝试使用缓存
        if (enableCaching && distanceCache.IsValid(initialVelocity))
        {
            predictedDistance = distanceCache.GetCachedDistance();
            Debug.Log($"[MovementDistancePredictor] 使用缓存: 力度={initialVelocity:F2}, 距离={predictedDistance:F2}");
        }
        else
        {
            // 重新计算
            Debug.Log($"[MovementDistancePredictor] 开始重新计算: 力度={initialVelocity:F2}");
            predictedDistance = CalculateMovementDistance(initialVelocity, ballData);
            
            // 更新缓存
            if (enableCaching)
            {
                distanceCache.UpdateCache(initialVelocity, predictedDistance);
            }
            
            Debug.Log($"[MovementDistancePredictor] 重新计算完成: 力度={initialVelocity:F2}, 距离={predictedDistance:F2}");
        }
        
        // 更新状态
        lastUpdateTime = Time.time;
        lastPredictedDistance = predictedDistance;
        
        return predictedDistance;
    }
    
    /// <summary>
    /// 强制重新计算（跳过缓存和频率限制）
    /// </summary>
    public float ForcePredictMovementDistance(float initialVelocity, BallData ballData)
    {
        float predictedDistance = CalculateMovementDistance(initialVelocity, ballData);
        
        // 更新缓存
        if (enableCaching)
        {
            distanceCache.UpdateCache(initialVelocity, predictedDistance);
        }
        
        // 更新状态
        lastUpdateTime = Time.time;
        lastPredictedDistance = predictedDistance;
        
        return predictedDistance;
    }
    
    #endregion
    
    #region 核心计算方法
    
    /// <summary>
    /// 计算运动距离（物理模拟）
    /// </summary>
    private float CalculateMovementDistance(float initialVelocity, BallData ballData)
    {
        // 总是进行物理模拟，即使力度为0也要验证物理系统
        return PhysicsSimulationPredict(initialVelocity, ballData);
    }
    
    /// <summary>
    /// 物理模拟预测（方案A：逐帧微模拟）
    /// </summary>
    private float PhysicsSimulationPredict(float initialVelocity, BallData ballData)
    {
        try
        {
            // 1. 状态克隆：创建临时球体
            CreateTempBall(ballData);
            
            // 2. 设置初始状态
            Vector2 initialDirection = Vector2.right;
            Vector2 velocity = initialDirection * initialVelocity;
            tempRb.linearVelocity = velocity;
            
            // 3. 固定步推进：逐帧微模拟
            float totalDistance = SimulatePhysicsMovementPlanA();
            
            // 4. 清理临时对象
            DestroyTempBall();
            
            // 应用偏差校正
            float finalDistance = ApplyDistanceOffset(totalDistance);
            
            Debug.Log($"[方案A] 初始速度: {initialVelocity:F2}, 模拟距离: {totalDistance:F2}, 校正后距离: {finalDistance:F2}, 阻尼: {ballData.linearDamping:F3}");
            
            return finalDistance;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MovementDistancePredictor] 物理模拟失败: {e.Message}");
            DestroyTempBall(); // 确保清理
            return 0f;
        }
    }
    
    /// <summary>
    /// 创建临时球体
    /// </summary>
    private void CreateTempBall(BallData ballData)
    {
        // 创建临时GameObject
        tempBall = new GameObject("TempPhysicsBall");
        tempBall.transform.position = Vector3.zero;
        
        // 添加到场景中，确保物理引擎能处理它
        tempBall.transform.SetParent(null);
        tempBall.SetActive(true);
        
        // 添加Rigidbody2D
        tempRb = tempBall.AddComponent<Rigidbody2D>();
        tempRb.mass = ballData.mass;
        tempRb.gravityScale = 0f;
        tempRb.linearDamping = ballData.linearDamping;
        tempRb.angularDamping = 0f;
        tempRb.freezeRotation = true;
        
        // 添加CircleCollider2D
        tempCollider = tempBall.AddComponent<CircleCollider2D>();
        tempCollider.radius = 0.5f; // 默认半径
        tempCollider.isTrigger = false;
        
        // 创建物理材质
        tempMaterial = new PhysicsMaterial2D();
        tempMaterial.bounciness = ballData.bounceDamping;
        tempMaterial.friction = ballData.friction;
        tempCollider.sharedMaterial = tempMaterial;
        
        // 初始化动态参数缓存（复制自BallPhysics）
        lastBounciness = ballData.bounceDamping;
        lastDamping = ballData.linearDamping;
        lastPhysicsUpdateTime = 0f;
        isMoving = false;
        ballStartTime = 0f;
        currentBallData = ballData; // 存储当前BallData
        
        Debug.Log($"[方案A] 创建临时球体 - 位置: {tempBall.transform.position}, 质量: {tempRb.mass}, 阻尼: {tempRb.linearDamping}");
    }
    
    /// <summary>
    /// 方案A：固定步推进 - 逐帧微模拟
    /// </summary>
    private float SimulatePhysicsMovementPlanA()
    {
        float totalDistance = 0f;
        Vector3 lastPosition = tempBall.transform.position;
        float currentTime = 0f;
        
        Debug.Log($"[方案A] 开始模拟 - 初始位置: {lastPosition}, 初始速度: {tempRb.linearVelocity}");
        
        // 初始化物理状态（发射时设置固定参数）
        SetFixedPhysicsForLaunch();
        
        for (int step = 0; step < maxSimulationSteps; step++)
        {
            // 更新模拟时间
            currentTime += simulationTimeStep;
            
            // 检查运动状态
            CheckMovementForSimulation(currentTime);
            
            // 更新动态物理参数（调用现有物理系统）
            UpdateDynamicPhysicsForSimulation(currentTime);
            
            // 记录模拟前的状态
            Vector3 beforePos = tempBall.transform.position;
            Vector2 beforeVel = tempRb.linearVelocity;
            
            // 手动模拟物理（因为Physics2D.Simulate可能不工作）
            SimulatePhysicsManually(simulationTimeStep);
            
            // 记录模拟后的状态
            Vector3 afterPos = tempBall.transform.position;
            Vector2 afterVel = tempRb.linearVelocity;
            
            // 累加弧长作为路程：D += |∆pos|
            float stepDistance = Vector3.Distance(beforePos, afterPos);
            totalDistance += stepDistance;
            lastPosition = afterPos;
            
            // 每10步输出一次调试信息
            if (step % 10 == 0 || step < 5)
            {
                Debug.Log($"[方案A] 第{step}步 - 位置变化: {beforePos} → {afterPos}, 距离: {stepDistance:F3}, 速度变化: {beforeVel} → {afterVel}, 总距离: {totalDistance:F3}");
            }
            
            // 停止条件：|v| < vMin
            if (tempRb.linearVelocity.magnitude < simulationStopThreshold)
            {
                Debug.Log($"[方案A] 第{step}步停止，总距离: {totalDistance:F2}, 最终速度: {tempRb.linearVelocity.magnitude:F3}");
                break;
            }
        }
        
        Debug.Log($"[方案A] 模拟结束 - 总距离: {totalDistance:F3}, 最终位置: {tempBall.transform.position}, 最终速度: {tempRb.linearVelocity}");
        return totalDistance;
    }
    
    /// <summary>
    /// 发射时设置固定的物理参数（复制自BallPhysics.SetFixedPhysicsForLaunch）
    /// </summary>
    private void SetFixedPhysicsForLaunch()
    {
        BallData ballData = GetCurrentBallData();
        if (ballData == null) return;
        
        if (tempMaterial != null)
        {
            // 使用基础反弹系数，不使用动态值
            tempMaterial.bounciness = ballData.bounceDamping;
            tempMaterial.friction = ballData.friction;
        }
        
        // 使用基础阻尼，不使用动态值
        if (tempRb != null)
        {
            tempRb.linearDamping = ballData.linearDamping;
            // 确保刚体处于正确状态
            tempRb.angularVelocity = 0f;
        }
        
        // 重置动态参数缓存，避免动态系统干扰
        lastBounciness = ballData.bounceDamping;
        lastDamping = ballData.linearDamping;
        lastPhysicsUpdateTime = 0f;
        
        // 重置时间阻尼状态
        isMoving = false;
        ballStartTime = 0f;
    }
    
    /// <summary>
    /// 手动模拟物理（替代Physics2D.Simulate）
    /// </summary>
    private void SimulatePhysicsManually(float timeStep)
    {
        if (tempRb == null) return;
        
        // 获取当前速度
        Vector2 currentVelocity = tempRb.linearVelocity;
        
        // 应用阻尼：v = v * (1 - damping * timeStep)
        Vector2 dampedVelocity = currentVelocity * (1f - tempRb.linearDamping * timeStep);
        
        // 确保速度不会变为负值
        if (dampedVelocity.magnitude < 0.001f)
        {
            dampedVelocity = Vector2.zero;
        }
        
        // 更新位置：position = position + velocity * timeStep
        Vector3 newPosition = tempBall.transform.position + (Vector3)(currentVelocity * timeStep);
        tempBall.transform.position = newPosition;
        
        // 更新速度
        tempRb.linearVelocity = dampedVelocity;
    }
    
    /// <summary>
    /// 销毁临时球体
    /// </summary>
    private void DestroyTempBall()
    {
        if (tempBall != null)
        {
            DestroyImmediate(tempBall);
            tempBall = null;
            tempRb = null;
            tempCollider = null;
            tempMaterial = null;
        }
    }
    
    /// <summary>
    /// 检查运动状态（复制自BallPhysics.CheckMovement）
    /// </summary>
    private void CheckMovementForSimulation(float currentTime)
    {
        float currentSpeed = tempRb.linearVelocity.magnitude;
        
        // 确保球不会旋转
        if (tempRb.angularVelocity != 0f)
        {
            tempRb.angularVelocity = 0f;
        }
        
        // 记录运动状态
        BallData ballData = GetCurrentBallData();
        float stopThreshold = ballData != null ? ballData.stopThreshold : 0.5f;
        if (currentSpeed > stopThreshold)
        {
            // 球在运动，记录开始运动时间
            if (!isMoving)
            {
                isMoving = true;
                ballStartTime = currentTime;
                if (showDebugInfo)
                {
                    Debug.Log($"[物理模拟] 球开始运动，记录时间 {ballStartTime:F2}");
                }
            }
        }
        else
        {
            // 球停止运动
            if (isMoving)
            {
                isMoving = false;
                if (showDebugInfo)
                {
                    Debug.Log($"[物理模拟] 球停止运动，运动时长: {currentTime - ballStartTime:F2}s");
                }
            }
        }
    }
    
    /// <summary>
    /// 更新动态物理参数（复制自BallPhysics.UpdateDynamicPhysics）
    /// </summary>
    private void UpdateDynamicPhysicsForSimulation(float currentTime)
    {
        // 获取当前的BallData（从临时变量中获取）
        BallData ballData = GetCurrentBallData();
        if (ballData == null) return;
        
        // 检查更新间隔
        if (currentTime - lastPhysicsUpdateTime < ballData.updateInterval)
        {
            return;
        }
        
        float currentSpeed = tempRb.linearVelocity.magnitude;
        float normalizedSpeed = Mathf.Clamp01(currentSpeed / ballData.maxSpeed);
        
        // 计算动态弹性系数（使用真实的AnimationCurve）
        float targetBounciness = ballData.speedToBounciness.Evaluate(normalizedSpeed);
        targetBounciness = Mathf.Lerp(ballData.minBounciness, ballData.maxBounciness, targetBounciness);
        
        // 计算动态阻尼（使用真实的AnimationCurve）
        float targetDamping = ballData.speedToDamping.Evaluate(normalizedSpeed);
        targetDamping = Mathf.Lerp(ballData.minDamping, ballData.maxDamping, targetDamping);
        
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
                targetDamping += timeDamping;
                
                if (showDebugInfo)
                {
                    Debug.Log($"[物理模拟] 时间阻尼 - 运动时长: {timeSinceStart:F2}s, 时间阻尼: {timeDamping:F2}, 总阻尼: {targetDamping:F2}");
                }
            }
        }
        
        // 检查参数变化是否超过阈值
        bool bouncinessChanged = Mathf.Abs(targetBounciness - lastBounciness) > ballData.updateThreshold;
        bool dampingChanged = Mathf.Abs(targetDamping - lastDamping) > ballData.updateThreshold;
        
        // 更新弹性系数
        if (bouncinessChanged && tempMaterial != null)
        {
            tempMaterial.bounciness = targetBounciness;
            lastBounciness = targetBounciness;
            
            if (showDebugInfo)
            {
                Debug.Log($"[物理模拟] 更新弹性: {targetBounciness:F3}");
            }
        }
        
        // 更新阻尼
        if (dampingChanged && tempRb != null)
        {
            tempRb.linearDamping = targetDamping;
            lastDamping = targetDamping;
            
            if (showDebugInfo)
            {
                Debug.Log($"[物理模拟] 更新阻尼: {targetDamping:F3}");
            }
        }
        
        // 更新最后更新时间
        lastPhysicsUpdateTime = currentTime;
    }
    
    /// <summary>
    /// 获取当前模拟使用的BallData
    /// </summary>
    private BallData GetCurrentBallData()
    {
        return currentBallData;
    }
    
    /// <summary>
    /// 应用偏差校正
    /// </summary>
    private float ApplyDistanceOffset(float originalDistance)
    {
        if (!enableDistanceOffset)
        {
            return originalDistance;
        }
        
        float correctedDistance = originalDistance + distanceOffset;
        
        // 确保校正后的距离不为负数
        if (correctedDistance < 0f)
        {
            correctedDistance = 0f;
        }
        
        if (distanceOffset != 0f)
        {
            Debug.Log($"[偏差校正] 原始距离: {originalDistance:F2}, 校正值: {distanceOffset:F2}, 校正后: {correctedDistance:F2}");
        }
        
        return correctedDistance;
    }
    
    #endregion
    
    #region 辅助方法
    
    /// <summary>
    /// 检查是否应该更新（频率控制）
    /// </summary>
    private bool ShouldUpdate()
    {
        return Time.time - lastUpdateTime > updateInterval;
    }
    
    /// <summary>
    /// 设置调试信息显示
    /// </summary>
    public void SetDebugInfo(bool enabled)
    {
        showDebugInfo = enabled;
    }
    
    /// <summary>
    /// 清除缓存
    /// </summary>
    public void ClearCache()
    {
        if (distanceCache != null)
        {
            distanceCache.Clear();
        }
    }
    
    /// <summary>
    /// 设置偏差校正值
    /// </summary>
    public void SetDistanceOffset(float offset)
    {
        distanceOffset = offset;
        Debug.Log($"[MovementDistancePredictor] 设置偏差校正值: {offset:F2}");
    }
    
    /// <summary>
    /// 启用/禁用偏差校正
    /// </summary>
    public void SetDistanceOffsetEnabled(bool enabled)
    {
        enableDistanceOffset = enabled;
        Debug.Log($"[MovementDistancePredictor] 偏差校正: {(enabled ? "启用" : "禁用")}");
    }
    
    /// <summary>
    /// 在Inspector中显示调试信息
    /// </summary>
    [ContextMenu("测试预测")]
    void TestPrediction()
    {
        if (Application.isPlaying)
        {
            // 使用默认参数测试
            float testForce = 15f;
            var testBallData = ScriptableObject.CreateInstance<BallData>();
            testBallData.linearDamping = 0.1f;
            testBallData.friction = 0.1f;
            testBallData.maxSpeed = 25f;
            
            float distance = PredictMovementDistance(testForce, testBallData);
            Debug.Log($"[MovementDistancePredictor] 测试预测 - 力度:{testForce}, 距离:{distance:F2}");
        }
    }
    
    /// <summary>
    /// 详细调试预测过程
    /// </summary>
    [ContextMenu("详细调试预测")]
    void DebugPrediction()
    {
        if (Application.isPlaying)
        {
            showDebugInfo = true;
            
            // 测试不同力度
            float[] testForces = {5f, 10f, 15f, 20f, 25f};
            var testBallData = ScriptableObject.CreateInstance<BallData>();
            testBallData.linearDamping = 0.1f;
            testBallData.friction = 0.1f;
            testBallData.maxSpeed = 25f;
            
            Debug.Log("=== 详细预测调试 ===");
            foreach (float force in testForces)
            {
                float distance = ForcePredictMovementDistance(force, testBallData);
                Debug.Log($"力度: {force:F1} → 预测距离: {distance:F2}");
            }
            
            showDebugInfo = false;
        }
    }
    
    /// <summary>
    /// 测试方案A物理模拟
    /// </summary>
    [ContextMenu("测试方案A物理模拟")]
    void TestPhysicsSimulation()
    {
        if (Application.isPlaying)
        {
            // 测试方案A物理模拟
            float testForce = 15f;
            var testBallData = ScriptableObject.CreateInstance<BallData>();
            testBallData.linearDamping = 0.1f;
            testBallData.friction = 0.1f;
            testBallData.maxSpeed = 25f;
            testBallData.stopThreshold = 0.5f;
            testBallData.updateInterval = 0.02f;
            testBallData.updateThreshold = 0.1f;
            testBallData.enableTimeDamping = true;
            testBallData.timeDampingStartTime = 2.0f;
            testBallData.timeDampingRate = 0.2f;
            testBallData.maxTimeDamping = 1.5f;
            testBallData.minDamping = 0.1f;
            testBallData.maxDamping = 0.8f;
            testBallData.minBounciness = 0.3f;
            testBallData.maxBounciness = 1.0f;
            
            Debug.Log("=== 方案A物理模拟测试 ===");
            
            float distance = ForcePredictMovementDistance(testForce, testBallData);
            Debug.Log($"方案A物理模拟 - 力度: {testForce:F1} → 距离: {distance:F2}");
        }
    }
    
    #endregion
    
    #region 缓存类
    
    /// <summary>
    /// 距离缓存类
    /// </summary>
    private class DistanceCache
    {
        private float lastVelocity = -1f;
        private float lastDistance = 0f;
        private float threshold;
        
        public DistanceCache(float threshold = 0.5f)
        {
            this.threshold = threshold;
        }
        
        public bool IsValid(float currentVelocity)
        {
            return Mathf.Abs(currentVelocity - lastVelocity) <= threshold;
        }
        
        public float GetCachedDistance()
        {
            return lastDistance;
        }
        
        public void UpdateCache(float velocity, float distance)
        {
            lastVelocity = velocity;
            lastDistance = distance;
        }
        
        public void Clear()
        {
            lastVelocity = -1f;
            lastDistance = 0f;
        }
    }
    
    #endregion
    
    #region 公共属性
    
    /// <summary>
    /// 获取上次预测的距离
    /// </summary>
    public float LastPredictedDistance => lastPredictedDistance;
    
    /// <summary>
    /// 获取上次更新时间
    /// </summary>
    public float LastUpdateTime => lastUpdateTime;
    
    /// <summary>
    /// 缓存是否有效
    /// </summary>
    public bool IsCacheValid => distanceCache.IsValid(lastPredictedDistance);
    
    #endregion
}
