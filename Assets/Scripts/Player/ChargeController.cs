using UnityEngine;

/// <summary>
/// 蓄力控制器 - 协调蓄力操作流程
/// 
/// 【核心职责】：
/// - 监听角色选择事件，启动对应角色的蓄力
/// - 响应滚轮输入，调节当前角色的蓄力
/// - 响应发射输入，触发当前角色的发射
/// - 发布蓄力和发射事件
/// 
/// 【设计原则】：
/// - 不检测输入（由GlobalInputManager负责）
/// - 不维护选中状态（由CharacterSelectionController负责）
/// - 直接调用特定角色的ChargeSystem（不使用全局事件）
/// 
/// 【执行顺序】：
/// - CONTROLLER: 控制器层，处理蓄力逻辑
/// - 在 OnEnable 中订阅 GameEventBus 事件（完全解耦，不依赖 GlobalInputManager）
/// </summary>
[DefaultExecutionOrder(ManagerExecutionOrder.CONTROLLER)]
public class ChargeController : MonoBehaviour
{
    [Header("调试")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 当前蓄力的角色
    private ChargeSystem currentChargeSystem;
    private string currentCharacterID;
    // ⚠️ 移除 hasEnteredChargingState 标志
    // 状态转换由 PlayerStateMachine 根据力度自动管理
    
    // 场景单例
    private static ChargeController instance;
    public static ChargeController Instance => instance;
    
    void Awake()
    {
        // 单例检查
        if (instance != null && instance != this)
        {
            Debug.LogWarning("ChargeController: 场景中存在多个实例，销毁多余实例");
            Destroy(gameObject);
            return;
        }
        instance = this;
        
        if (showDebugInfo)
        {
            Debug.Log("ChargeController: 初始化完成");
        }
    }
    
    void OnEnable()
    {
        // 订阅 GameEventBus 事件
        GameEventBus.OnCharacterSelected += HandleCharacterSelected;
        GameEventBus.OnCharacterDeselected += HandleCharacterDeselected;
        GameEventBus.OnScrollInput += HandleScrollInput;
        GameEventBus.OnLaunchInput += HandleLaunchInput;
    }
    
    void OnDisable()
    {
        // 取消订阅
        GameEventBus.OnCharacterSelected -= HandleCharacterSelected;
        GameEventBus.OnCharacterDeselected -= HandleCharacterDeselected;
        GameEventBus.OnScrollInput -= HandleScrollInput;
        GameEventBus.OnLaunchInput -= HandleLaunchInput;
    }
    
    void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }
    
    void Start()
    {
        if (showDebugInfo)
        {
            Debug.Log("ChargeController: 初始化完成，已通过 OnEnable 订阅 GameEventBus 输入事件");
        }
    }
    
    #region 角色选择响应
    
    /// <summary>
    /// 处理角色选中
    /// </summary>
    void HandleCharacterSelected(string characterID)
    {
        if (showDebugInfo)
        {
            Debug.Log($"ChargeController: 角色被选中 {characterID}");
        }
        
        // 获取角色的ChargeSystem
        ChargeSystem chargeSystem = GetChargeSystemForCharacter(characterID);
        if (chargeSystem == null)
        {
            Debug.LogError($"ChargeController: 无法找到角色 {characterID} 的ChargeSystem");
            return;
        }
        
        // 保存引用
        currentChargeSystem = chargeSystem;
        currentCharacterID = characterID;
        
        // ✅ 修复：不立即启动蓄力，等待滚轮输入
        // 滚轮输入时会自动开始蓄力，并在力度≥门槛时发布 ChargingStarted 事件
        if (chargeSystem.CurrentChargeMode == ChargeSystem.ChargeMode.ScrollBased)
        {
            // 只初始化蓄力系统，不发布事件
            chargeSystem.StartCharging();
            
            if (showDebugInfo)
            {
                Debug.Log($"ChargeController: 角色 {characterID} 已选中，等待滚轮输入（当前力度=0）");
            }
        }
    }
    
    /// <summary>
    /// 处理角色取消选中
    /// </summary>
    void HandleCharacterDeselected(string characterID)
    {
        if (showDebugInfo)
        {
            Debug.Log($"ChargeController: 角色取消选中 {characterID}");
        }
        
        // 停止蓄力
        if (currentChargeSystem != null && currentCharacterID == characterID)
        {
            float currentForce = currentChargeSystem.GetCurrentForce();
            currentChargeSystem.ResetCharging();
            
            // 发布蓄力停止事件
            GameEventBus.PublishCharacterChargingStopped(characterID, currentForce);
            
            // 清空引用
            currentChargeSystem = null;
            currentCharacterID = null;
            
            if (showDebugInfo)
            {
                Debug.Log($"ChargeController: 停止角色 {characterID} 的蓄力");
            }
        }
    }
    
    #endregion
    
    #region 输入响应
    
    /// <summary>
    /// 处理滚轮输入
    /// </summary>
    void HandleScrollInput(float scrollDelta)
    {
        if (currentChargeSystem == null)
        {
            // 没有选中角色，忽略滚轮输入
            return;
        }
        
        // ✅ 状态机思想：只调节力度，状态由 PlayerStateMachine 自动管理
        currentChargeSystem.ProcessScrollInput(scrollDelta);
        
        if (showDebugInfo)
        {
            float newForce = currentChargeSystem.GetCurrentForce();
            float threshold = currentChargeSystem.LaunchForceThreshold;
            Debug.Log($"ChargeController: 调节角色 {currentCharacterID} 蓄力，delta={scrollDelta}, 当前力度={newForce:F2}, 门槛={threshold}");
        }
    }
    
    /// <summary>
    /// 处理发射输入
    /// </summary>
    void HandleLaunchInput()
    {
        if (currentChargeSystem == null)
        {
            if (showDebugInfo)
            {
                Debug.Log("ChargeController: 无选中角色，忽略发射输入");
            }
            return;
        }
        
        // 检查是否可以发射
        if (!currentChargeSystem.CanLaunch())
        {
            if (showDebugInfo)
            {
                Debug.LogWarning($"ChargeController: 角色 {currentCharacterID} 蓄力不足，无法发射（当前={currentChargeSystem.GetCurrentForce():F2}，需要>={currentChargeSystem.LaunchForceThreshold:F2}）");
            }
            return;
        }
        
        // 获取发射参数
        float force = currentChargeSystem.GetCurrentForce();
        
        // 获取球体位置
        GameObject ballObject = CharacterSelectionController.Instance.SelectedBallObject;
        if (ballObject == null)
        {
            Debug.LogError("ChargeController: 选中的球体对象为空！");
            return;
        }
        
        Vector3 ballPosition = ballObject.transform.position;
        Vector3 direction = currentChargeSystem.GetLaunchDirection(ballPosition);
        
        // 停止蓄力
        currentChargeSystem.StopCharging();
        
        // ✅ 发布发射事件（事件驱动，让球体自己响应）
        GameEventBus.PublishCharacterLaunched(currentCharacterID, direction, force);
        
        // 发射后自动取消选中
        if (CharacterSelectionController.Instance != null)
        {
            CharacterSelectionController.Instance.ForceDeselect();
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"ChargeController: ✅ 发布角色 {currentCharacterID} 发射事件！力度={force:F2}，方向={direction}");
        }
    }
    
    #endregion
    
    #region 工具方法
    
    /// <summary>
    /// 获取角色的ChargeSystem
    /// </summary>
    ChargeSystem GetChargeSystemForCharacter(string characterID)
    {
        // 从CharacterSelectionController获取选中的球体
        if (CharacterSelectionController.Instance == null)
        {
            Debug.LogError("ChargeController: CharacterSelectionController实例不存在");
            return null;
        }
        
        GameObject ballObject = CharacterSelectionController.Instance.SelectedBallObject;
        if (ballObject == null)
        {
            Debug.LogError($"ChargeController: 角色 {characterID} 的球体对象为空");
            return null;
        }
        
        // 获取ChargeSystem组件
        ChargeSystem chargeSystem = ballObject.GetComponent<ChargeSystem>();
        if (chargeSystem == null)
        {
            Debug.LogError($"ChargeController: 球体 {ballObject.name} 缺少ChargeSystem组件");
        }
        
        return chargeSystem;
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取当前蓄力力度
    /// </summary>
    public float GetCurrentForce()
    {
        return currentChargeSystem != null ? currentChargeSystem.GetCurrentForce() : 0f;
    }
    
    /// <summary>
    /// 检查当前是否有角色在蓄力
    /// </summary>
    public bool IsCharging()
    {
        return currentChargeSystem != null && currentChargeSystem.IsCharging();
    }
    
    #endregion
}

