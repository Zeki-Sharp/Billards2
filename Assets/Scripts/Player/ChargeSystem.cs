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
/// 
public class ChargeSystem : MonoBehaviour
{
    /// <summary>
    /// 蓄力模式枚举
    /// </summary>
    public enum ChargeMode
    {
        TimeBased,    // 时间模式：力度随时间自动循环变化
        BowPull,      // 拉弓模式：力度基于拖拽距离
        ScrollBased   // 滚轮模式：鼠标滚轮调节力度
    }
    
    [Header("蓄力模式设置")]
    [SerializeField] private ChargeMode chargeMode = ChargeMode.TimeBased;
    [SerializeField] [Tooltip("是否显示当前使用的蓄力模式")] private bool showModeInfo = true;
    
    [Header("通用蓄力设置")]
    [SerializeField] private float maxForce = 25f; // 最大力度
    // ⚠️ 多角色系统改造：移除 minForce，统一从0开始
    
    [Header("时间蓄力设置")]
    [SerializeField] private float chargeTime = 2f; // 蓄满力所需时间（秒）
    
    [Header("拉弓蓄力设置")]
    [SerializeField] [Tooltip("拉弓的最大距离（世界单位）")] private float maxPullDistance = 5f;
    [SerializeField] [Tooltip("拉弓的最小有效距离")] private float minPullDistance = 0.1f;
    
    [Header("力度门槛设置")]
    [SerializeField] [Tooltip("发射力度阈值（小于此值不能发射，也可以切换选择）")] 
    private float launchForceThreshold = 2f;
    
    [Header("滚轮蓄力设置")]
    [SerializeField] [Tooltip("滚轮灵敏度")] private float scrollSensitivity = 0.5f;
    
    [Header("组件引用")]
    [SerializeField] private PlayerBehavior playerBehavior; // 用于获取球位置
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
    
    // 滚轮模式状态
    private float scrollAccumulatedValue = 0f; // 累计滚轮输入值
    
    // 时停特效状态（暂未使用）
    // private bool timestopEffectTriggered = false; // 是否已触发时停入场特效
    
    
    void Start()
    {
        // ⚠️ 多角色系统改造：移除全局事件订阅
        // 现在由 ChargeController 直接调用公共方法，不再响应全局事件
        
        // 【已移除】订阅蓄力事件：OnChargingStarted, OnChargingStopped, OnChargingReset
        // 【已移除】滚轮模式自动蓄力逻辑：OnPlayerPlayingPhaseStarted, CheckInitialGameState
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 初始化完成（模式={chargeMode}），等待 ChargeController 调用");
        }
    }
    
    void OnDestroy()
    {
        // ⚠️ 多角色系统改造：已移除全局事件订阅，无需取消订阅
    }
    
    void Update()
    {
        // 游戏暂停时，不更新蓄力进度
        if (GameManager.Instance != null && GameManager.Instance.IsGamePaused)
        {
            return;
        }
        
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
            else if (chargeMode == ChargeMode.ScrollBased)
            {
                UpdateChargingProgress_ScrollBased();
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
        currentForce = 0f;  // ✅ 统一从0开始
        // timestopEffectTriggered = false; // 重置时停特效状态（暂未使用）
        
        // 拉弓模式：记录开始位置（球的位置作为拉弓中心）
        if (chargeMode == ChargeMode.BowPull)
        {
            if (playerBehavior != null)
            {
                bowPullStartPosition = playerBehavior.transform.position;
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
        else if (chargeMode == ChargeMode.ScrollBased)
        {
            // 滚轮模式：重置累计值
            scrollAccumulatedValue = 0f;
            
            if (showDebugInfo && showModeInfo)
            {
                Debug.Log("ChargeSystem [滚轮模式]: 开始蓄力，等待滚轮输入调节力度");
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
        scrollAccumulatedValue = 0f;
        
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
        
        if (playerBehavior == null)
        {
            Debug.LogError("ChargeSystem [拉弓模式]: PlayerCore未设置，无法更新拉弓进度！");
            return;
        }
        
        // 获取球的当前位置（拉弓中心）
        Vector3 ballPosition = playerBehavior.transform.position;
        
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
        // ✅ 方案B：从0开始，统一门槛值逻辑
        currentForce = Mathf.Lerp(0f, maxForce, chargingPower);
    }
    
    /// <summary>
    /// 计算当前力度 - 拉弓模式
    /// </summary>
    void CalculateCurrentForce_BowPull()
    {
        // ✅ 方案B：从0开始，统一门槛值逻辑
        currentForce = Mathf.Lerp(0f, maxForce, chargingPower);
    }
    
    /// <summary>
    /// 更新蓄力进度 - 滚轮模式
    /// </summary>
    void UpdateChargingProgress_ScrollBased()
    {
        if (!isCharging) return;
        
        // 根据累计的滚轮值计算蓄力进度（0-1）
        chargingPower = Mathf.Clamp01(scrollAccumulatedValue);
        
        // 计算当前力度
        CalculateCurrentForce_ScrollBased();
        
        // 触发事件
        GameEventBus.PublishChargingProgressChanged(chargingPower);
        GameEventBus.PublishForceChanged(currentForce);
        
        // 调试信息（每帧都打印，便于调试）
        if (showDebugInfo && Time.frameCount % 60 == 0) // 每秒打印一次
        {
            Debug.Log($"ChargeSystem [滚轮模式]: 滚轮累计值={scrollAccumulatedValue:F2}, 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}, 阈值={launchForceThreshold:F2}");
        }
    }
    
    /// <summary>
    /// 计算当前力度 - 滚轮模式
    /// </summary>
    void CalculateCurrentForce_ScrollBased()
    {
        // ✅ 方案B：从0开始，统一门槛值逻辑
        currentForce = Mathf.Lerp(0f, maxForce, chargingPower);
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
            // 时间模式和滚轮模式：从球指向鼠标
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            Vector2 direction = (mouseWorldPos - ballPosition).normalized;
            return direction;
        }
    }
    
    /// <summary>
    /// 处理滚轮输入（由 PlayerInputHandler 调用）
    /// </summary>
    /// <param name="scrollDelta">滚轮输入增量</param>
    public void ProcessScrollInput(float scrollDelta)
    {
        if (!isCharging || chargeMode != ChargeMode.ScrollBased) return;
        
        // 累加滚轮输入（考虑灵敏度）
        scrollAccumulatedValue += scrollDelta * scrollSensitivity;
        
        // 限制在 0-1 范围内
        scrollAccumulatedValue = Mathf.Clamp01(scrollAccumulatedValue);
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem [滚轮模式]: 滚轮输入={scrollDelta:F2}, 累计值={scrollAccumulatedValue:F2}");
        }
    }
    
    /// <summary>
    /// 检查当前力度是否达到发射阈值
    /// </summary>
    /// <returns>是否可以发射</returns>
    public bool CanLaunch()
    {
        // ✅ 方案B：所有模式统一检查门槛值
        return currentForce >= launchForceThreshold;
    }
    
    #endregion
    
    #region 配置管理
    
    /// <summary>
    /// 设置蓄力参数
    /// </summary>
    public void SetChargingParameters(float maxF, float threshold)
    {
        maxForce = maxF;
        launchForceThreshold = threshold;
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 更新蓄力参数 - 最大力度: {maxF}, 发射门槛: {threshold}");
        }
    }
    
    #endregion
    
    #region 时停特效管理
    
    
    #endregion
    
    #region 公共属性
    
    public float MaxForce => maxForce;
    // ⚠️ 方案B：移除 MinForce，统一从0开始
    public ChargeMode CurrentChargeMode => chargeMode;
    public float CurrentPullDistance => currentPullDistance;
    public Vector3 BowPullStartPosition => bowPullStartPosition;
    public Vector2 BowPullDirection => bowPullDirection;
    public Vector2 LaunchDirection => launchDirection;
    public float LaunchForceThreshold => launchForceThreshold;
    public float ScrollAccumulatedValue => scrollAccumulatedValue;
    
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
    
    #region 玩家回合事件处理（滚轮模式专用）
    
    // ⚠️ 多角色系统改造：已移除自动蓄力逻辑
    // 【已删除】OnPlayerPlayingPhaseStarted() - 不再自动响应Playing阶段开始
    // 【已删除】CheckInitialGameState() - 不再延迟检查游戏状态
    // 现在由 ChargeController 在角色被选中时主动调用 StartCharging()
    
    #endregion
    
    #region 组件设置
    
    
    #endregion
}
