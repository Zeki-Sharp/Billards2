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
    [Header("时间蓄力设置")]
    [SerializeField] private float maxForce = 25f; // 最大力度
    [SerializeField] private float minForce = 5f; // 最小力度
    [SerializeField] private float chargeTime = 2f; // 蓄满力所需时间（秒）
    
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
        
        // 基于时间计算蓄力（循环模式：0->1->0->1...）
        float elapsedTime = Time.time - chargingStartTime;
        // PingPong 会在 0 到 chargeTime 之间来回，除以 chargeTime 得到 0-1 的循环值
        chargingPower = Mathf.PingPong(elapsedTime, chargeTime) / chargeTime;
        
        // 计算当前力度
        CalculateCurrentForce();
        
        // 触发事件
        GameEventBus.PublishChargingProgressChanged(chargingPower);
        GameEventBus.PublishForceChanged(currentForce);
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"ChargeSystem [循环模式]: 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}");
        }
    }
    
    /// <summary>
    /// 计算当前力度
    /// </summary>
    void CalculateCurrentForce()
    {
        // 时间模式：直接基于蓄力进度（时间）计算力度
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
