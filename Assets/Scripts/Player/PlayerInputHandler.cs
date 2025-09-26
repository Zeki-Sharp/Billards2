using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家输入处理器 - 统一处理所有玩家输入检测和分发
/// 
/// 【核心职责】：
/// - 处理WASD移动输入和鼠标蓄力输入
/// - 使用New Input System进行输入检测
/// - 与PlayerInputPermissionManager协作进行权限检查
/// - 与PlayerStateMachine协作处理蓄力状态
/// 
/// 【设计原则】：
/// - 作为输入的统一入口，避免其他组件直接检测输入
/// - 专注于输入检测和分发，权限检查委托给PlayerInputPermissionManager
/// - 通过权限管理器检查所有输入权限
/// - 与PlayerStateMachine协作处理蓄力状态变化
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
    private PlayerStateMachine stateMachine;
    private PlayerMovementController movementController;
    private PlayerInputPermissionManager permissionManager;
    
    // Input System支持
    private InputAction moveAction;
    private InputAction chargeAction;
    private InputActionMap inputActionMap;
    
    // 输入状态
    private Vector2 moveInput;
    private bool isMovePressed;
    private bool isChargePressed;
    private bool isChargeHeld;
    private bool isChargeReleased;
    
    void Start()
    {
        // 获取组件引用
        stateMachine = GetComponent<PlayerStateMachine>();
        movementController = GetComponent<PlayerMovementController>();
        permissionManager = GetComponent<PlayerInputPermissionManager>();
        
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
    }
    
    #endregion
    
    #region 输入处理
    
    /// <summary>
    /// 处理输入
    /// </summary>
    void HandleInput()
    {
        
        // 处理WASD移动
        if (movementController != null && permissionManager.CanMoveInCurrentSubPhase())
        {
            movementController.HandleMovement(moveInput, isMovePressed);
        }
        
        // 处理蓄力输入
        if (isChargePressed && permissionManager.CanChargeInCurrentSubPhase())
        {
            if (showDebugInfo)
            {
                Debug.Log("PlayerInputHandler: 检测到蓄力输入，开始蓄力");
            }
            
            // 开始蓄力
            if (stateMachine != null)
            {
                stateMachine.StartCharging();
            }
        }
        
        // 处理蓄力释放 - 蓄力状态下释放鼠标
        if (isChargeReleased && stateMachine != null && stateMachine.CurrentState == PlayerStateMachine.PlayerState.Charging)
        {
            stateMachine.LaunchCharged();
        }
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
