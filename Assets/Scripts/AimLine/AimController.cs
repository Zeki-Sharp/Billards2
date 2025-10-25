using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄准线控制器 - 事件驱动的瞄准线系统
/// 
/// 【核心职责】：
/// - 管理瞄准线显示和隐藏
/// - 响应蓄力事件控制UI状态
/// - 处理瞄准方向计算和更新
/// - 协调反射计算器和渲染器
/// 
/// 【设计原则】：
/// - 事件驱动架构，松耦合通信
/// - 专注瞄准线逻辑，不处理蓄力计算
/// - 通过GameEventBus响应蓄力事件
/// - 可独立测试和扩展
/// </summary>
public class AimController : MonoBehaviour
{
    // 常量定义
    private const float DEFAULT_BALL_RADIUS = 0.5f;
    
    [Header("瞄准设置")]
    
    
    [Header("球体设置")]
    public PlayerCore playerCore; // 玩家核心引用
    
    
    [Header("相机设置")]
    public Camera targetCamera; // 目标相机，如果为空则使用主相机
    
    [Header("反射计算器")]
    public AimLineReflectionCalculator reflectionCalculator; // 反射计算器引用
    
    [Header("渲染器")]
    public AimLineRenderer aimLineRenderer; // 瞄准线渲染器引用
    
    [Header("距离预测设置")]
    [Tooltip("是否启用距离预测和落点显示")]
    public bool enableDistancePrediction = true;
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息
    
    // 私有变量
    private Camera cam;
    private bool isVisible = false; // 是否显示瞄准线
    private Vector2 aimDirection;
    
    // 距离预测相关
    private MovementDistancePredictor distancePredictor;
    private AimLineLandingPointManager landingPointManager;
    private ChargeSystem chargeSystem;
    private float lastPredictedDistance = 0f;
    
    void Start()
    {
        InitializeController();
        
        // 订阅蓄力事件
        GameEventBus.OnChargingStarted += ShowChargingUI;
        GameEventBus.OnChargingStopped += HideChargingUI;
        GameEventBus.OnChargingReset += HideChargingUI;
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力事件
        GameEventBus.OnChargingStarted -= ShowChargingUI;
        GameEventBus.OnChargingStopped -= HideChargingUI;
        GameEventBus.OnChargingReset -= HideChargingUI;
        
        // 取消订阅蓄力进度事件
        GameEventBus.OnChargingProgressChanged -= UpdateChargingProgress;
        GameEventBus.OnForceChanged -= UpdateForceDisplay;
    }
    
    void Update()
    {
        UpdateAimLine();
        UpdateAimDirection();
    }
    
    void InitializeController()
    {
        // 获取相机
        cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            Debug.LogError("AimController: 找不到相机！请设置targetCamera或确保有MainCamera标签的相机");
            return;
        }
        
        // 验证必需组件
        if (playerCore == null)
        {
            Debug.LogError("AimController: PlayerCore 未设置！请在Inspector中设置PlayerCore引用");
            return;
        }
        
        
        
        // 初始化组件
        InitializeReflectionCalculator();
        InitializeRenderer();
        InitializeDistancePrediction();
        
        if (showDebugInfo)
        {
            Debug.Log("AimController: 初始化完成");
        }
    }
    
    
    void SetupAimLine()
    {
        // 初始化渲染器
        InitializeRenderer();
    }
    
    
    void InitializeReflectionCalculator()
    {
        // 如果没有设置反射计算器，尝试自动查找
        if (reflectionCalculator == null)
        {
            reflectionCalculator = GetComponent<AimLineReflectionCalculator>();
            if (reflectionCalculator == null)
            {
                reflectionCalculator = gameObject.AddComponent<AimLineReflectionCalculator>();
            }
        }
        
        if (reflectionCalculator != null && showDebugInfo)
        {
            Debug.Log("AimController: 反射计算器初始化完成");
        }
        else
        {
            Debug.LogWarning("AimController: 反射计算器初始化失败");
        }
    }
    
    
    void InitializeRenderer()
    {
        // 如果没有设置渲染器，尝试自动查找
        if (aimLineRenderer == null)
        {
            aimLineRenderer = GetComponent<AimLineRenderer>();
            if (aimLineRenderer == null)
            {
                aimLineRenderer = gameObject.AddComponent<AimLineRenderer>();
            }
        }
        
        if (aimLineRenderer != null && showDebugInfo)
        {
            Debug.Log("AimController: 渲染器初始化完成");
        }
        else if (aimLineRenderer == null)
        {
            Debug.LogWarning("AimController: 渲染器初始化失败");
        }
    }
    
    /// <summary>
    /// 初始化距离预测组件
    /// </summary>
    void InitializeDistancePrediction()
    {
        if (!enableDistancePrediction)
        {
            if (showDebugInfo)
            {
                Debug.Log("AimController: 距离预测已禁用");
            }
            return;
        }
        
        // 获取距离预测器组件
        distancePredictor = GetComponent<MovementDistancePredictor>();
        if (distancePredictor == null)
        {
            Debug.LogWarning("AimController: 找不到MovementDistancePredictor组件，距离预测功能将不可用");
        }
        
        // 获取落点管理器组件
        landingPointManager = GetComponent<AimLineLandingPointManager>();
        if (landingPointManager == null)
        {
            Debug.LogWarning("AimController: 找不到AimLineLandingPointManager组件，落点显示功能将不可用");
        }
        
        // 获取蓄力系统组件（用于获取当前力度）
        if (playerCore != null)
        {
            chargeSystem = playerCore.GetComponent<ChargeSystem>();
            if (chargeSystem == null)
            {
                Debug.LogWarning("AimController: 找不到ChargeSystem组件，无法获取当前力度");
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"AimController: 距离预测初始化完成 - 预测器: {(distancePredictor != null ? "已找到" : "未找到")}, 落点管理器: {(landingPointManager != null ? "已找到" : "未找到")}, 蓄力系统: {(chargeSystem != null ? "已找到" : "未找到")}");
        }
    }
    
    /// <summary>
    /// 更新移动距离预测
    /// </summary>
    void UpdateMovementPrediction()
    {
        if (!enableDistancePrediction || distancePredictor == null || chargeSystem == null)
        {
            return;
        }
        
        // 获取当前力度并转换为速度
        float currentForce = chargeSystem.GetCurrentForce();
        // 力度转换为速度：假设力度直接对应速度（需要根据实际物理调整）
        float initialVelocity = currentForce;
        
        // 获取球的物理数据
        BallData ballData = null;
        if (playerCore != null)
        {
            BallPhysics ballPhysics = playerCore.GetComponent<BallPhysics>();
            if (ballPhysics != null)
            {
                ballData = ballPhysics.ballData;
            }
        }
        
        if (ballData == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("AimController: 无法获取BallData，跳过距离预测");
            }
            return;
        }
        
        // 预测移动距离
        float predictedDistance = distancePredictor.PredictMovementDistance(initialVelocity, ballData);
        lastPredictedDistance = predictedDistance;
        
        if (showDebugInfo)
        {
            Debug.Log($"[AimController] 距离预测 - 力度: {currentForce:F2}, 预测距离: {predictedDistance:F2}, BallData阻尼: {(ballData != null ? ballData.linearDamping.ToString("F3") : "null")}");
        }
    }
    
    
    /// <summary>
    /// 更新瞄准方向（根据蓄力模式）
    /// </summary>
    void UpdateAimDirection()
    {
        if (playerCore == null || cam == null) return;
        
        // 根据蓄力系统的模式获取瞄准方向
        if (chargeSystem != null)
        {
            // 使用ChargeSystem的GetLaunchDirection方法获取正确的发射方向
            Vector2 direction = chargeSystem.GetLaunchDirection(playerCore.transform.position);
            
            if (direction.magnitude > 0.1f) // 避免零向量
            {
                aimDirection = direction.normalized;
            }
        }
        else
        {
            // 如果没有ChargeSystem，使用默认逻辑（从球指向鼠标）
            Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            Vector3 mouseScreenPos = new Vector3(mousePos.x, mousePos.y, 0f);
            Vector3 mouseWorldPos = GetMouseWorldPosition(mouseScreenPos);
            Vector3 direction = mouseWorldPos - playerCore.transform.position;
            
            if (direction.magnitude > 0.1f)
            {
                aimDirection = direction.normalized;
            }
        }
    }
    
    Vector3 GetMouseWorldPosition(Vector3 mouseScreenPos)
    {
        // 使用稳定的2D世界坐标转换方法
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float cameraSize = cam.orthographicSize;
        float aspectRatio = (float)screenWidth / screenHeight;
        
        float worldX = (mouseScreenPos.x / screenWidth - 0.5f) * cameraSize * aspectRatio * 2f;
        float worldY = (mouseScreenPos.y / screenHeight - 0.5f) * cameraSize * 2f;
        
        return new Vector3(worldX, worldY, 0f);
    }
    
    /// <summary>
    /// 显示蓄力UI（由蓄力事件调用）
    /// </summary>
    public void ShowChargingUI()
    {
        isVisible = true;
        
        
        // 订阅蓄力进度事件
        GameEventBus.OnChargingProgressChanged += UpdateChargingProgress;
        GameEventBus.OnForceChanged += UpdateForceDisplay;
        
        if (showDebugInfo)
        {
            Debug.Log("AimController: 已订阅蓄力进度事件");
        }
        
        // 触发蓄力开始特效
        if (playerCore != null)
        {
            playerCore.gameObject.PublishEffect("Charge", playerCore.transform.position);
        }
    }
    
    /// <summary>
    /// 更新蓄力进度显示（由ChargeSystem事件调用）
    /// </summary>
    /// <param name="chargingProgress">蓄力进度 (0-1)</param>
    void UpdateChargingProgress(float chargingProgress)
    {
        if (!isVisible) return;
        
    }
    
    /// <summary>
    /// 更新力度显示（由ChargeSystem事件调用）
    /// </summary>
    /// <param name="currentForce">当前力度</param>
    void UpdateForceDisplay(float currentForce)
    {
        if (!isVisible) return;
        
        // 这里可以添加力度相关的UI更新逻辑
        // 比如力度指示器、颜色变化等
    }
    
    /// <summary>
    /// 隐藏蓄力UI（由蓄力事件调用）
    /// </summary>
    public void HideChargingUI()
    {
        isVisible = false;
        
        // 隐藏落点
        if (landingPointManager != null)
        {
            landingPointManager.HideLandingPoint();
        }
        
        // 取消订阅蓄力进度事件
        GameEventBus.OnChargingProgressChanged -= UpdateChargingProgress;
        GameEventBus.OnForceChanged -= UpdateForceDisplay;
        
    }
    
    void UpdateAimLine()
    {
        // 确保渲染器已初始化
        if (aimLineRenderer == null)
        {
            SetupAimLine();
            if (aimLineRenderer == null)
            {
                Debug.LogError("AimController: 无法初始化渲染器，跳过更新");
                return;
            }
        }
        
        // 检查白球是否在移动
        if (playerCore == null || playerCore.IsPhysicsMoving())
        {
            aimLineRenderer.ClearAllLines();
            return;
        }
        
        // 只有在显示状态时才显示瞄准线
        if (!isVisible)
        {
            aimLineRenderer.ClearAllLines();
            return;
        }
        
        Vector3 startPos = playerCore.transform.position;
        
        // 使用反射计算器计算路径
        if (reflectionCalculator != null)
        {
            // 获取白球半径（从BallData获取，已包含实际的世界空间半径）
            float ballRadius = DEFAULT_BALL_RADIUS; // 默认半径
            if (playerCore != null)
            {
                BallPhysics ballPhysics = playerCore.GetComponent<BallPhysics>();
                if (ballPhysics != null)
                {
                    ballRadius = ballPhysics.GetRadius();
                }
            }
            
            List<Vector3> pathPoints = reflectionCalculator.CalculateReflectionPath(startPos, aimDirection, ballRadius);
            
            // 更新距离预测
            UpdateMovementPrediction();
            
            // 根据距离预测决定是否显示落点并截断瞄准线
            if (enableDistancePrediction && distancePredictor != null && landingPointManager != null)
            {
                // 计算瞄准线总长度
                float aimLineLength = aimLineRenderer.GetPathTotalLength(pathPoints);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[AimController] 落点判断 - 预测距离: {lastPredictedDistance:F2}, 瞄准线长度: {aimLineLength:F2}, 条件: {lastPredictedDistance <= aimLineLength && lastPredictedDistance > 0}");
                }
                
                // 判断是否显示落点（去掉各种限制，只保留基本条件）
                if (lastPredictedDistance <= aimLineLength && lastPredictedDistance > 0)
                {
                    // 截断路径到预测距离并渲染
                    List<Vector3> truncatedPath = aimLineRenderer.TruncatePathAtDistance(pathPoints, lastPredictedDistance);
                    aimLineRenderer.RenderSegmentedAimLine(truncatedPath);
                    
                    // 显示落点在截断位置
                    if (truncatedPath.Count > 0)
                    {
                        Vector3 landingPosition = truncatedPath[truncatedPath.Count - 1];
                        landingPointManager.ShowLandingPoint(landingPosition);
                        
                        if (showDebugInfo)
                        {
                            Debug.Log($"[AimController] 显示落点 - 位置: {landingPosition}");
                        }
                    }
                }
                else
                {
                    // 显示完整瞄准线，隐藏落点
                    aimLineRenderer.RenderSegmentedAimLine(pathPoints);
                    landingPointManager.HideLandingPoint();
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[AimController] 隐藏落点 - 预测距离: {lastPredictedDistance:F2}, 瞄准线长度: {aimLineLength:F2}");
                    }
                }
            }
            else
            {
                // 距离预测未启用或组件缺失，使用原始逻辑
                aimLineRenderer.RenderSegmentedAimLine(pathPoints);
                
                if (showDebugInfo)
                {
                    Debug.Log($"[AimController] 距离预测未启用 - enableDistancePrediction: {enableDistancePrediction}, distancePredictor: {(distancePredictor != null)}, landingPointManager: {(landingPointManager != null)}");
                }
            }
        }
        else
        {
            // 反射计算器未初始化时，不显示瞄准线
            aimLineRenderer.ClearAllLines();
            if (showDebugInfo)
            {
                Debug.LogWarning("AimController: 反射计算器未初始化，无法显示瞄准线");
            }
        }
    }
    
    
    // 公共方法
    
    /// <summary>
    /// 是否正在显示瞄准线（与蓄力状态关联）
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }
    
    /// <summary>
    /// 获取当前预测的移动距离
    /// </summary>
    /// <returns>预测距离</returns>
    public float GetPredictedDistance()
    {
        return lastPredictedDistance;
    }
    
    /// <summary>
    /// 设置是否启用距离预测
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetDistancePredictionEnabled(bool enable)
    {
        enableDistancePrediction = enable;
        
        if (!enable && landingPointManager != null)
        {
            landingPointManager.HideLandingPoint();
        }
    }
    
    /// <summary>
    /// 获取距离预测器组件
    /// </summary>
    /// <returns>距离预测器</returns>
    public MovementDistancePredictor GetDistancePredictor()
    {
        return distancePredictor;
    }
    
    /// <summary>
    /// 获取落点管理器组件
    /// </summary>
    /// <returns>落点管理器</returns>
    public AimLineLandingPointManager GetLandingPointManager()
    {
        return landingPointManager;
    }
    
    
    // 反射相关方法
    
    public AimLineReflectionCalculator GetReflectionCalculator()
    {
        return reflectionCalculator;
    }
    
    
    public string GetReflectionStats()
    {
        if (reflectionCalculator != null)
        {
            return reflectionCalculator.GetReflectionStats();
        }
        return "反射计算器未初始化";
    }
    
    // 渲染器相关方法
    public AimLineRenderer GetRenderer()
    {
        return aimLineRenderer;
    }
    
    public string GetRenderStats()
    {
        if (aimLineRenderer != null)
        {
            return aimLineRenderer.GetRenderStats();
        }
        return "渲染器未初始化";
    }
}
