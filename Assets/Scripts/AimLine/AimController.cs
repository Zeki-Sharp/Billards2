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
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息
    
    // 私有变量
    private Camera cam;
    private bool isVisible = false; // 是否显示瞄准线
    private Vector2 aimDirection;
    
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
    /// 更新瞄准方向（从白球指向鼠标）
    /// </summary>
    void UpdateAimDirection()
    {
        if (playerCore == null || cam == null) return;
        
        // 直接使用New Input System更新瞄准方向
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseScreenPos = new Vector3(mousePos.x, mousePos.y, 0f);
        
        // 转换为世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition(mouseScreenPos);
        
        // 计算瞄准方向 - 从白球指向鼠标的方向
        Vector3 direction = mouseWorldPos - playerCore.transform.position;
        if (direction.magnitude > 0.1f) // 避免零向量
        {
            aimDirection = direction.normalized;
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
            aimLineRenderer.RenderSegmentedAimLine(pathPoints);
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
