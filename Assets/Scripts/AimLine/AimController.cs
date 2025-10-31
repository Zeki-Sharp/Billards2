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
    public PlayerBehavior playerCore; // 玩家核心引用
    
    
    [Header("相机设置")]
    public Camera targetCamera; // 目标相机，如果为空则使用主相机
    
    [Header("轨迹计算")]
    public TrajectoryPredictor trajectoryPredictor; // 轨迹预测器（物理模拟系统）
    
    [Header("渲染器")]
    public AimLineRenderer aimLineRenderer; // 瞄准线渲染器引用
    
    [Header("显示限制")]
    [Tooltip("瞄准线最大显示距离（米），超出此距离的轨迹将被截断并渐隐")]
    [SerializeField] private float maxDisplayDistance = 8f;
    
    [Tooltip("最多显示的碰撞次数，超过此次数后截断轨迹（0表示无限制）")]
    [SerializeField] private int maxDisplayCollisions = 2;
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息
    
    // 私有变量
    private Camera cam;
    private bool isVisible = false; // 是否显示瞄准线
    private Vector2 aimDirection;
    
    // 组件引用
    private AimLineLandingPointManager landingPointManager;
    private ChargeSystem chargeSystem;
    
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
        // 游戏暂停时，不更新瞄准线
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            return;
        }
        
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
        InitializeRenderer();
        InitializeComponents();
        
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
    /// 初始化必要组件
    /// </summary>
    void InitializeComponents()
    {
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
            Debug.Log($"AimController: 组件初始化完成 - 落点管理器: {(landingPointManager != null ? "已找到" : "未找到")}, 蓄力系统: {(chargeSystem != null ? "已找到" : "未找到")}");
        }
    }
    
    /// <summary>
    /// 计算路径总长度
    /// </summary>
    float CalculatePathLength(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count < 2)
        {
            return 0f;
        }
        
        float totalLength = 0f;
        for (int i = 1; i < pathPoints.Count; i++)
        {
            totalLength += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
        }
        
        return totalLength;
    }
    
    /// <summary>
    /// 计算截断距离（考虑最大距离和最大碰撞次数）
    /// </summary>
    float CalculateTruncationDistance(List<Vector3> pathPoints, List<Vector3> collisionPoints, float maxDistance, int maxCollisions)
    {
        float truncationDistance = maxDistance;
        
        // 如果设置了碰撞次数限制
        if (maxCollisions > 0 && collisionPoints != null && collisionPoints.Count > maxCollisions)
        {
            // 找到第N次碰撞在路径上的累积距离
            Vector3 limitCollision = collisionPoints[maxCollisions];  // 第N+1次碰撞（索引从0开始）
            
            float distanceToCollision = 0f;
            for (int i = 1; i < pathPoints.Count; i++)
            {
                distanceToCollision += Vector3.Distance(pathPoints[i - 1], pathPoints[i]);
                
                // 找到这个碰撞点的位置
                if (Vector3.Distance(pathPoints[i], limitCollision) < 0.05f)
                {
                    // 使用两个限制中较小的那个
                    truncationDistance = Mathf.Min(truncationDistance, distanceToCollision);
                    break;
                }
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[AimController] 碰撞次数截断: {collisionPoints.Count}次 > {maxCollisions}次限制, 截断距离: {distanceToCollision:F2}m");
            }
        }
        
        return truncationDistance;
    }
    
    /// <summary>
    /// 过滤显示范围内的碰撞点
    /// </summary>
    List<Vector3> FilterCollisionsInRange(List<Vector3> collisionPoints, List<Vector3> pathPoints, float maxDistance)
    {
        if (collisionPoints == null || collisionPoints.Count == 0)
        {
            return new List<Vector3>();
        }
        
        List<Vector3> visibleCollisions = new List<Vector3>();
        
        // 遍历每个碰撞点，找到它在路径上的累积距离
        for (int i = 0; i < collisionPoints.Count; i++)
        {
            Vector3 collision = collisionPoints[i];
            
            // 计算此碰撞点在路径上的累积距离
            float collisionDistance = 0f;
            for (int j = 1; j < pathPoints.Count; j++)
            {
                collisionDistance += Vector3.Distance(pathPoints[j - 1], pathPoints[j]);
                
                // 找到碰撞点在路径上的位置（容差 0.05m）
                if (Vector3.Distance(pathPoints[j], collision) < 0.05f)
                {
                    // 如果碰撞点在显示范围内，添加到可见列表
                    if (collisionDistance <= maxDistance)
                    {
                        visibleCollisions.Add(collision);
                    }
                    break;
                }
            }
        }
        
        return visibleCollisions;
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
        
        // 使用物理模拟系统计算轨迹
        if (trajectoryPredictor == null)
        {
            aimLineRenderer.ClearAllLines();
            if (showDebugInfo)
            {
                Debug.LogWarning("AimController: TrajectoryPredictor 未配置！");
            }
            return;
        }
        
        // 计算初始速度 = 方向 * 力度
        float currentForce = 0f;
        if (chargeSystem != null)
        {
            currentForce = chargeSystem.GetCurrentForce();
        }
        
        Vector2 initialVelocity = aimDirection * currentForce;
        List<Vector3> pathPoints = trajectoryPredictor.PredictTrajectory(startPos, initialVelocity);
        List<Vector3> collisionPoints = trajectoryPredictor.GetCollisionPoints();
        
        // 渲染轨迹
        if (pathPoints != null && pathPoints.Count > 0)
        {
            // 计算轨迹总长度
            float totalLength = CalculatePathLength(pathPoints);
            
            // ✅ 计算截断距离（考虑距离限制和碰撞次数限制）
            float truncationDistance = CalculateTruncationDistance(
                pathPoints, 
                collisionPoints, 
                maxDisplayDistance, 
                maxDisplayCollisions
            );
            
            // 判断是否需要截断
            bool needTruncate = totalLength > truncationDistance || 
                               (maxDisplayCollisions > 0 && collisionPoints != null && collisionPoints.Count > maxDisplayCollisions);
            
            if (needTruncate)
            {
                // ⚠️ 需要截断轨迹
                List<Vector3> displayPath = aimLineRenderer.TruncatePathAtDistance(pathPoints, truncationDistance);
                
                // 过滤碰撞点：只显示在截断范围内的
                List<Vector3> visibleCollisions = FilterCollisionsInRange(collisionPoints, pathPoints, truncationDistance);
                
                // 渲染截断后的轨迹（末端自动渐隐）
                aimLineRenderer.RenderSegmentedAimLine(displayPath, visibleCollisions);
                
                // ❌ 隐藏落点（实际落点超出显示范围）
                if (landingPointManager != null)
                {
                    landingPointManager.HideLandingPoint();
                }
                
                if (showDebugInfo)
                {
                    string reason = totalLength > maxDisplayDistance ? "距离限制" : "碰撞次数限制";
                    Debug.Log($"[AimController] 轨迹截断({reason}): 总长{totalLength:F2}m, 截断距离{truncationDistance:F2}m, 碰撞: {collisionPoints.Count} → {visibleCollisions.Count}");
                }
            }
            else
            {
                // ✅ 完整轨迹在显示范围内
                aimLineRenderer.RenderSegmentedAimLine(pathPoints, collisionPoints);
                
                // ✅ 直接从轨迹终点获取精确落点
                if (landingPointManager != null)
                {
                    Vector3 landingPoint = pathPoints[pathPoints.Count - 1];
                    landingPointManager.ShowLandingPoint(landingPoint);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"[AimController] 显示完整轨迹({totalLength:F2}m), 碰撞{collisionPoints.Count}次");
                    }
                }
            }
        }
        else
        {
            // 轨迹预测失败，不显示瞄准线
            aimLineRenderer.ClearAllLines();
            if (landingPointManager != null)
            {
                landingPointManager.HideLandingPoint();
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
    /// 获取落点管理器组件
    /// </summary>
    /// <returns>落点管理器</returns>
    public AimLineLandingPointManager GetLandingPointManager()
    {
        return landingPointManager;
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
