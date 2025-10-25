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
    /// <summary>
    /// 蓄力模式枚举
    /// </summary>
    public enum ChargeMode
    {
        TimeBased,    // 时间模式：力度随时间自动循环变化
        BowPull       // 拉弓模式：力度基于拖拽距离
    }
    
    [Header("蓄力模式设置")]
    [SerializeField] private ChargeMode chargeMode = ChargeMode.TimeBased;
    [SerializeField] [Tooltip("是否显示当前使用的蓄力模式")] private bool showModeInfo = true;
    
    [Header("通用蓄力设置")]
    [SerializeField] private float maxForce = 25f; // 最大力度
    [SerializeField] private float minForce = 5f; // 最小力度
    
    [Header("时间蓄力设置")]
    [SerializeField] private float chargeTime = 2f; // 蓄满力所需时间（秒）
    
    [Header("拉弓蓄力设置")]
    [SerializeField] [Tooltip("拉弓的最大距离（世界单位）")] private float maxPullDistance = 5f;
    [SerializeField] [Tooltip("拉弓的最小有效距离")] private float minPullDistance = 0.1f;
    
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
    
    // 拉弓模式状态
    private Vector3 bowPullStartPosition; // 拉弓开始位置（世界坐标）
    private float currentPullDistance = 0f; // 当前拉弓距离
    private Vector2 bowPullDirection = Vector2.zero; // 拉弓方向（从起始位置指向当前位置）
    private Vector2 launchDirection = Vector2.zero; // 发射方向（拉弓方向的反向）
    
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
            // 根据模式更新蓄力进度
            if (chargeMode == ChargeMode.TimeBased)
            {
                UpdateChargingProgress_TimeBased();
            }
            else if (chargeMode == ChargeMode.BowPull)
            {
                UpdateChargingProgress_BowPull();
            }
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
        
        // 拉弓模式：记录开始位置（球的位置作为拉弓中心）
        if (chargeMode == ChargeMode.BowPull)
        {
            if (playerCore != null)
            {
                bowPullStartPosition = playerCore.transform.position;
            }
            else
            {
                Debug.LogError("ChargeSystem [拉弓模式]: PlayerCore未设置，无法获取球的位置！");
                bowPullStartPosition = Vector3.zero;
            }
            currentPullDistance = 0f;
            
            if (showDebugInfo && showModeInfo)
            {
                Debug.Log($"ChargeSystem [拉弓模式]: 开始蓄力，拉弓中心（球位置）: {bowPullStartPosition}");
            }
        }
        else
        {
            if (showDebugInfo && showModeInfo)
            {
                Debug.Log("ChargeSystem [时间模式]: 开始蓄力");
            }
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
        currentPullDistance = 0f;
        bowPullStartPosition = Vector3.zero;
        bowPullDirection = Vector2.zero;
        launchDirection = Vector2.zero;
        
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
    /// 更新蓄力进度 - 时间模式
    /// </summary>
    void UpdateChargingProgress_TimeBased()
    {
        if (!isCharging) return;
        
        // 基于时间计算蓄力（循环模式：0->1->0->1...）
        float elapsedTime = Time.time - chargingStartTime;
        // PingPong 会在 0 到 chargeTime 之间来回，除以 chargeTime 得到 0-1 的循环值
        chargingPower = Mathf.PingPong(elapsedTime, chargeTime) / chargeTime;
        
        // 计算当前力度
        CalculateCurrentForce_TimeBased();
        
        // 触发事件
        GameEventBus.PublishChargingProgressChanged(chargingPower);
        GameEventBus.PublishForceChanged(currentForce);
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"ChargeSystem [时间模式]: 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}");
        }
    }
    
    /// <summary>
    /// 更新蓄力进度 - 拉弓模式
    /// </summary>
    void UpdateChargingProgress_BowPull()
    {
        if (!isCharging) return;
        
        if (playerCore == null)
        {
            Debug.LogError("ChargeSystem [拉弓模式]: PlayerCore未设置，无法更新拉弓进度！");
            return;
        }
        
        // 获取球的当前位置（拉弓中心）
        Vector3 ballPosition = playerCore.transform.position;
        
        // 获取当前鼠标位置（世界坐标）
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        
        // 计算拉弓方向和距离（从球的位置指向鼠标位置）
        Vector3 pullVector = mouseWorldPos - ballPosition;
        currentPullDistance = pullVector.magnitude;
        
        // 计算拉弓方向（归一化）
        if (currentPullDistance > 0.01f)
        {
            bowPullDirection = pullVector.normalized;
            // 发射方向是拉弓方向的反向
            launchDirection = -bowPullDirection;
        }
        
        // 根据拉弓距离计算蓄力进度（0-1）
        chargingPower = Mathf.Clamp01((currentPullDistance - minPullDistance) / (maxPullDistance - minPullDistance));
        
        // 计算当前力度
        CalculateCurrentForce_BowPull();
        
        // 触发事件
        GameEventBus.PublishChargingProgressChanged(chargingPower);
        GameEventBus.PublishForceChanged(currentForce);
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"ChargeSystem [拉弓模式]: 球位置={ballPosition}, 鼠标位置={mouseWorldPos}, 拉弓距离={currentPullDistance:F2}, 拉弓方向={bowPullDirection}, 发射方向={launchDirection}, 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}");
        }
    }
    
    /// <summary>
    /// 计算当前力度 - 时间模式
    /// </summary>
    void CalculateCurrentForce_TimeBased()
    {
        // 时间模式：直接基于蓄力进度（时间）计算力度
        currentForce = Mathf.Lerp(minForce, maxForce, chargingPower);
    }
    
    /// <summary>
    /// 计算当前力度 - 拉弓模式
    /// </summary>
    void CalculateCurrentForce_BowPull()
    {
        // 拉弓模式：基于拉弓距离计算力度
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
    
    /// <summary>
    /// 获取发射方向（根据当前模式）
    /// </summary>
    /// <param name="ballPosition">球的位置（用于时间模式）</param>
    /// <returns>发射方向</returns>
    public Vector2 GetLaunchDirection(Vector3 ballPosition)
    {
        if (chargeMode == ChargeMode.BowPull)
        {
            // 拉弓模式：使用计算好的发射方向（拉弓反向）
            return launchDirection;
        }
        else
        {
            // 时间模式：从球指向鼠标
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector2 direction = (mouseWorldPos - ballPosition).normalized;
            return direction;
        }
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
    public ChargeMode CurrentChargeMode => chargeMode;
    public float CurrentPullDistance => currentPullDistance;
    public Vector3 BowPullStartPosition => bowPullStartPosition;
    public Vector2 BowPullDirection => bowPullDirection;
    public Vector2 LaunchDirection => launchDirection;
    
    #endregion
    
    #region 模式切换
    
    /// <summary>
    /// 切换蓄力模式
    /// </summary>
    public void SetChargeMode(ChargeMode mode)
    {
        if (isCharging)
        {
            Debug.LogWarning("ChargeSystem: 蓄力进行中，无法切换模式");
            return;
        }
        
        chargeMode = mode;
        
        if (showDebugInfo && showModeInfo)
        {
            Debug.Log($"ChargeSystem: 切换到 {(mode == ChargeMode.TimeBased ? "时间模式" : "拉弓模式")}");
        }
    }
    
    /// <summary>
    /// 切换到时间模式
    /// </summary>
    public void SwitchToTimeBasedMode()
    {
        SetChargeMode(ChargeMode.TimeBased);
    }
    
    /// <summary>
    /// 切换到拉弓模式
    /// </summary>
    public void SwitchToBowPullMode()
    {
        SetChargeMode(ChargeMode.BowPull);
    }
    
    #endregion
    
    #region 组件设置
    
    
    #endregion
}
