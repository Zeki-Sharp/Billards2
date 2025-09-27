using System.Collections.Generic;
using UnityEngine;

public class AimController : MonoBehaviour
{
    // 常量定义
    private const float DEFAULT_LINE_WIDTH = 0.1f;
    private const int DEFAULT_SORTING_ORDER = 10;
    private const int DEFAULT_CAP_VERTICES = 8;
    private const float MIN_DIRECTION_MAGNITUDE = 0.1f;
    private const float DEFAULT_BALL_RADIUS = 0.5f;
    private const float MIN_ANGLE_THRESHOLD = 0.01f;
    private const float BACKOFF_MULTIPLIER = 0.5f;
    
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
    public MonoBehaviour reflectionCalculator; // 反射计算器引用
    
    [Header("渲染器")]
    public AimLineRenderer aimLineRenderer; // 瞄准线渲染器引用
    
    [Header("调试设置")]
    [SerializeField] private bool showDebugInfo = true; // 是否显示调试信息
    
    // 私有变量
    private Camera cam;
    private bool isVisible = false; // 是否显示瞄准线
    private Vector2 aimDirection;
    private List<Vector3> reflectionPath = new List<Vector3>();
    
    // 事件
    public System.Action<Vector2, float> OnLaunch;
    
    void Start()
    {
        InitializeController();
    }
    
    void Update()
    {
        // 如果PlayerCore还没有找到，尝试再次查找
        if (playerCore == null)
        {
            playerCore = FindAnyObjectByType<PlayerCore>();
            if (playerCore != null)
            {
                // 设置瞄准线
                SetupAimLine();
                
                // 初始化反射计算器
                InitializeReflectionCalculator();
                
                // 初始化渲染器
                InitializeRenderer();
                
            }
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
        
        // 获取玩家核心 - 使用延迟查找
        if (playerCore == null)
        {
            playerCore = FindAnyObjectByType<PlayerCore>();
            if (playerCore == null)
            {
                Debug.LogWarning("AimController: 当前找不到PlayerCore，将在下一帧重试");
                // 即使没有PlayerCore，也要设置瞄准线
                SetupAimLine();
                // 使用协程延迟查找
                StartCoroutine(DelayedPlayerCoreSearch());
                return;
            }
        }
        
        // 获取蓄力系统 - 使用延迟查找
        if (chargeSystem == null)
        {
            chargeSystem = FindAnyObjectByType<ChargeSystem>();
            if (chargeSystem == null)
            {
                Debug.LogWarning("AimController: 当前找不到ChargeSystem，将在下一帧重试");
                return;
            }
        }
        
        // 获取蓄力条UI - 使用延迟查找
        if (chargeBarUI == null)
        {
            chargeBarUI = FindAnyObjectByType<ChargeBarUI>();
            if (chargeBarUI == null)
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning("AimController: 当前找不到ChargeBarUI，将在下一帧重试");
                }
                return;
            }
        }
        
        // 设置瞄准线
        SetupAimLine();
        
        // 初始化反射计算器
        InitializeReflectionCalculator();
        
        // 初始化渲染器
        InitializeRenderer();
        
    }
    
    /// <summary>
    /// 延迟查找PlayerCore的协程
    /// </summary>
    System.Collections.IEnumerator DelayedPlayerCoreSearch()
    {
        int maxAttempts = 30; // 最多尝试30次（约0.5秒）
        int attempts = 0;
        
        while (playerCore == null && attempts < maxAttempts)
        {
            yield return new WaitForEndOfFrame();
            playerCore = FindAnyObjectByType<PlayerCore>();
            attempts++;
        }
        
        if (playerCore != null)
        {
            
            // 查找蓄力系统
            if (chargeSystem == null)
            {
                chargeSystem = FindAnyObjectByType<ChargeSystem>();
            }
            
            // 查找蓄力条UI
            if (chargeBarUI == null)
            {
                chargeBarUI = FindAnyObjectByType<ChargeBarUI>();
                if (chargeBarUI == null && showDebugInfo)
                {
                    Debug.LogWarning("AimController: 延迟查找仍未找到ChargeBarUI");
                }
            }
            
            // 设置瞄准线
            SetupAimLine();
            
            // 初始化反射计算器
            InitializeReflectionCalculator();
            
            // 初始化渲染器
            InitializeRenderer();
            
        }
        else
        {
            Debug.LogError("AimController: 延迟查找失败，无法找到PlayerCore！");
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
        
        // 订阅反射计算器事件
        if (reflectionCalculator != null)
        {
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
            {
                calculator.OnPathCalculated += OnReflectionPathCalculated;
            }
        }
        else
        {
            Debug.LogWarning("AimController: 反射计算器初始化失败");
        }
    }
    
    void OnReflectionPathCalculated(List<Vector3> pathPoints)
    {
        reflectionPath = new List<Vector3>(pathPoints);
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
        
        if (aimLineRenderer != null)
        {
            if (showDebugInfo)
            {
                Debug.Log("AimController: 渲染器初始化完成");
            }
        }
        else
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
        
        // 获取鼠标屏幕坐标
        Vector3 mouseScreenPos = Input.mousePosition;
        
        // 转换为世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition(mouseScreenPos);
        
        // 计算瞄准方向 - 从白球指向鼠标的方向
        Vector3 direction = mouseWorldPos - playerCore.transform.position;
        if (direction.magnitude > MIN_DIRECTION_MAGNITUDE) // 避免零向量
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
            // 尝试重新查找
            chargeBarUI = FindAnyObjectByType<ChargeBarUI>();
            if (chargeBarUI != null)
            {
                chargeBarUI.SetVisible(true);
            }
            else if (showDebugInfo)
            {
                Debug.LogWarning("AimController: 无法找到ChargeBarUI");
            }
        }
        
        // 订阅蓄力系统事件
        if (chargeSystem != null)
        {
            chargeSystem.OnChargingProgressChanged += UpdateChargingProgress;
            chargeSystem.OnForceChanged += UpdateForceDisplay;
        }
        else if (showDebugInfo)
        {
            Debug.LogWarning("AimController: ChargeSystem 为 null，无法订阅事件");
        }
        
        // 触发蓄力开始特效
        if (playerCore != null)
        {
            EventTrigger.ChargeStart(playerCore.transform.position, playerCore.gameObject);
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
        if (chargeSystem != null)
        {
            chargeSystem.OnChargingProgressChanged -= UpdateChargingProgress;
            chargeSystem.OnForceChanged -= UpdateForceDisplay;
        }
        
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
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
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
                
                List<Vector3> pathPoints = calculator.CalculateReflectionPath(startPos, aimDirection, ballRadius);
                aimLineRenderer.RenderSegmentedAimLine(pathPoints);
            }
            else
            {
                // 反射计算器失败时，使用简单瞄准线作为后备
                Vector3 endPos = startPos + (Vector3)aimDirection * aimLineLength;
                aimLineRenderer.RenderSimpleAimLine(startPos, endPos);
            }
        }
        else
        {
            // 反射计算器未设置时，使用简单瞄准线
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
        if (chargeSystem != null)
        {
            chargeSystem.OnChargingProgressChanged -= UpdateChargingProgress;
            chargeSystem.OnForceChanged -= UpdateForceDisplay;
        }
        
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
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
            {
                calculator.ClearPath();
            }
        }
        
    }
    
    // 反射相关方法
    
    public AimLineReflectionCalculator GetReflectionCalculator()
    {
        return reflectionCalculator as AimLineReflectionCalculator;
    }
    
    public List<Vector3> GetCurrentReflectionPath()
    {
        return reflectionPath != null ? new List<Vector3>(reflectionPath) : new List<Vector3>();
    }
    
    public string GetReflectionStats()
    {
        if (reflectionCalculator != null)
        {
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
            {
                return calculator.GetReflectionStats();
            }
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
