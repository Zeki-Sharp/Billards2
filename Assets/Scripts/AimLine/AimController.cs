using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AimController : MonoBehaviour
{
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
    
    [Header("材质控制器")]
    public AimLineMaterialController materialController; // 材质控制器引用
    
    // 私有变量
    private Camera cam;
    private bool isVisible = false; // 是否显示瞄准线
    private Vector2 aimDirection;
    private LineRenderer aimLine;
    private List<Vector3> reflectionPath = new List<Vector3>();
    
    // 分段绘制相关
    private List<LineRenderer> segmentLines = new List<LineRenderer>();
    private GameObject lineContainer; // 用于管理所有线段
    
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
                Debug.Log("AimController: 在Update中找到PlayerCore，开始初始化");
                // 设置瞄准线
                SetupAimLine();
                
                // 初始化反射计算器
                InitializeReflectionCalculator();
                
                // 初始化材质控制器
                InitializeMaterialController();
                
                Debug.Log("TestAimController Update初始化完成");
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
            Debug.LogError("TestAimController: 找不到相机！请设置targetCamera或确保有MainCamera标签的相机");
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
                Debug.LogWarning("AimController: 当前找不到ChargeBarUI，将在下一帧重试");
                // 尝试查找所有ChargeBarUI组件进行调试
                ChargeBarUI[] allChargeBars = FindObjectsByType<ChargeBarUI>(FindObjectsSortMode.None);
                Debug.Log($"AimController: 场景中找到 {allChargeBars.Length} 个ChargeBarUI组件");
                for (int i = 0; i < allChargeBars.Length; i++)
                {
                    Debug.Log($"ChargeBarUI[{i}]: {allChargeBars[i].name}, Active: {allChargeBars[i].gameObject.activeInHierarchy}");
                }
                return;
            }
            else
            {
                Debug.Log($"AimController: 找到ChargeBarUI: {chargeBarUI.name}");
            }
        }
        
        // 设置瞄准线
        SetupAimLine();
        
        // 初始化反射计算器
        InitializeReflectionCalculator();
        
        // 初始化材质控制器
        InitializeMaterialController();
        
        Debug.Log("TestAimController 初始化完成");
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
            Debug.Log("AimController: 延迟查找成功，找到PlayerCore");
            
            // 查找蓄力系统
            if (chargeSystem == null)
            {
                chargeSystem = FindAnyObjectByType<ChargeSystem>();
            }
            
            // 查找蓄力条UI
            if (chargeBarUI == null)
            {
                chargeBarUI = FindAnyObjectByType<ChargeBarUI>();
                if (chargeBarUI != null)
                {
                    Debug.Log($"AimController: 延迟查找找到ChargeBarUI: {chargeBarUI.name}");
                }
                else
                {
                    Debug.LogWarning("AimController: 延迟查找仍未找到ChargeBarUI");
                }
            }
            
            // 设置瞄准线
            SetupAimLine();
            
            // 初始化反射计算器
            InitializeReflectionCalculator();
            
            // 初始化材质控制器
            InitializeMaterialController();
            
            Debug.Log("TestAimController 延迟初始化完成");
        }
        else
        {
            Debug.LogError("AimController: 延迟查找失败，无法找到PlayerCore！");
        }
    }
    
    void SetupAimLine()
    {
        // 如果已经存在，先清理
        if (aimLine != null)
        {
            if (aimLine.gameObject != null)
            {
                DestroyImmediate(aimLine.gameObject);
            }
            aimLine = null;
        }
        
        if (lineContainer != null)
        {
            DestroyImmediate(lineContainer);
            lineContainer = null;
        }
        
        // 创建瞄准线容器
        lineContainer = new GameObject("AimLineContainer");
        lineContainer.transform.SetParent(transform);
        
        // 创建主瞄准线对象（用于无反射情况）
        GameObject lineObj = new GameObject("AimLine");
        lineObj.transform.SetParent(lineContainer.transform);
        
        // 添加LineRenderer组件
        aimLine = lineObj.AddComponent<LineRenderer>();
        aimLine.material = new Material(Shader.Find("Sprites/Default"));
        
        // 设置默认颜色（材质控制器会在后续更新）
        aimLine.startColor = Color.yellow;
        aimLine.endColor = Color.yellow;
        aimLine.startWidth = 0.1f;
        aimLine.endWidth = 0.1f;
        aimLine.positionCount = 0;
        aimLine.sortingOrder = 10;
        aimLine.useWorldSpace = true;
        
        // 设置Round Cap
        aimLine.numCapVertices = 8; // 圆形端点的顶点数
        aimLine.alignment = LineAlignment.TransformZ; // 确保线条朝向正确
        
        Debug.Log("瞄准线设置完成");
    }
    
    // 创建分段线段
    LineRenderer CreateSegmentLine(int segmentIndex)
    {
        GameObject segmentObj = new GameObject($"AimLineSegment_{segmentIndex}");
        segmentObj.transform.SetParent(lineContainer.transform);
        
        LineRenderer segmentLine = segmentObj.AddComponent<LineRenderer>();
        
        // 使用材质控制器的材质，如果没有则使用默认材质
        if (materialController != null && materialController.GetAimLineMaterial() != null)
        {
            segmentLine.material = materialController.GetAimLineMaterial();
        }
        else
        {
            segmentLine.material = new Material(Shader.Find("Sprites/Default"));
        }
        
        // 从材质中获取颜色和透明度
        if (materialController != null && materialController.GetAimLineMaterial() != null)
        {
            Material aimMaterial = materialController.GetAimLineMaterial();
            
            // 尝试获取_Tint参数，如果没有则使用默认颜色
            if (aimMaterial.HasProperty("_Tint"))
            {
                Color tintColor = aimMaterial.GetColor("_Tint");
                segmentLine.startColor = tintColor;
                segmentLine.endColor = tintColor;
            }
            else
            {
                // 如果没有_Tint参数，使用默认颜色
                segmentLine.startColor = Color.yellow;
                segmentLine.endColor = Color.yellow;
            }
        }
        else
        {
            segmentLine.startColor = Color.yellow;
            segmentLine.endColor = Color.yellow;
        }
        segmentLine.startWidth = 0.1f;
        segmentLine.endWidth = 0.1f;
        segmentLine.positionCount = 2; // 每个线段只有起点和终点
        segmentLine.sortingOrder = 10;
        segmentLine.useWorldSpace = true;
        
        // 设置Round Cap
        segmentLine.numCapVertices = 8;
        segmentLine.alignment = LineAlignment.TransformZ;
        
        return segmentLine;
    }
    
    // 清除所有分段线段
    void ClearSegmentLines()
    {
        foreach (LineRenderer line in segmentLines)
        {
            if (line != null)
            {
                DestroyImmediate(line.gameObject);
            }
        }
        segmentLines.Clear();
    }
    
    // 计算端点回退距离
    float CalculateBackoff(Vector3 point1, Vector3 point2, Vector3 point3, float lineWidth)
    {
        // 计算两条线段的方向向量
        Vector3 dir1 = (point2 - point1).normalized;
        Vector3 dir2 = (point3 - point2).normalized;
        
        // 计算夹角（弧度）
        float angle = Vector3.Angle(dir1, dir2) * Mathf.Deg2Rad;
        
        // 避免除零错误
        if (angle < 0.01f)
        {
            return 0f;
        }
        
        // 使用公式：backoff = 0.5 * width / tan(angle/2)
        float backoff = 0.5f * lineWidth / Mathf.Tan(angle * 0.5f);
        
        return backoff;
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
                Debug.Log("TestAimController: 自动创建反射计算器组件");
            }
        }
        
        // 订阅反射计算器事件
        if (reflectionCalculator != null)
        {
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
            {
                calculator.OnPathCalculated += OnReflectionPathCalculated;
                Debug.Log("TestAimController: 反射计算器初始化完成");
            }
        }
        else
        {
            Debug.LogWarning("TestAimController: 反射计算器初始化失败");
        }
    }
    
    void OnReflectionPathCalculated(List<Vector3> pathPoints)
    {
        reflectionPath = new List<Vector3>(pathPoints);
    }
    
    void InitializeMaterialController()
    {
        // 如果没有设置材质控制器，尝试自动查找
        if (materialController == null)
        {
            materialController = GetComponent<AimLineMaterialController>();
            if (materialController == null)
            {
                materialController = gameObject.AddComponent<AimLineMaterialController>();
                Debug.Log("TestAimController: 自动创建材质控制器组件");
            }
        }
        
        if (materialController != null)
        {
            Debug.Log("TestAimController: 材质控制器初始化完成");
        }
        else
        {
            Debug.LogWarning("TestAimController: 材质控制器初始化失败");
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
            Debug.Log("AimController: ChargeBarUI 已显示");
        }
        else
        {
            Debug.LogWarning("AimController: ChargeBarUI 为 null，无法显示蓄力条");
            // 尝试重新查找
            chargeBarUI = FindAnyObjectByType<ChargeBarUI>();
            if (chargeBarUI != null)
            {
                chargeBarUI.SetVisible(true);
                Debug.Log("AimController: 重新查找后找到ChargeBarUI并显示");
            }
            else
            {
                Debug.LogError("AimController: 重新查找后仍未找到ChargeBarUI");
            }
        }
        
        // 订阅蓄力系统事件
        if (chargeSystem != null)
        {
            chargeSystem.OnChargingProgressChanged += UpdateChargingProgress;
            chargeSystem.OnForceChanged += UpdateForceDisplay;
            Debug.Log("AimController: 已订阅蓄力系统事件");
        }
        else
        {
            Debug.LogWarning("AimController: ChargeSystem 为 null，无法订阅事件");
        }
        
        // 触发蓄力开始特效
        if (playerCore != null)
        {
            EventTrigger.ChargeStart(playerCore.transform.position, playerCore.gameObject);
        }
        
        Debug.Log("AimController: 显示蓄力UI");
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
            Debug.Log($"AimController: 更新蓄力进度 {chargingProgress:F2}");
        }
        else
        {
            Debug.LogWarning("AimController: ChargeBarUI 为 null，无法更新蓄力进度");
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
        
        Debug.Log("AimController: 隐藏蓄力UI");
    }
    
    void UpdateAimLine()
    {
        // 确保aimLine已初始化
        if (aimLine == null)
        {
            Debug.LogWarning("AimController: aimLine未初始化，尝试重新设置");
            SetupAimLine();
            if (aimLine == null)
            {
                Debug.LogError("AimController: 无法初始化aimLine，跳过更新");
                return;
            }
        }
        
        // 检查白球是否在移动
        if (playerCore == null || playerCore.IsPhysicsMoving())
        {
            aimLine.positionCount = 0;
            ClearSegmentLines(); // 清除分段线段
            return;
        }
        
        // 只有在显示状态时才显示瞄准线
        if (!isVisible)
        {
            aimLine.positionCount = 0;
            ClearSegmentLines(); // 清除分段线段
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
                float ballRadius = 0.5f; // 默认半径
                if (playerCore != null)
                {
                    BallPhysics ballPhysics = playerCore.GetComponent<BallPhysics>();
                    if (ballPhysics != null && ballPhysics.ballData != null)
                    {
                        ballRadius = ballPhysics.ballData.radius;
                    }
                }
                
                List<Vector3> pathPoints = calculator.CalculateReflectionPath(startPos, aimDirection, ballRadius);
                UpdateAimLineWithSegmentedReflection(pathPoints);
            }
            else
            {
                Debug.LogWarning("TestAimController: 反射计算器类型转换失败");
                // 反射计算器失败时，使用简单瞄准线作为后备
                UpdateSimpleAimLine(startPos);
            }
        }
        else
        {
            Debug.LogWarning("TestAimController: 反射计算器未设置，使用简单瞄准线");
            // 反射计算器未设置时，使用简单瞄准线
            UpdateSimpleAimLine(startPos);
        }
    }
    
    void UpdateSimpleAimLine(Vector3 startPos)
    {
        // 使用固定长度的简单瞄准线
        Vector3 endPos = startPos + (Vector3)aimDirection * aimLineLength;
        aimLine.positionCount = 2;
        aimLine.SetPosition(0, startPos);
        aimLine.SetPosition(1, endPos);
        
        // 应用材质颜色和透明度
        if (materialController != null && materialController.GetAimLineMaterial() != null)
        {
            Material aimMaterial = materialController.GetAimLineMaterial();
            
            // 尝试获取_Tint参数，如果没有则使用默认颜色
            if (aimMaterial.HasProperty("_Tint"))
            {
                Color tintColor = aimMaterial.GetColor("_Tint");
                aimLine.startColor = tintColor;
                aimLine.endColor = tintColor;
            }
            else
            {
                // 如果没有_Tint参数，使用默认颜色
                aimLine.startColor = Color.yellow;
                aimLine.endColor = Color.yellow;
            }
            
            // 为简单瞄准线应用淡出效果
            float segmentLength = Vector3.Distance(startPos, endPos);
            materialController.UpdateSegmentMaterial(aimLine, segmentLength, true);
        }
        
        // 隐藏分段线段
        ClearSegmentLines();
    }
    
    void UpdateAimLineWithSegmentedReflection(List<Vector3> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count <= 1)
        {
            aimLine.positionCount = 0;
            ClearSegmentLines();
            return;
        }
        
        // 隐藏主瞄准线
        aimLine.positionCount = 0;
        
        // 清除旧的分段线段
        ClearSegmentLines();
        
        // 获取线条宽度
        float lineWidth = aimLine.startWidth;
        
        // 根据路径点数创建分段
        int segmentCount = pathPoints.Count - 1;
        
        for (int i = 0; i < segmentCount; i++)
        {
            // 创建分段线段
            LineRenderer segmentLine = CreateSegmentLine(i);
            segmentLines.Add(segmentLine);
            
            // 计算起点和终点
            Vector3 startPoint = pathPoints[i];
            Vector3 endPoint = pathPoints[i + 1];
            
            // 如果不是第一个线段，需要计算回退
            if (i > 0)
            {
                Vector3 prevPoint = pathPoints[i - 1];
                float backoff = CalculateBackoff(prevPoint, pathPoints[i], endPoint, lineWidth);
                
                // 第一个线段的终点回退
                Vector3 direction = (pathPoints[i] - prevPoint).normalized;
                startPoint = pathPoints[i] - direction * backoff;
            }
            
            // 如果不是最后一个线段，需要计算回退
            if (i < segmentCount - 1)
            {
                Vector3 nextPoint = pathPoints[i + 2];
                float backoff = CalculateBackoff(pathPoints[i], pathPoints[i + 1], nextPoint, lineWidth);
                
                // 当前线段的终点回退
                Vector3 direction = (pathPoints[i + 1] - pathPoints[i]).normalized;
                endPoint = pathPoints[i + 1] - direction * backoff;
            }
            
            // 设置分段线段的起点和终点
            segmentLine.SetPosition(0, startPoint);
            segmentLine.SetPosition(1, endPoint);
            
            // 应用材质效果（根据线段长度调整UScale）
            if (materialController != null)
            {
                float segmentLength = Vector3.Distance(startPoint, endPoint);
                
                // 判断是否为最后一个线段，如果是则应用淡出效果
                bool isLastSegment = (i == segmentCount - 1);
                materialController.UpdateSegmentMaterial(segmentLine, segmentLength, isLastSegment);
            }
            
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
        Debug.Log($"AimController: 设置玩家核心引用为 {core.name}");
    }
    
    // 手动设置蓄力系统引用（用于运行时动态设置）
    public void SetChargeSystem(ChargeSystem system)
    {
        chargeSystem = system;
        Debug.Log($"AimController: 设置蓄力系统引用为 {system.name}");
    }
    
    // 手动设置相机引用
    public void SetCamera(Camera camera)
    {
        cam = camera;
        Debug.Log($"TestAimController: 设置相机引用为 {camera.name}");
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
        
        if (aimLine != null)
        {
            aimLine.positionCount = 0;
        }
        
        // 清除分段线段
        ClearSegmentLines();
        
        // 清除反射路径
        if (reflectionCalculator != null)
        {
            var calculator = reflectionCalculator as AimLineReflectionCalculator;
            if (calculator != null)
            {
                calculator.ClearPath();
            }
        }
        
        Debug.Log("AimController 状态已重置");
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
    
    // 材质控制器相关方法
    public AimLineMaterialController GetMaterialController()
    {
        return materialController;
    }
    
    public void SetAimLineMaterial(Material material)
    {
        if (materialController != null)
        {
            materialController.SetAimLineMaterial(material);
        }
    }
    
    public void SetMaterialFlowEffect(bool enabled, float speed = 1f)
    {
        if (materialController != null)
        {
            materialController.SetFlowEffect(enabled, speed);
        }
    }
    
    public void ResetMaterialFlowTime()
    {
        if (materialController != null)
        {
            materialController.ResetFlowTime();
        }
    }
    
    public string GetMaterialStats()
    {
        if (materialController != null)
        {
            return materialController.GetMaterialStats();
        }
        return "材质控制器未初始化";
    }
}
