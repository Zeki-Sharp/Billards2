using System.Collections.Generic;
using UnityEngine;

public class AimController : MonoBehaviour
{
    // 常量定义
    private const float DEFAULT_BALL_RADIUS = 0.5f;
    
    [Header("瞄准设置")]
    public float aimLineLength = 3f; // 瞄准线固定长度（反射计算器失败时的后备方案）
    
    [Header("蓄力系统")]
    public ChargeSystem chargeSystem; // 蓄力系统引用
    
    [Header("UI设置")]
    public ChargeBarUI chargeBarUI; // 蓄力条UI
    
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
    
    // 事件 (已迁移到 GameEventBus)
    
    void Start()
    {
        InitializeController();
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
        
        if (chargeSystem == null)
        {
            Debug.LogError("AimController: ChargeSystem 未设置！请在Inspector中设置ChargeSystem引用");
            return;
        }
        
        if (chargeBarUI == null)
        {
            Debug.LogError("AimController: ChargeBarUI 未设置！请在Inspector中设置ChargeBarUI引用");
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
    /// 显示蓄力UI（由外部调用）
    /// </summary>
    public void ShowChargingUI()
    {
        isVisible = true;
        
        // 显示蓄力条
        if (chargeBarUI != null)
        {
            chargeBarUI.SetVisible(true);
        }
        else
        {
            Debug.LogWarning("AimController: ChargeBarUI 未设置，无法显示蓄力条");
        }
        
        // 订阅 GameEventBus 事件
        GameEventBus.OnChargingProgressChanged += UpdateChargingProgress;
        GameEventBus.OnForceChanged += UpdateForceDisplay;
        
        if (showDebugInfo)
        {
            Debug.Log("AimController: 已订阅 GameEventBus 事件");
        }
        
        // 触发蓄力开始特效
        if (playerCore != null)
        {
            playerCore.gameObject.PublishEffect("ChargeStart", playerCore.transform.position);
        }
        
    }
    
    /// <summary>
    /// 更新蓄力进度显示（由ChargeSystem事件调用）
    /// </summary>
    /// <param name="chargingProgress">蓄力进度 (0-1)</param>
    void UpdateChargingProgress(float chargingProgress)
    {
        if (!isVisible) return;
        
        // 更新蓄力条UI
        if (chargeBarUI != null)
        {
            chargeBarUI.UpdateCharge(chargingProgress);
        }
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
    /// 隐藏蓄力UI（由外部调用）
    /// </summary>
    public void HideChargingUI()
    {
        isVisible = false;
        
        // 取消订阅蓄力系统事件
        // 取消订阅 GameEventBus 事件
        GameEventBus.OnChargingProgressChanged -= UpdateChargingProgress;
        GameEventBus.OnForceChanged -= UpdateForceDisplay;
        
        // 隐藏蓄力条
        if (chargeBarUI != null)
        {
            chargeBarUI.SetVisible(false);
        }
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
                if (ballPhysics != null && ballPhysics.ballData != null)
                {
                    ballRadius = ballPhysics.ballData.radius;
                }
            }
            
            List<Vector3> pathPoints = reflectionCalculator.CalculateReflectionPath(startPos, aimDirection, ballRadius);
            aimLineRenderer.RenderSegmentedAimLine(pathPoints);
        }
        else
        {
            // 反射计算器失败时，使用简单瞄准线作为后备
            Vector3 endPos = startPos + (Vector3)aimDirection * aimLineLength;
            aimLineRenderer.RenderSimpleAimLine(startPos, endPos);
        }
    }
    
    
    
    // 公共方法
    public float GetCurrentForce()
    {
        return chargeSystem != null ? chargeSystem.GetCurrentForce() : 0f;
    }
    
    /// <summary>
    /// 是否正在显示蓄力UI
    /// </summary>
    public bool IsVisible()
    {
        return isVisible;
    }
    
    public Vector2 GetAimDirection()
    {
        return aimDirection;
    }
    
    // 手动设置白球引用（用于运行时动态设置）
    public void SetPlayerCore(PlayerCore core)
    {
        playerCore = core;
    }
    
    // 手动设置蓄力系统引用（用于运行时动态设置）
    public void SetChargeSystem(ChargeSystem system)
    {
        chargeSystem = system;
    }
    
    // 手动设置相机引用
    public void SetCamera(Camera camera)
    {
        cam = camera;
    }
    
    // 重置控制器状态
    public void ResetController()
    {
        isVisible = false;
        aimDirection = Vector2.zero;
        
        // 取消订阅蓄力系统事件
        // 取消订阅 GameEventBus 事件
        GameEventBus.OnChargingProgressChanged -= UpdateChargingProgress;
        GameEventBus.OnForceChanged -= UpdateForceDisplay;
        
        if (chargeBarUI != null)
        {
            chargeBarUI.SetVisible(false);
        }
        
        if (aimLineRenderer != null)
        {
            aimLineRenderer.ClearAllLines();
        }
        
        // 清除反射路径
        if (reflectionCalculator != null)
        {
            reflectionCalculator.ClearPath();
        }
        
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
