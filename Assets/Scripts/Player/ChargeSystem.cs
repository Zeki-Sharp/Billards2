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
    [Header("蓄力设置")]
    [SerializeField] private float maxChargingTime = 3f; // 最大蓄力时间
    [SerializeField] private float chargingSpeed = 1f; // 蓄力速度（已弃用，保留兼容性）
    [SerializeField] private float maxForce = 25f; // 最大力度
    [SerializeField] private float minForce = 5f; // 最小力度
    [SerializeField] private bool useCyclingCharge = false; // 是否使用循环蓄力
    [SerializeField] private float cycleSpeed = 3f; // 循环速度
    
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 蓄力状态
    private bool isCharging = false;
    private float chargingStartTime = 0f;
    private float chargingPower = 0f; // 蓄力进度 (0-1)
    private float currentForce = 0f; // 当前力度
    
    // 时停特效状态
    private bool timestopEffectTriggered = false; // 是否已触发时停入场特效
    private TransitionManager transitionManager; // 用于获取门槛值
    private TimeStopEffect timeStopEffect; // 时停特效控制器
    
    
    // 事件
    public System.Action<float> OnChargingProgressChanged; // 蓄力进度变化 (0-1)
    public System.Action<float> OnForceChanged; // 力度变化
    public System.Action OnChargingStarted; // 开始蓄力
    public System.Action OnChargingCompleted; // 蓄力完成
    public System.Action OnChargingStopped; // 停止蓄力
    
    void Start()
    {
        // 获取TransitionManager引用
        transitionManager = FindFirstObjectByType<TransitionManager>();
        if (transitionManager == null)
        {
            Debug.LogWarning("ChargeSystem: 未找到TransitionManager，时停特效门槛值将使用默认值");
        }
        
        // 获取TimeStopEffect引用
        timeStopEffect = FindFirstObjectByType<TimeStopEffect>();
        if (timeStopEffect == null)
        {
            Debug.LogWarning("ChargeSystem: 未找到TimeStopEffect，时停特效将不可用");
        }
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
        
        OnChargingStarted?.Invoke();
        
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
        OnChargingStopped?.Invoke();
        
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
        
        OnChargingProgressChanged?.Invoke(0f);
        OnForceChanged?.Invoke(0f);
        
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
        
        // 计算蓄力进度
        float chargingTime = Time.time - chargingStartTime;
        chargingPower = Mathf.Clamp01(chargingTime / maxChargingTime);
        
        // 更新时停特效强度
        UpdateTimestopEffect();
        
        // 计算当前力度
        CalculateCurrentForce();
        
        // 触发事件
        OnChargingProgressChanged?.Invoke(chargingPower);
        OnForceChanged?.Invoke(currentForce);
        
        // 调试信息
        if (showDebugInfo && Time.frameCount % 30 == 0) // 每30帧打印一次
        {
            Debug.Log($"ChargeSystem: 蓄力时间={chargingTime:F2}s, 蓄力进度={chargingPower:F2}, 当前力度={currentForce:F2}, 最小力度={minForce}, 最大力度={maxForce}");
        }
        
        // 检查是否蓄力完成
        if (chargingPower >= 1f)
        {
            chargingPower = 1f;
            OnChargingCompleted?.Invoke();
            
            if (showDebugInfo)
            {
                Debug.Log("ChargeSystem: 蓄力完成！");
            }
        }
    }
    
    /// <summary>
    /// 计算当前力度
    /// </summary>
    void CalculateCurrentForce()
    {
        if (useCyclingCharge)
        {
            // 循环蓄力：在最小和最大力度之间循环
            float range = maxForce - minForce;
            float cycleTime = 2f / cycleSpeed;
            float time = Time.time % cycleTime;
            
            float cycleValue;
            if (time < cycleTime * 0.5f)
            {
                cycleValue = time / (cycleTime * 0.5f);
            }
            else
            {
                cycleValue = 2f - (time / (cycleTime * 0.5f));
            }
            
            currentForce = minForce + cycleValue * range;
        }
        else
        {
            // 线性蓄力：基于蓄力进度
            currentForce = Mathf.Lerp(minForce, maxForce, chargingPower);
        }
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
    public void SetChargingParameters(float maxTime, float speed, float maxF, float minF)
    {
        maxChargingTime = maxTime;
        chargingSpeed = speed;
        maxForce = maxF;
        minForce = minF;
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 更新蓄力参数 - 最大时间: {maxTime}, 速度: {speed}, 最大力度: {maxF}, 最小力度: {minF}");
        }
    }
    
    /// <summary>
    /// 设置循环蓄力模式
    /// </summary>
    public void SetCyclingMode(bool useCycling, float cycleS = 3f)
    {
        useCyclingCharge = useCycling;
        cycleSpeed = cycleS;
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeSystem: 设置循环模式 - 使用循环: {useCycling}, 循环速度: {cycleS}");
        }
    }
    
    #endregion
    
    #region 时停特效管理
    
    /// <summary>
    /// 检查时停特效触发
    /// </summary>
    private void UpdateTimestopEffect()
    {
        if (timeStopEffect == null) return;
        
        // 直接设置时停特效强度（跟随充能进度）
        timeStopEffect.SetIntensity(chargingPower);
    }
    
    #endregion
    
    #region 公共属性
    
    public float MaxChargingTime => maxChargingTime;
    public float ChargingSpeed => chargingSpeed;
    public float MaxForce => maxForce;
    public float MinForce => minForce;
    public bool UseCyclingCharge => useCyclingCharge;
    public float CycleSpeed => cycleSpeed;
    
    #endregion
    
    #region 组件设置
    
    
    #endregion
}
