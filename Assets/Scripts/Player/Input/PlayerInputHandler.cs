using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

/// <summary>
/// 玩家输入处理器 - 统一处理所有玩家输入检测和分发
/// 
/// 【核心职责】：
/// - 处理WASD移动输入和鼠标蓄力输入
/// - 使用New Input System进行输入检测
/// - 与PlayerInputPermissionManager协作进行权限检查
/// 
/// 【设计原则】：
/// - 作为输入的统一入口，避免其他组件直接检测输入
/// - 专注于输入检测和分发，权限检查委托给PlayerInputPermissionManager
/// - 通过权限管理器检查所有输入权限
/// 
/// 【输入类型】：
/// - 移动输入：WASD键盘输入，用于控制玩家移动
/// - 蓄力输入：鼠标左键，用于开始和结束蓄力
/// </summary>
public class PlayerInputHandler : MonoBehaviour
{
    [Header("输入设置")]
    [SerializeField] private bool showDebugInfo = true;
    
    // 组件引用
    private PlayerMovementController movementController; // 需要处理WASD移动
    private PlayerInputPermissionManager permissionManager; // 需要权限检查
    private ChargeSystem chargeSystem; // 需要处理滚轮输入
    
    // Input System支持
    private InputAction moveAction;
    private InputAction chargeAction;
    private InputAction scrollAction;
    private InputActionMap inputActionMap;
    
    // 输入状态
    private Vector2 moveInput;
    private bool isMovePressed;
    private bool isChargePressed;
    private bool isChargeHeld;
    private bool isChargeReleased;
    private float scrollDelta;
    
    void Start()
    {
        // 获取组件引用
        movementController = GetComponent<PlayerMovementController>();
        permissionManager = GetComponent<PlayerInputPermissionManager>();
        chargeSystem = GetComponent<ChargeSystem>();
        
        // 确保权限管理器存在
        if (permissionManager == null)
        {
            permissionManager = gameObject.AddComponent<PlayerInputPermissionManager>();
            Debug.LogWarning("PlayerInputHandler: 自动添加PlayerInputPermissionManager组件");
        }
        
        // 初始化输入系统
        InitializeInputSystem();
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerInputHandler: 初始化完成");
        }
    }
    
    void Update()
    {
        
        // 更新输入状态
        UpdateInputState();
        
        // 处理输入
        HandleInput();
    }
    
    void OnEnable()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Enable();
        }
    }
    
    void OnDisable()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Disable();
        }
    }
    
    void OnDestroy()
    {
        if (inputActionMap != null)
        {
            inputActionMap.Dispose();
        }
    }
    
    #region 输入系统初始化
    
    /// <summary>
    /// 初始化输入系统
    /// </summary>
    void InitializeInputSystem()
    {
        // 创建Input Actions
        inputActionMap = new InputActionMap("Player");
        
        // 创建Move Action
        moveAction = inputActionMap.AddAction("Move", InputActionType.Value, "<Keyboard>/w");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        
        // 创建Charge Action
        chargeAction = inputActionMap.AddAction("Charge", InputActionType.Button, "<Mouse>/leftButton");
        
        // 创建Scroll Action（鼠标滚轮）
        scrollAction = inputActionMap.AddAction("Scroll", InputActionType.Value, "<Mouse>/scroll/y");
        
        // 启用Actions
        inputActionMap.Enable();
        
        if (showDebugInfo)
        {
            Debug.Log("PlayerInputHandler: Input System初始化完成");
        }
    }
    
    #endregion
    
    #region 输入状态更新
    
    /// <summary>
    /// 更新输入状态
    /// </summary>
    void UpdateInputState()
    {
        // 使用New Input System
        moveInput = moveAction.ReadValue<Vector2>();
        isMovePressed = moveInput.magnitude > 0.1f;
        isChargePressed = chargeAction.WasPressedThisFrame();
        isChargeHeld = chargeAction.IsPressed();
        isChargeReleased = chargeAction.WasReleasedThisFrame();
        scrollDelta = scrollAction.ReadValue<float>();
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        // 首先检查顶层权限：是否在玩家阶段
        if (!permissionManager.CanProcessInputInCurrentPhase())
        {
            return; // 不在玩家阶段，不处理任何输入
        }
        
        // 处理WASD移动（只在Transition子阶段允许）
        if (movementController != null && permissionManager.CanMoveInCurrentSubPhase())
        {
            movementController.HandleMovement(moveInput, isMovePressed);
        }
        
        // 处理滚轮输入（在蓄力状态下调节力度）
        if (chargeSystem != null && Mathf.Abs(scrollDelta) > 0.01f)
        {
            chargeSystem.ProcessScrollInput(scrollDelta);
        }
        
        // 【滚轮模式特殊处理】：点击发射（不受Idle状态限制，因为已自动进入蓄力状态）
        if (isChargePressed && chargeSystem != null && 
            chargeSystem.CurrentChargeMode == ChargeSystem.ChargeMode.ScrollBased && 
            chargeSystem.IsCharging())
        {
            // 检查鼠标是否在UI上
            if (IsPointerOverUI())
            {
                return;
            }
            
            // 点击发射：检查阈值并发射
            if (!chargeSystem.CanLaunch())
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"PlayerInputHandler [滚轮模式]: 力度不足，无法发射（当前={chargeSystem.GetCurrentForce():F2}，需要>={chargeSystem.LaunchForceThreshold:F2}）");
                }
                return;
            }
            
            GameEventBus.PublishChargingStopped();
            return;
        }
        
        // 【其他模式】处理蓄力输入（只在Idle状态允许）
        if (isChargePressed && permissionManager.CanChargeInCurrentSubPhase())
        {
            // 检查鼠标是否在UI上
            if (IsPointerOverUI())
            {
                if (showDebugInfo)
                {
                    Debug.Log("PlayerInputHandler: 鼠标在UI上，忽略蓄力输入");
                }
                return;
            }
            
            // 滚轮模式已经自动进入蓄力，这里不需要处理
            if (chargeSystem != null && chargeSystem.CurrentChargeMode == ChargeSystem.ChargeMode.ScrollBased)
            {
                // 滚轮模式不走这里，已经在上面处理
                return;
            }
            
            // 其他模式：左键按下开始蓄力
            if (showDebugInfo)
            {
                Debug.Log("PlayerInputHandler: 检测到蓄力输入，发布蓄力开始事件");
            }
            GameEventBus.PublishChargingStarted();
        }
        
        // 非滚轮模式：左键释放停止蓄力
        if (isChargeReleased && chargeSystem != null && chargeSystem.CurrentChargeMode != ChargeSystem.ChargeMode.ScrollBased)
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerInputHandler: 检测到蓄力释放，发布蓄力停止事件");
            }
            GameEventBus.PublishChargingStopped();
        }
    }
    
    
    #endregion
    
    #region UI检测
    
    /// <summary>
    /// 检测鼠标是否在UI上
    /// </summary>
    bool IsPointerOverUI()
    {
        // 检查EventSystem是否存在
        if (EventSystem.current == null)
        {
            return false;
        }
        
        // PC端：检测鼠标是否在UI上
        return EventSystem.current.IsPointerOverGameObject();
    }
    
    #endregion
    
    #region 公共接口
    
    /// <summary>
    /// 获取移动输入
    /// </summary>
    public Vector2 GetMoveInput()
    {
        return moveInput;
    }
    
    /// <summary>
    /// 是否按下移动键
    /// </summary>
    public bool IsMovePressed()
    {
        return isMovePressed;
    }
    
    /// <summary>
    /// 是否按下蓄力键
    /// </summary>
    public bool IsChargePressed()
    {
        return isChargePressed;
    }
    
    /// <summary>
    /// 是否持续按住蓄力键
    /// </summary>
    public bool IsChargeHeld()
    {
        return isChargeHeld;
    }
    
    /// <summary>
    /// 是否释放蓄力键
    /// </summary>
    public bool IsChargeReleased()
    {
        return isChargeReleased;
    }
    
    #endregion
}
