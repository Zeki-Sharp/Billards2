using UnityEngine;

/// <summary>
/// 蓄力系统 - 统一管理蓄力逻辑和状态
/// 
/// 【核心职责】：
/// - 管理蓄力进度计算和状态
/// - 提供蓄力配置和参数
/// - 通过事件通知其他组件蓄力状态变化
/// - 支持多种蓄力模式（线性、循环、曲线等）
/// 
/// 【设计原则】：
/// - 纯逻辑组件，不处理UI显示
/// - 事件驱动，松耦合通信
/// - 配置化设计，易于扩展
/// - 可独立测试
/// </summary>
public class ChargeSystem : MonoBehaviour
{
    [Header("拉弓式蓄力设置")]
    [SerializeField] private float maxForce = 25f; // 最大力度
    [SerializeField] private float minForce = 5f; // 最小力度
    [SerializeField] private float maxPullDistance = 8f; // 最大拉弓距离（世界单位）
    [SerializeField] private float minPullDistance = 1f; // 最小拉弓距离
    
    [Header("组件引用")]
    [SerializeField] private PlayerCore playerCore; // 用于获取球位置
    [SerializeField] private Camera targetCamera; // 用于坐标转换
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 蓄力状态
    private bool isCharging = false;
    private float chargingStartTime = 0f;
    private float chargingPower = 0f; // 蓄力进度 (0-1)
    private float currentForce = 0f; // 当前力度
    
    // 时停特效状态
    private bool timestopEffectTriggered = false; // 是否已触发时停入场特效
    
    
    void Start()
    {
        
        // 订阅蓄力事件
        GameEventBus.OnChargingStarted += StartCharging;
        GameEventBus.OnChargingStopped += StopCharging;
        GameEventBus.OnChargingReset += ResetCharging;
    }
    
    void OnDestroy()
    {
        // 取消订阅蓄力事件
        GameEventBus.OnChargingStarted -= StartCharging;
        GameEventBus.OnChargingStopped -= StopCharging;
        GameEventBus.OnChargingReset -= ResetCharging;
    }
    
    void Update()
    {
        if (isCharging)
        {
            UpdateChargingProgress();
        }
    }
    
    #region 蓄力控制
    
    /// <summary>
    /// 开始蓄力
    /// </summary>
    public void StartCharging()
    {
        if (isCharging) return;
        
        isCharging = true;
        chargingStartTime = Time.time;
        chargingPower = 0f;
        currentForce = minForce;
        timestopEffectTriggered = false; // 重置时停特效状态

        if (showDebugInfo)
        {
            Debug.Log("ChargeSystem: 开始蓄力");
        }
    }
    
    /// <summary>
    /// 停止蓄力
    /// </summary>
    public void StopCharging()
    {
        if (!isCharging) return;
        
        isCharging = false;
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 停止蓄力 - 最终力度: {currentForce:F2}");
        }
    }
    
    /// <summary>
    /// 重置蓄力
    /// </summary>
    public void ResetCharging()
    {
        isCharging = false;
        chargingPower = 0f;
        chargingStartTime = 0f;
        currentForce = 0f;
        
        GameEventBus.PublishChargingProgressChanged(0f);
        GameEventBus.PublishForceChanged(0f);
        
        if (showDebugInfo)
        {
            Debug.Log("ChargeSystem: 重置蓄力");
        }
    }
    
    #endregion
    
    #region 蓄力计算
    
    /// <summary>
    /// 更新蓄力进度
    /// </summary>
    void UpdateChargingProgress()
    {
        if (!isCharging) return;
        
        // 基于鼠标距离计算蓄力
        UpdateBowPullCharging();
        
        // 计算当前力度
        CalculateCurrentForce();
        
        // 触发事件
        GameEventBus.PublishChargingProgressChanged(chargingPower);
        GameEventBus.PublishForceChanged(currentForce);
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"ChargeSystem [拉弓模式]: 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}");
        }
    }
    
    /// <summary>
    /// 更新基于鼠标距离的拉弓蓄力
    /// </summary>
    void UpdateBowPullCharging()
    {
        // 检查必要的组件引用
        if (playerCore == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("ChargeSystem: PlayerCore未设置，无法使用拉弓模式");
            }
            return;
        }
        
        // 获取相机
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null)
        {
            if (showDebugInfo)
            {
                Debug.LogWarning("ChargeSystem: 找不到相机，无法使用拉弓模式");
            }
            return;
        }
        
        // 获取鼠标世界坐标
        Vector3 mouseWorldPos = GetMouseWorldPosition(cam);
        
        // 获取球位置
        Vector3 ballPos = playerCore.transform.position;
        
        // 计算距离
        float distance = Vector2.Distance(new Vector2(mouseWorldPos.x, mouseWorldPos.y), new Vector2(ballPos.x, ballPos.y));
        
        // 距离映射到蓄力进度 (0-1)
        if (distance <= minPullDistance)
        {
            chargingPower = 0f;
        }
        else if (distance >= maxPullDistance)
        {
            chargingPower = 1f;
        }
        else
        {
            chargingPower = (distance - minPullDistance) / (maxPullDistance - minPullDistance);
        }
    }
    
    /// <summary>
    /// 获取鼠标的世界坐标（2D）
    /// 复用AimController的坐标转换逻辑
    /// </summary>
    Vector3 GetMouseWorldPosition(Camera cam)
    {
        // 使用New Input System获取鼠标位置
        Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
        Vector3 mouseScreenPos = new Vector3(mousePos.x, mousePos.y, 0f);
        
        // 转换为世界坐标（与AimController相同的转换逻辑）
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        float cameraSize = cam.orthographicSize;
        float aspectRatio = (float)screenWidth / screenHeight;
        
        float worldX = (mouseScreenPos.x / screenWidth - 0.5f) * cameraSize * aspectRatio * 2f;
        float worldY = (mouseScreenPos.y / screenHeight - 0.5f) * cameraSize * 2f;
        
        return new Vector3(worldX, worldY, 0f);
    }
    
    /// <summary>
    /// 计算当前力度
    /// </summary>
    void CalculateCurrentForce()
    {
        // 拉弓模式：直接基于蓄力进度（距离）计算力度
        currentForce = Mathf.Lerp(minForce, maxForce, chargingPower);
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取蓄力进度 (0-1)
    /// </summary>
    public float GetChargingProgress()
    {
        return chargingPower;
    }
    
    /// <summary>
    /// 获取当前力度
    /// </summary>
    public float GetCurrentForce()
    {
        return currentForce;
    }
    
    /// <summary>
    /// 获取蓄力强度 (0-1)
    /// </summary>
    public float GetChargingPower()
    {
        return chargingPower;
    }
    
    /// <summary>
    /// 是否正在蓄力
    /// </summary>
    public bool IsCharging()
    {
        return isCharging;
    }
    
    /// <summary>
    /// 是否蓄力完成
    /// </summary>
    public bool IsChargingComplete()
    {
        return chargingPower >= 1f;
    }
    
    #endregion
    
    #region 配置管理
    
    /// <summary>
    /// 设置蓄力参数
    /// </summary>
    public void SetChargingParameters(float maxF, float minF)
    {
        maxForce = maxF;
        minForce = minF;
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 更新蓄力参数 - 最大力度: {maxF}, 最小力度: {minF}");
        }
    }
    
    #endregion
    
    #region 时停特效管理
    
    
    #endregion
    
    #region 公共属性
    
    public float MaxForce => maxForce;
    public float MinForce => minForce;
    
    #endregion
    
    #region 组件设置
    
    
    #endregion
}
